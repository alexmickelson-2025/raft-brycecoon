using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DistributedTDDProject1;

public interface INode
{
    public Guid id { get; set; }
    public Guid voteId { get; set; }
    public int voteTerm { get; set; }
    public nodeState state { get; set; }
    public Guid currentLeader { get; set; }
    public int term { get; set; }
    public long timeoutInterval { get; set; }
    public int timeoutMultiplier { get; set; }
    public double networkDelay { get; set; }


    //functions
    public void requestVote(INode[] nodes);
    public string sendAppendRPC(INode recievingNode);
    public void sendVoteRequest(INode recievingNode);
    public void sendHeartbeatRPC(INode[] nodes);
    public void ResetTimer();
}
