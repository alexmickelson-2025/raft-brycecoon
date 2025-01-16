using System.Timers;

namespace DistributedTDDProject1;

public enum nodeState { 
    FOLLOWER,
    CANDIDATE,
    LEADER
}

public class Node : INode
{
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
    int heartbeatsSent = 0; //for testing

    public Node()
    {
        id = Guid.NewGuid();
        term = 0;
        voteTerm = 0;
        timeoutInterval = Random.Shared.NextInt64(150,301);
        neighbors = [];
        electionTimeoutTimer = new System.Timers.Timer(timeoutInterval);
        electionTimeoutTimer.Elapsed += Timer_Timeout;
        electionTimeoutTimer.AutoReset = false;
        electionTimeoutTimer.Start();
    }

    public void setElectionResults()
    {
        if (numVotesRecieved >= Math.Ceiling(((double)neighbors.Length+1) / 2))
        {
            state = nodeState.LEADER;
            StartHeartbeat();
        }
    }

    private void StartHeartbeat()
    {
        foreach(Node node in neighbors)
        {
            sendAppendRPC(node);
        }
        heartbeatsSent++;
        heartbeatTimer = new System.Timers.Timer(50);
        heartbeatTimer.Elapsed += sendHeartbeat;
        heartbeatTimer.AutoReset = true;
        heartbeatTimer.Start();
    }

    private void sendHeartbeat(object sender, ElapsedEventArgs e)
    {
        foreach(Node node in neighbors)
        {
            sendAppendRPC(node);
        }
        heartbeatsSent++;
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
            recievingNode.voteId = id;
            recievingNode.voteTerm = term;
            numVotesRecieved++;
        }
    }

    public string sendAppendRPC(INode recievingNode)
    {
        if (recievingNode.state == nodeState.CANDIDATE && (term >= recievingNode.term))
        {
            recievingNode.state = nodeState.FOLLOWER;
        }
        if (recievingNode.term <= term)
        {
            recievingNode.currentLeader = id;
            recievingNode.ResetTimer();
            return "recieved";
        }
        else { return "rejected"; }
    }

    public void startElection()
    {
        voteId = id;
        numVotesRecieved++;
        term++;
        state = nodeState.CANDIDATE;
    }

    public void Timer_Timeout(object sender, ElapsedEventArgs e)
    {
        startElection();
        ResetTimer();
    }

    public void ResetTimer()
    {
        electionTimeoutTimer.Stop();
        timeoutInterval = Random.Shared.NextInt64(150, 301);
        electionTimeoutTimer = new System.Timers.Timer(timeoutInterval);
        electionTimeoutTimer.Elapsed += Timer_Timeout;
        electionTimeoutTimer.AutoReset = false;
        electionTimeoutTimer.Start();
    }
}
