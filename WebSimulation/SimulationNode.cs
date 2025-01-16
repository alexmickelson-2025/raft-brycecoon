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

    Guid INode.id { get => InnerNode.id; set => InnerNode.id = value; }

    Guid INode.voteId { get => InnerNode.voteId; set => InnerNode.voteId = value; }
    int INode.voteTerm { get => InnerNode.voteTerm; set => InnerNode.voteTerm = value; }
    int INode.term { get => InnerNode.term; set => InnerNode.term = value; }
    nodeState INode.state { get => InnerNode.state; set => InnerNode.state = value; }
    Guid INode.currentLeader { get => InnerNode.currentLeader; set => InnerNode.currentLeader = value; }

    public void requestVote(INode[] nodes)
    {
        throw new NotImplementedException();
    }

    public void ResetTimer()
    {
        throw new NotImplementedException();
    }

    public string sendAppendRPC(INode recievingNode)
    {
        throw new NotImplementedException();
    }

    public void setElectionResults()
    {
        throw new NotImplementedException();
    }

    public void startElection()
    {
        throw new NotImplementedException();
    }

    public void Timer_Timeout(object sender, ElapsedEventArgs e)
    {
        throw new NotImplementedException();
    }
}
