using DistributedTDDProject1;
using System.Text.Json;
using System.Timers;

namespace WebSimulation;

//wraps the functionality and will add things like extra network delays
public class SimulationNode : INode
{
    public readonly Node InnerNode;
    public SimulationNode(Node node)
    {
        this.InnerNode = node;
    }

    public Guid id { get => InnerNode.id; set => InnerNode.id = value; }
    public Guid voteId { get => InnerNode.voteId; set => InnerNode.voteId = value; }
    public int term { get => InnerNode.term; set => InnerNode.term = value; }
    public int timeoutMultiplier { get => InnerNode.timeoutMultiplier; set => InnerNode.timeoutMultiplier = value; }
    public double networkDelay { get => InnerNode.networkDelay; set => InnerNode.networkDelay = value; }
    public long timeoutInterval { get => InnerNode.timeoutInterval; set => InnerNode.timeoutInterval = value; }
    public nodeState state { get => InnerNode.state; set => InnerNode.state = value; }
    public Guid currentLeader { get => InnerNode.currentLeader; set => InnerNode.currentLeader = value; }
    public List<Log> logs { get => InnerNode.logs; set => InnerNode.logs = value; }
    public Dictionary<string,string> stateMachine { get => InnerNode.stateMachine; set => InnerNode.stateMachine = value; }
    public int highestCommittedLogIndex { get => InnerNode.highestCommittedLogIndex; set => InnerNode.highestCommittedLogIndex = value; }
    public int prevIndex { get => InnerNode.prevIndex; }
    public string message { get; set; }
    public Dictionary<Guid, int> neighborNextIndexes { get => InnerNode.neighborNextIndexes; set => InnerNode.neighborNextIndexes = value; }


    public void Pause()
    {
        InnerNode.Resume();
    }

    public async Task RequestAppendEntry(AppendEntriesRequestRPC rpc)
    {
        message = JsonSerializer.Serialize(rpc);
        await InnerNode.RequestAppendEntry(rpc);
    }

    public async Task ReceiveAppendEntryRPCResponse(AppendEntriesResponseRPC rpc)
    {
        await InnerNode.ReceiveAppendEntryRPCResponse(rpc);
    }

    public async Task ReceiveVoteResponse(VoteResponseRPC rpc)
    {
        await InnerNode.ReceiveVoteResponse(rpc);
    }

    public async Task RequestVote(VoteRequestRPC rpc)
    {
        await InnerNode.RequestVote(rpc);
    }

    public void ResetTimer()
    {
        InnerNode.ResetTimer();
    }

    public void Resume()
    {
        InnerNode.Resume();
    }


    public Task sendVoteRequest(Guid id, int term)
    {
        return ((INode)InnerNode).RequestVote(new VoteRequestRPC { candidateId = id, candidateTerm = term });
    }

    public Task<bool> receiveCommandFromClient(string key, string value)
    {
        return InnerNode.recieveCommandFromClient(new clientData { key = key, message = value });
    }
}
