using System.Timers;

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
    int heartbeatsSent = 0; //for testing

    public Node()
    {
        id = Guid.NewGuid();
        term = 0;
        voteTerm = 0;
        timeoutInterval = Random.Shared.NextInt64(150,301);
        neighbors = [];
        timeoutMultiplier = 1;
        networkDelay = 0;
        electionTimeoutTimer = new System.Timers.Timer(timeoutInterval * timeoutMultiplier);
        electionTimeoutTimer.Elapsed += Timer_Timeout;
        electionTimeoutTimer.AutoReset = false;
        electionTimeoutTimer.Start();
    }

    public void setElectionResults()
    {
        if (numVotesRecieved >= Math.Ceiling(((double)neighbors.Length+1) / 2))
        {
            if (state == nodeState.CANDIDATE)
            {
                state = nodeState.LEADER;
                currentLeader = id;
                StartHeartbeat();
            }
        }
    }

    private void StartHeartbeat()
    {
        foreach(var node in neighbors)
        {
            sendAppendRPC(node);
        }
        heartbeatsSent++;
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
        Thread.Sleep((int)(networkDelay * 1000));
        //Thread.Sleep(500);
        foreach (var node in nodes)
        {
            sendAppendRPC(node);
        }
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
        if (recievingNode.term < term)
        {
            if(recievingNode.state != nodeState.FOLLOWER)
            {
                recievingNode.state = nodeState.FOLLOWER;
            }
            recievingNode.currentLeader = id;
            recievingNode.ResetTimer();
            return "recieved";
        }
        else { return "rejected"; }
    }

    public void startElection()
    {
        if (state != nodeState.LEADER)
        {
            voteId = id;
            numVotesRecieved++;
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
            setElectionResults();
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
}
