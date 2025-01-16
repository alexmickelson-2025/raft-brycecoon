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
    public System.Timers.Timer timer;
    public long timeoutInterval;
    public Guid currentLeader;

    public Node()
    {
        id = Guid.NewGuid();
        term = 0;
        voteTerm = 0;
        timeoutInterval = Random.Shared.NextInt64(150,301);
        neighbors = [];
        timer = new System.Timers.Timer(timeoutInterval);
        timer.Elapsed += Timer_Timeout;
        timer.AutoReset = false;
        timer.Start();
    }

    public void setElectionResults()
    {
        double voteCount = 0;
        if (voteId == id) { voteCount++; }
        foreach (Node node in neighbors)
        {
            if (node.voteId == id) { voteCount++; }
        }
        if (voteCount >= Math.Ceiling(((double)neighbors.Length+1) / 2))
        {
            state = nodeState.LEADER;
        }
    }

    public void requestVote(Node[] nodes)
    {
        foreach (Node node in nodes)
        {
            if (node.voteTerm < term)
            {
                node.voteId = id;
                node.voteTerm = term;
            }
        }
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
        return "recieved";

        }
        else { return "rejected"; }
    }

    public void startElection()
    {
        voteId = id;
        term++;
        state = nodeState.CANDIDATE;
    }

    public void Timer_Timeout(object sender, ElapsedEventArgs e)
    {
        ResetTimer();
        startElection();
    }

    public void ResetTimer()
    {
        timeoutInterval = Random.Shared.NextInt64(150, 301);
        timer = new System.Timers.Timer(timeoutInterval);
        timer.Elapsed += Timer_Timeout;
        timer.AutoReset = false;
        timer.Start();
    }
}
