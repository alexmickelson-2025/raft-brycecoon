using System.Timers;
using System.Xml.Linq;
using System.Text.Json;

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
    public nodeState state { get; set; }
    public INode[] neighbors { get; set; }
    public INode Client { get; set; }
    System.Timers.Timer electionTimeoutTimer { get; set; }
    System.Timers.Timer heartbeatTimer { get; set; }
    public long timeoutInterval { get; set; }
    public Guid currentLeader { get; set; }
    public int numVotesRecieved { get; set; }
    public double networkDelay { get; set; }
    int heartbeatsSent = 0;
    public List<Log> logs { get; set; }
    public Dictionary<string, string> stateMachine;
    public Dictionary<Guid, int> neighborNextIndexes;
    public Dictionary<int, int> LogToTimesReceived;
    public int nextIndex;
    public int prevIndex { get => logs.Count -1; }
    public int highestCommittedLogIndex;
    public static double NodeIntervalScalar;
    public string url;

    public Node()
    {
        id = Guid.NewGuid();
        term = 0;
        voteTerm = 0;
        timeoutInterval = Random.Shared.NextInt64(150, 301);
        neighbors = [];
        timeoutMultiplier = 1;
        networkDelay = 0;
        nextIndex = 1;
        logs = new();
        highestCommittedLogIndex = -1;
        stateMachine = new Dictionary<string, string>();
        neighborNextIndexes = new Dictionary<Guid, int>();
        LogToTimesReceived = new Dictionary<int, int>();
        electionTimeoutTimer = new System.Timers.Timer(timeoutInterval * timeoutMultiplier);
        electionTimeoutTimer.Elapsed += Timer_Timeout;
        electionTimeoutTimer.AutoReset = false;
        electionTimeoutTimer.Start();
        
    }

    public Task RequestVoteFromEachNeighbor()
    {
        foreach (var node in neighbors)
        {
            node.RequestVote(new VoteRequestRPC { candidateId = id, candidateTerm = term });
        }
        return Task.CompletedTask;
    }

    public Task RequestVote(VoteRequestRPC rpc)
    {
        var candidateNode = neighbors.FirstOrDefault((n) => n.id == rpc.candidateId);
        if (candidateNode == null)
        {
            Console.WriteLine(rpc);
            Console.WriteLine(JsonSerializer.Serialize(neighbors));

            throw new Exception("Candidate Was Null");
        };

        if (rpc.candidateTerm > term)
        {
            term = rpc.candidateTerm;
            voteTerm = rpc.candidateTerm;
            voteId = rpc.candidateId;
            state = nodeState.FOLLOWER;
            candidateNode.ReceiveVoteResponse(new VoteResponseRPC { response = true });
        }
        else
        {
            candidateNode.ReceiveVoteResponse(new VoteResponseRPC { response = false });
        }

        return Task.CompletedTask;
    }

    public Task ReceiveVoteResponse(VoteResponseRPC rpc)
    {
        if (rpc.response)
        {
            numVotesRecieved++;
            setElectionResults();
        }
        return Task.CompletedTask;
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
        if (state != nodeState.LEADER) { return; }
        leaderTimeout();

        heartbeatTimer = new System.Timers.Timer(50);
        heartbeatTimer.Elapsed += (sender, e) => StartHeartbeat();
        heartbeatTimer.AutoReset = false;
        heartbeatTimer.Start();
    }

    public void leaderTimeout()
    {
        sendHeartbeatRPC(neighbors);
    }

    public void sendHeartbeatRPC(INode[] nodes)
    {
        foreach (var node in nodes)
        {
            sendAppendRPCRequest(node.id);
        }
    }

    public void sendAppendRPCRequest(Guid ReceiverId)
    {
        INode receivingNode = neighbors.FirstOrDefault((n) => n.id == ReceiverId);
        if (receivingNode == null)
        {
            throw new Exception("Receiving Node was null");
        }

        int sendingLogIndex = neighborNextIndexes[ReceiverId];
        int logTerm = (logs.Count > sendingLogIndex && sendingLogIndex > 0) ? logs[sendingLogIndex].term : 0;
        List<Log> entries = logs.Skip(sendingLogIndex).ToList();

        var rpc = new AppendEntriesRequestRPC
        {
            LeaderId = id,
            Term = term,
            PrevLogIndex = sendingLogIndex,
            PrevLogTerm = logTerm,
            Entries = entries,
            leaderHighestLogCommitted = highestCommittedLogIndex
        };

        receivingNode.RequestAppendEntry(rpc);
    }

    public Task RequestAppendEntry(AppendEntriesRequestRPC rpc)
    {

        INode leaderNode = neighbors.FirstOrDefault((n) => n.id == rpc.LeaderId);
        if (leaderNode == null)
        {
            throw new Exception("Leader Was Null");
        }

        if (rpc.Term >= term)
        {
            UpdatePerceivedLeader(leaderNode);
            ResetTimer();
        }

        if ((rpc.Term >= term) && rpc.PrevLogIndex == prevIndex)
        {

            AddReceivedLogsToPersonalLogs(rpc);

            List<Log> logsToCommit = logs[(highestCommittedLogIndex+1)..(rpc.leaderHighestLogCommitted+1)];
            highestCommittedLogIndex = rpc.leaderHighestLogCommitted;
            foreach(var log in logsToCommit)
            {
                stateMachine[log.key] = log.message;
            }

            SendReceivedTrueToLeader(leaderNode);
        }
        else if (LeaderHasLowerPrevIndex(rpc))
        {
            for (int i = 0; i < (prevIndex - rpc.PrevLogIndex); i++)
            {
                if (logs.Last() == null)
                {
                    throw new Exception("Tried to Remove a null object from logs");
                }
                logs.Remove(logs.Last());
            }
            SendReceivedFalseToLeader(leaderNode);
        }

        else
        {
            SendReceivedFalseToLeader(leaderNode);
            return Task.CompletedTask;
        }
        return Task.CompletedTask;

    }

    private bool LeaderHasLowerPrevIndex(AppendEntriesRequestRPC rpc)
    {
        return (rpc.Term >= term) && (rpc.PrevLogIndex < prevIndex + 1);
    }

    private Task SendReceivedFalseToLeader(INode leaderNode)
    {
        AppendEntriesResponseRPC rpcResponse = new AppendEntriesResponseRPC
        {
            sendingNode = id,
            received = false,
            followerHighestReceivedIndex = highestCommittedLogIndex
        };
        leaderNode.ReceiveAppendEntryRPCResponse(rpcResponse);
        return Task.CompletedTask;

    }

    private Task SendReceivedTrueToLeader(INode leaderNode)
    {
        AppendEntriesResponseRPC rpcResponse = new AppendEntriesResponseRPC
        {
            sendingNode = id,
            received = true,
            followerHighestReceivedIndex = prevIndex + 1 //most recent change
        };
        leaderNode.ReceiveAppendEntryRPCResponse(rpcResponse);
        return Task.CompletedTask;
    }

    private void AddReceivedLogsToPersonalLogs(AppendEntriesRequestRPC rpc)
    {
        foreach (Log log in rpc.Entries)
        {
            if (log.term > highestCommittedLogIndex)
            {
                logs.Add(log);
                stateMachine[log.key] = log.message;  // Commit log immediately
                if(logs.Count > stateMachine.Count)
                {
                    logs.Remove(logs.Last());
                }
            }
        }
    }

    private void UpdatePerceivedLeader(INode leaderNode)
    {
        currentLeader = leaderNode.id;
        if (state != nodeState.FOLLOWER) { state = nodeState.FOLLOWER; }
    }

    public Task ReceiveAppendEntryRPCResponse(AppendEntriesResponseRPC rpc)
    {
        if (rpc.received == true)
        {
            neighborNextIndexes[rpc.sendingNode] = rpc.followerHighestReceivedIndex + 1;
            if (LogToTimesReceived.ContainsKey(prevIndex))
            {
                LogToTimesReceived[prevIndex]++;
            }
            else
            {
                LogToTimesReceived[prevIndex] = 1;
            }

            if (LogToTimesReceived.ContainsKey(highestCommittedLogIndex + 1) &&
                LogToTimesReceived[highestCommittedLogIndex + 1] >= Math.Ceiling(((double)neighbors.Length + 1) / 2) &&
                highestCommittedLogIndex + 1 < logs.Count)
            {
                CommitNextLog();
            }

        }
        else if (rpc.followerHighestReceivedIndex != neighborNextIndexes[rpc.sendingNode])
        {
            neighborNextIndexes[rpc.sendingNode]--;
        }
        return Task.CompletedTask;
    }

    private void CommitNextLog()
    {
        highestCommittedLogIndex = prevIndex;
        var logEntry = logs[prevIndex];
        stateMachine[logEntry.key] = logEntry.message;

        nextIndex++;
    }

    public Task startElection()
    {
        if (state != nodeState.LEADER)
        {
            voteId = id;
            numVotesRecieved = 1;
            term++;
            state = nodeState.CANDIDATE;

            RequestVoteFromEachNeighbor();
        }
        return Task.CompletedTask;
    }

    public void Timer_Timeout(object sender, ElapsedEventArgs e)
    {
        if (state != nodeState.LEADER)
        {
            startElection();
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

    public async Task recieveCommandFromClient(clientData data)
    {
        if (state == nodeState.LEADER)
        {
            Log newLog = new Log();
            newLog.key = data.key;
            newLog.term = term;
            newLog.message = data.message;
            logs.Add(newLog);
        }
        await Task.CompletedTask;
    }

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