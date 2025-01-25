using System.Timers;
using System.Xml.Linq;

namespace DistributedTDDProject1;

public enum nodeState { 
    FOLLOWER,
    CANDIDATE,
    LEADER
}

public class Node : INode
{
    public int timeoutMultiplier { get; set; }
    public Guid id { get; set; }
    public Guid voteId {get;set;}
    public int voteTerm {get;set;}
    public int term {get;set;}
    public nodeState state {get;set;} //starts as the first option (follower)
    public INode[] neighbors {get;set;}
    System.Timers.Timer electionTimeoutTimer {get;set;}
    System.Timers.Timer heartbeatTimer {get;set;}
    public long timeoutInterval {get;set;}
    public Guid currentLeader {get;set;}
    public int numVotesRecieved {get;set;}
    public double networkDelay {get;set; }
    int heartbeatsSent = 0;
    public List<Log> logs { get; set; }
    public Dictionary<int, string> stateMachine;
    public Dictionary<Guid, int> neighborNextIndexes;
    public Dictionary<int, int> LogToTimesReceived;
    public int nextIndex;
    public int prevIndex;
    public int highestCommittedLogIndex;

    public Node()
    {
        id = Guid.NewGuid();
        term = 0;
        voteTerm = 0;
        timeoutInterval = Random.Shared.NextInt64(150,301);
        neighbors = [];
        timeoutMultiplier = 1;
        networkDelay = 0;
        prevIndex = 0;
        nextIndex = 0;
        logs = new();
        highestCommittedLogIndex = 0;
        stateMachine = new Dictionary<int, string>();
        neighborNextIndexes = new Dictionary<Guid, int>();
        LogToTimesReceived = new Dictionary<int, int>();
        electionTimeoutTimer = new System.Timers.Timer(timeoutInterval * timeoutMultiplier);
        electionTimeoutTimer.Elapsed += Timer_Timeout;
        electionTimeoutTimer.AutoReset = false;
        electionTimeoutTimer.Start();
    }

    public void requestVote(INode[] nodes)
    {
        foreach (var node in nodes)
        {
            sendVoteRequest(node);
        }
    }

    public void sendVoteRequest(INode recievingNode)
    {
        if (recievingNode.voteTerm < term)
        {
            recievingNode.RecieveVoteRequest(id, term);
        }
    }

    public void RecieveVoteRequest(Guid candidateId, int candidateTerm)
    {
        var candidateNode = neighbors.FirstOrDefault((n) => n.id == candidateId);
        if (candidateNode == null) return;
        if (candidateTerm >= term)
        {
            voteTerm = candidateTerm;
            voteId = candidateId;
            state = nodeState.FOLLOWER;
            candidateNode.recieveResponseToVoteRequest(true);
        }
        else
        {
            candidateNode.recieveResponseToVoteRequest(false);
        }
    }

    public void recieveResponseToVoteRequest(bool voteResponse)
    {
        if (voteResponse) { numVotesRecieved++; }
        setElectionResults();
    }

    public void setElectionResults()
    {
        if (numVotesRecieved >= Math.Ceiling(((double)neighbors.Length + 1) / 2))
        {
            if (state == nodeState.CANDIDATE)
            {
                state = nodeState.LEADER;
                currentLeader = id;
                foreach (var node in neighbors) { neighborNextIndexes[node.id] = nextIndex; }
                StartHeartbeat();
            }
        }
    }

    public void StartHeartbeat()
    {
        foreach (var node in neighbors)
        {
            sendAppendRPCRequest(node, "");
        }
        heartbeatTimer = new System.Timers.Timer(50);
        heartbeatTimer.Elapsed += leaderTimeout;
        heartbeatTimer.AutoReset = true;
        heartbeatTimer.Start();
    }

    public void leaderTimeout(object sender, ElapsedEventArgs e)
    {
        sendHeartbeatRPC(neighbors);
    }

    public void sendHeartbeatRPC(INode[] nodes)
    {
        foreach (var node in nodes)
        {
            sendAppendRPCRequest(node, "");
        }
    }

    //leader calls on follower
    public void sendAppendRPCRequest(INode recievingNode, string message)
    {
        //follower runs this
        recievingNode.ReceiveAppendEntryRequest(id, highestCommittedLogIndex, logs[prevIndex].message);
    }

    public void ReceiveAppendEntryRequest(Guid leaderId, int commitIndex, string message)
    {
        var leaderNode = neighbors.FirstOrDefault((n) => n.id == leaderId);

        Log newLog = new Log
        {
            term = term,
            message = message
        };

        logs.Add(newLog);
        prevIndex = logs.Count - 1;
        nextIndex = logs.Count;

        leaderNode?.recieveResponseToAppendEntryRPCRequest(id, true);  //include the data like who is the response from, and did they reject it
    }

    //Leaders response to requested data
    public void recieveResponseToAppendEntryRPCRequest(Guid sendingNode, bool received)
    {

        if (received)
        {
            if (!LogToTimesReceived.ContainsKey(prevIndex))
            {
                LogToTimesReceived[prevIndex] = 2; //2 already because the leader obviously received it
            }
            else
            {
                LogToTimesReceived[prevIndex] += 1;
            }
        }

        if(!LogToTimesReceived.ContainsKey(0))
        {
            return;
        }

        if (LogToTimesReceived[prevIndex] >= Math.Ceiling(((double)neighbors.Length + 1) / 2)) //majority won
        {
            int potentialCommittedLogkey = logs[prevIndex].key;
            commitLogToStateMachine(potentialCommittedLogkey);
        }
    }

    public void startElection()
    {
        if (state != nodeState.LEADER)
        {
            voteId = id;
            numVotesRecieved = 1;
            term++;
            state = nodeState.CANDIDATE;
        }
    }

    public void Timer_Timeout(object sender, ElapsedEventArgs e)
    {
        if (state != nodeState.LEADER)
        {
            startElection();
            requestVote(neighbors);
            ResetTimer();
        }
    }

    public void ResetTimer()
    {
        electionTimeoutTimer.Stop();
        timeoutInterval = Random.Shared.NextInt64(150, 301);
        electionTimeoutTimer = new System.Timers.Timer(timeoutInterval * timeoutMultiplier);
        electionTimeoutTimer.Elapsed += Timer_Timeout;
        electionTimeoutTimer.AutoReset = false;
        electionTimeoutTimer.Start();
    }

    public void recieveCommandFromClient(int key, string message)
    {
        Log newLog = new Log();
        newLog.key = key;
        newLog.term = term;
        newLog.message = message;
        logs.Add(newLog);
        prevIndex = logs.Count - 1;
        nextIndex = logs.Count;
    }

    public void commitLogToStateMachine(int logIndex)
    {
        stateMachine[highestCommittedLogIndex + 1] = logs.Where(x => x.key == logIndex).FirstOrDefault().message;
        highestCommittedLogIndex = stateMachine.Count;
    }

}
