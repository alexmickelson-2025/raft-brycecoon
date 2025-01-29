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
    public List<Log> logs { get; set; }


    //functions
    public Task RequestVote(INode[] nodes);
    public Task sendVoteRequest(INode recievingNode);
    public Task RecieveVoteRequest(Guid candidateId, int candidateTerm);
    public Task recieveResponseToVoteRequest(bool voteResponse);
    public Task sendHeartbeatRPC(INode[] nodes);
    public Task sendAppendRPCRequest(INode recievingNode);

    public Task ReceiveAppendEntryRequest(AppendEntriesRequestRPC rpc);
    public Task recieveResponseToAppendEntryRPCRequest(AppendEntriesResponseRPC rpc);
    public void ResetTimer();
    public void Pause();
    public void Resume();
    public Task ReceiveClientResponse(ClientResponseArgs clientResponseArgs);
}
