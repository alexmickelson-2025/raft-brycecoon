using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedTDDProject1;

public class Log
{
    public string key;
    public int term;
    public string message;
}

public class AppendEntriesRequestRPC
{
    public Guid LeaderId { get; set; }
    public int Term { get; set; }
    public int PrevLogIndex { get; set; }
    public int PrevLogTerm { get; set; }
    public List<Log> Entries { get; set; } = new();
    public int leaderHighestLogCommitted { get; set; }
}

public class AppendEntriesResponseRPC
{
    public Guid sendingNode { get; set; }
    public bool received {  get; set; }
    public int followerHighestReceivedIndex { get; set; }
}
