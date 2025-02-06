using DistributedTDDProject1;

namespace RaftClient;

public class NodeDataDTO {
    public Guid id;
    public nodeState state; 
    public int timeoutPercentageLeft;
    public int Term;
    public Guid CurrentTermLeader; 
    public int CommittedEntryIndex;
    public List<Log> logs;
    public Dictionary<string,string> StateMachine;
  };
