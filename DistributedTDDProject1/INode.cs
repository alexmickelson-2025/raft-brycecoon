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

    public Task RequestVote(VoteRequestRPC rpc);
    public Task ReceiveVoteResponse(VoteResponseRPC rpc);
    public Task RequestAppendEntry(AppendEntriesRequestRPC rpc);
    public Task ReceiveAppendEntryRPCResponse(AppendEntriesResponseRPC rpc);
}
