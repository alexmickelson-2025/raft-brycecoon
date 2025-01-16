using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DistributedTDDProject1;

public interface INode
{
    Guid id { get; set; }
    Guid voteId { get; set; }
    int voteTerm { get; set; }
    nodeState state { get; set; }
    Guid currentLeader { get; set; }
    int term { get; set; }


    //functions
    void setElectionResults();
    void requestVote(INode[] nodes);
    string sendAppendRPC(INode recievingNode);
    void startElection();
    void Timer_Timeout(object sender, ElapsedEventArgs e);
    void ResetTimer();
}
