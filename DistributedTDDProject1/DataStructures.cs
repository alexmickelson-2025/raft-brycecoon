using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedTDDProject1;

public class Log
{
    public int key;
    public int term;
    public string message;
}

//public record SendRPCRequestArgs
//{
//    public int highestCommittedIndex;
//    public INode recievingNode;
//    public string message;
//}

//public record RecieveRPCRequestArgs
//{
//    public Guid leaderId; 
//    public int commitIndex;
//    public string message;

//    public RecieveRPCRequestArgs(Guid id, int highestCommittedIndex, string message)
//    {
//        Id = id;
//        HighestCommittedIndex = highestCommittedIndex;
//        this.message = message;
//    }

//    public Guid Id { get; }
//    public int HighestCommittedIndex { get; }
//}

//public record RPCResponseArgs
//{
//    INode recievingNode;
//}


