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
    public double numNodes;
    public Timer timer;
    public long timeoutInterval;

    public Node()
    {
        id = Guid.NewGuid();
        term = 0;
        voteTerm = 0;
        timeoutInterval = Random.Shared.NextInt64(150,301);
        timer = new Timer(timerCallback, null, 0, timeoutInterval);
    }

    public void countVotes()
    {
        if (voteId == id) { state = nodeState.LEADER; }
    }

    public bool countVotes(Node[] nodes)
    {
        double voteCount = 0;
        if (voteId == id) { voteCount++; }
        foreach (Node node in nodes)
        {
            if (node.voteId == id) { voteCount++; }
        }
        return (voteCount >= Math.Ceiling(numNodes/2));
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

    public void resetTimeoutInterval()
    {
        timeoutInterval = Random.Shared.NextInt64(150, 301);
    }

    public string sendAppendRPC(Node n)
    {
        if (n.state == nodeState.CANDIDATE && (term >= n.term))
        {
            n.state = nodeState.FOLLOWER;
        }
        if (n.term <= term)
        { 
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

    public void timerCallback(object state)
    {
        startElection();
    }
}
