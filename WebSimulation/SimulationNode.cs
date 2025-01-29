using DistributedTDDProject1;
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
    public int voteTerm { get => InnerNode.voteTerm; set => InnerNode.voteTerm = value; }
    public int term { get => InnerNode.term; set => InnerNode.term = value; }
    public int timeoutMultiplier { get => InnerNode.timeoutMultiplier; set => InnerNode.timeoutMultiplier = value; }
    public double networkDelay { get => InnerNode.networkDelay; set => InnerNode.networkDelay = value; }
    public long timeoutInterval { get => InnerNode.timeoutInterval; set => InnerNode.timeoutInterval = value; }
    public nodeState state { get => InnerNode.state; set => InnerNode.state = value; }
    public Guid currentLeader { get => InnerNode.currentLeader; set => InnerNode.currentLeader = value; }
    public List<Log> logs { get => InnerNode.logs; set => InnerNode.logs = value; }

    public void Pause()
    {
        InnerNode.Resume();
    }

    public async Task ReceiveAppendEntryRequest(AppendEntriesRequestRPC rpc)
    {
        await InnerNode.ReceiveAppendEntryRequest(rpc);
    }

    public Task ReceiveClientResponse(ClientResponseArgs clientResponseArgs)
    {
        throw new NotImplementedException();
    }

    public async Task recieveResponseToAppendEntryRPCRequest(AppendEntriesResponseRPC rpc)
    {
        await InnerNode.recieveResponseToAppendEntryRPCRequest(rpc);
    }

    public async Task recieveResponseToVoteRequest(bool voteResponse)
    {
        await InnerNode.recieveResponseToVoteRequest(voteResponse);
    }

    public async Task RecieveVoteRequest(Guid candidateId, int candidateTerm)
    {
        await InnerNode.RecieveVoteRequest((Guid)candidateId, candidateTerm);
    }

    public async Task RequestVote(INode[] nodes)
    {
        await InnerNode.RequestVote(nodes);
    }

    public void ResetTimer()
    {
        InnerNode.ResetTimer();
    }

    public void Resume()
    {
        InnerNode.Resume();
    }

    public async Task sendAppendRPCRequest(INode recievingNode)
    {
        await InnerNode.sendAppendRPCRequest(recievingNode);
    }

    public async Task sendHeartbeatRPC(INode[] nodes)
    {
        await InnerNode.sendHeartbeatRPC(nodes);
    }

    public async Task sendVoteRequest(INode recievingNode)
    {
        await InnerNode.sendVoteRequest(recievingNode);
    }
}
