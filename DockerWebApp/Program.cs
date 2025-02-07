using System.Text.Json;
using DistributedTDDProject1;
using RaftClient;


var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");

var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? throw new Exception("NODE_ID environment variable not set");
var otherNodesRaw = Environment.GetEnvironmentVariable("OTHER_NODES") ?? throw new Exception("OTHER_NODES environment variable not set");
var nodeUrls = Environment.GetEnvironmentVariable("NODE_URLS") ?? throw new Exception("NODE_INTERVAL_SCALAR environment variable not set");

var app = builder.Build();

var logger = app.Services.GetService<ILogger<Program>>();
Console.WriteLine($"Node ID {nodeId}");
Console.WriteLine($"Other nodes environment config: {otherNodesRaw}");

INode[] otherNodes = otherNodesRaw
    .Split(";", StringSplitOptions.RemoveEmptyEntries)
    .Select(s =>
    {
        var parts = s.Split(",", StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            throw new FormatException($"Invalid node format: '{s}'");
        }

        string guidString = parts[0].Trim();
        string url = parts[1].Trim();

        // Print the extracted GUID before parsing
        Console.WriteLine($"Extracted GUID: '{guidString}'");

        if (!Guid.TryParse(guidString, out var guid))
        {
            throw new FormatException($"Failed to parse GUID: '{guidString}'");
        }

        return new HttpRpcOtherNode(guid, url);
    })
    .ToArray();

var node = new Node()
{
    id = Guid.Parse(nodeId),
};

node.neighbors = otherNodes;

app.MapGet("/request/nodeData", () =>
{

    //set the node Data then send it
    return new NodeDataDTO
    (
        id: node.id,
        state: node.state,
        timeoutPercentageLeft: node.timeoutInterval,
        Term : node.term,
        CurrentTermLeader : node.currentLeader,
        CommittedEntryIndex : node.highestCommittedLogIndex,
        logs : node.logs,
        StateMachine : node.stateMachine
    );
});

app.MapPost("/request/appendEntries", async (AppendEntriesRequestRPC request) =>
{
    // Console.WriteLine($"received append entries request {request}");
    await node.RequestAppendEntry(request);
});

app.MapPost("/request/vote", async (VoteRequestRPC request) =>
{
    // Console.WriteLine($"received vote request {request}");
    await node.RequestVote(request);
});

app.MapPost("/response/appendEntries", async (AppendEntriesResponseRPC response) =>
{
    // Console.WriteLine($"received append entries response {response}");
    await node.ReceiveAppendEntryRPCResponse(response);
});

app.MapPost("/response/vote", async (VoteResponseRPC response) =>
{
    // Console.WriteLine($"received vote response {response}");
    await node.ReceiveVoteResponse(response);
});

app.MapPost("/request/command", async (clientData data) =>
{
    Console.WriteLine($"Received data: key = {data.key}, message = {data.message}");
    await node.recieveCommandFromClient(data);
});

app.Run();