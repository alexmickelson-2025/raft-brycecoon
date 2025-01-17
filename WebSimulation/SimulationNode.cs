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

    public void requestVote(INode[] nodes)
    {
        InnerNode.requestVote(nodes);
    }

    public void ResetTimer()
    {
        InnerNode.ResetTimer();
    }

    public string sendAppendRPC(INode recievingNode)
    {
        Thread.Sleep(500);
        return InnerNode.sendAppendRPC(recievingNode);
    }

    public void sendHeartbeatRPC(INode[] nodes)
    {
        Thread.Sleep(500);
        InnerNode.sendHeartbeatRPC(nodes);
    }

    public void sendVoteRequest(INode recievingNode)
    {
        InnerNode.sendVoteRequest(recievingNode);
    }
}
