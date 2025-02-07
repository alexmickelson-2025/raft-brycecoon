using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedTDDProject1;

public record Log
{
    public string Key {get;set;}
    public int term {get;set;}
    public string Message {get;set;}
}

public record AppendEntriesRequestRPC
{
    public Guid LeaderId { get; set; }
    public int Term { get; set; }
    public int PrevLogIndex { get; set; }
    public int PrevLogTerm { get; set; }
    public List<Log> Entries { get; set; }
    public int leaderHighestLogCommitted { get; set; }
}

public record AppendEntriesResponseRPC
{
    public Guid sendingNode { get; set; }
    public bool received {  get; set; }
    public int followerHighestReceivedIndex { get; set; }
    public int term { get; set; }
}

public record VoteRequestRPC
{
    public Guid candidateId { get; set; }  
    public int candidateTerm { get; set; }
}

public record VoteResponseRPC
{
    public bool response { get; set; }
}

public record clientData
{
    public string Key { get; set; }
    public string Message { get; set; }
}