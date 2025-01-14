using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedTDDProject1;

public interface INode
{
    Guid id { get; set; }
    Guid voteId { get; set; }
    int term { get; set; }
    nodeState state { get; set; } //starts as the first option (follower)
    double numNodes { get; set; }
}
