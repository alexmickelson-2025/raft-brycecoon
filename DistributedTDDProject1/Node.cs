using System.Timers;

namespace DistributedTDDProject1;

public enum nodeState { 
    FOLLOWER,
    CANDIDATE,
    LEADER
}

public class Node
{
    public Guid id;
    public Guid voteId;
    public int voteTerm;
    public int term;
    public nodeState state; //starts as the first option (follower)
    public Node[] neighbors;
    public System.Timers.Timer electionTimeoutTimer;
    public System.Timers.Timer heartbeatTimer;
    public long timeoutInterval;
    public Guid currentLeader;
    public int numVotesRecieved;

    public int heartbeatsSent = 0; //for testing

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
        double voteCount = 0;
        if (voteId == id) { voteCount++; }
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

    public void requestVote(Node[] nodes)
    {
        foreach (Node node in nodes)
        {
            if (node.voteTerm < term)
            {
                node.voteId = id;
                node.voteTerm = term;
                sendYesVote();
            }
        }
    }

    private void sendYesVote()
    {
        numVotesRecieved++;
    }

    public string sendAppendRPC(Node recievingNode)
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
        sendYesVote();
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
