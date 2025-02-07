using DistributedTDDProject1;

namespace RaftClient;

public record NodeDataDTO (
    Guid id,
    nodeState state,
    long timeoutPercentageLeft,
    int Term,
    Guid CurrentTermLeader,
    int CommittedEntryIndex,
    List<Log>? logs,
    Dictionary<string,string>? StateMachine
);

