﻿using System.Timers;
using System.Xml.Linq;

namespace DistributedTDDProject1;

public enum nodeState
{
    FOLLOWER,
    CANDIDATE,
    LEADER
}

public class Node : INode
{
    public int timeoutMultiplier { get; set; }
    public Guid id { get; set; }
    public Guid voteId { get; set; }
    public int voteTerm { get; set; }
    public int term { get; set; }
    public nodeState state { get; set; } //starts as the first option (follower)
    public INode[] neighbors { get; set; }
    System.Timers.Timer electionTimeoutTimer { get; set; }
    System.Timers.Timer heartbeatTimer { get; set; }
    public long timeoutInterval { get; set; }
    public Guid currentLeader { get; set; }
    public int numVotesRecieved { get; set; }
    public double networkDelay { get; set; }
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
        timeoutInterval = Random.Shared.NextInt64(150, 301);
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

    public async Task RequestVote(INode[] nodes)
    {
        foreach (var node in nodes)
        {
            await sendVoteRequest(node);
        }
    }

    public async Task sendVoteRequest(INode recievingNode)
    {
        if (recievingNode.voteTerm < term)
        {
            await recievingNode.RecieveVoteRequest(id, term);
        }
    }

    public async Task RecieveVoteRequest(Guid candidateId, int candidateTerm)
    {
        var candidateNode = neighbors.FirstOrDefault((n) => n.id == candidateId);
        if (candidateNode == null) return;

        if (candidateTerm >= voteTerm)
        {
            voteTerm = candidateTerm;
            voteId = candidateId;
            state = nodeState.FOLLOWER;
            await candidateNode.recieveResponseToVoteRequest(true);
        }
        else
        {
            await candidateNode.recieveResponseToVoteRequest(false);
        }
    }

    public async Task recieveResponseToVoteRequest(bool voteResponse)
    {
        if (voteResponse) { numVotesRecieved++; }
        await setElectionResults();
    }

    public async Task setElectionResults()
    {
        if (numVotesRecieved >= Math.Ceiling(((double)neighbors.Length + 1) / 2))
        {
            if (state == nodeState.CANDIDATE)
            {
                state = nodeState.LEADER;
                currentLeader = id;
                foreach (var node in neighbors) { neighborNextIndexes[node.id] = nextIndex; }
                await StartHeartbeat();
            }
        }
    }

    public async Task StartHeartbeat()
    {
        foreach (var node in neighbors)
        {
            await sendAppendRPCRequest(node, "");
        }

        heartbeatTimer = new System.Timers.Timer(50);
        heartbeatTimer.Elapsed += async (sender, e) => await leaderTimeout(sender, e);
        heartbeatTimer.AutoReset = true;
        heartbeatTimer.Start();
    }

    public async Task leaderTimeout(object sender, ElapsedEventArgs e)
    {
        await sendHeartbeatRPC(neighbors);
    }


    public async Task sendHeartbeatRPC(INode[] nodes)
    {
        foreach (var node in nodes)
        {
            await sendAppendRPCRequest(node, "");
        }
    }

    public async Task sendAppendRPCRequest(INode recievingNode, string message)
    {
        //follower runs this
        await recievingNode.ReceiveAppendEntryRequest(id, highestCommittedLogIndex, message);
    }

    public async Task ReceiveAppendEntryRequest(Guid leaderId, int commitIndex, string message)
    {
        INode leaderNode = neighbors.FirstOrDefault((n) => n.id == leaderId);
        if (leaderNode == null)
        {
            throw new Exception("Leader Was Null");
        }

        if (leaderNode.term >= term)
        {
            currentLeader = leaderNode.id;
            if (state != nodeState.FOLLOWER) { state = nodeState.FOLLOWER; }

            //Log newLog = new Log
            //{
            //    term = term,
            //    message = message
            //};

            //logs.Add(newLog);
            //prevIndex = logs.Count - 1;
            //nextIndex = logs.Count;
            ResetTimer();
            await leaderNode.recieveResponseToAppendEntryRPCRequest(id, true);
        }
        else if (leaderNode.term < term)
        {
            await leaderNode.recieveResponseToAppendEntryRPCRequest(id, false);
        }
    }
    public async Task recieveResponseToAppendEntryRPCRequest(Guid sendingNode, bool received)
    {
        // If the log entry is successfully received, handle the log updates (commented out here).
        // if (received)
        // {
        //     if (!LogToTimesReceived.ContainsKey(prevIndex))
        //     {
        //         LogToTimesReceived[prevIndex] = 2;
        //     }
        //     else
        //     {
        //         LogToTimesReceived[prevIndex] += 1;
        //     }
        //     if (LogToTimesReceived[prevIndex] >= Math.Ceiling(((double)neighbors.Length + 1) / 2))
        //     {
        //         // Commit the log to state machine
        //     }
        // }
    }


    public async Task startElection()
    {
        if (state != nodeState.LEADER)
        {
            voteId = id;
            numVotesRecieved = 1;
            term++;
            state = nodeState.CANDIDATE;

            await RequestVote(neighbors);
        }
    }

    public async void Timer_Timeout(object sender, ElapsedEventArgs e)
    {
        if (state != nodeState.LEADER)
        {
            await startElection();
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

    //public void commitLogToStateMachine(string message)
    //{
    //    var log = logs.FirstOrDefault(x => x.key == logIndex);
    //    if (log != null)
    //    {
    //        stateMachine[highestCommittedLogIndex + 1] = log.message;
    //        highestCommittedLogIndex = stateMachine.Count;
    //    }
    //}

    public void Pause()
    {
        heartbeatTimer.Stop();
        electionTimeoutTimer.Stop();
    }

    public void Resume()
    {
        heartbeatTimer.Start();
        electionTimeoutTimer.Start();
    }
}
