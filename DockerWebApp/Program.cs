using System.Text.Json;
using DistributedTDDProject1;


var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");
 
 
var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? throw new Exception("NODE_ID environment variable not set");
var otherNodesRaw = Environment.GetEnvironmentVariable("OTHER_NODES") ?? throw new Exception("OTHER_NODES environment variable not set");
var nodeIntervalScalarRaw = Environment.GetEnvironmentVariable("NODE_INTERVAL_SCALAR") ?? throw new Exception("NODE_INTERVAL_SCALAR environment variable not set");
 
var app = builder.Build();
 
var logger = app.Services.GetService<ILogger<Program>>();
Console.WriteLine($"Node ID {nodeId}" );
Console.WriteLine($"Other nodes environment config: {otherNodesRaw}");
 
 
INode[] otherNodes = otherNodesRaw
  .Split(";")
  .Select(s => new HttpRpcOtherNode(Guid.Parse(s.Split(",")[0]), s.Split(",")[1]))
  .ToArray();
 
 
Console.WriteLine("other nodes {nodes}", JsonSerializer.Serialize(otherNodes));
 
 
var node = new Node()
{
  id = Guid.Parse(nodeId),
};

node.neighbors = otherNodes;
 
Node.NodeIntervalScalar = double.Parse(nodeIntervalScalarRaw);
 
app.MapGet("/health", () => "healthy");
 
app.MapPost("/request/appendEntries", async (AppendEntriesRequestRPC request) =>
{
  Console.WriteLine($"received append entries request {request}");
  await node.RequestAppendEntry(request);
});
 
app.MapPost("/request/vote", async (VoteRequestRPC request) =>
{
  Console.WriteLine($"received vote request {request}");
  await node.RequestVote(request);
});
 
app.MapPost("/response/appendEntries", async (AppendEntriesResponseRPC response) =>
{
  Console.WriteLine($"received append entries response {response}");
  await node.ReceiveAppendEntryRPCResponse(response);
});
 
app.MapPost("/response/vote", async (VoteResponseRPC response) =>
{
  Console.WriteLine($"received vote response {response}");
  await node.ReceiveVoteResponse(response);
});
 
app.MapPost("/request/command", async (clientData data) =>
{
  await node.recieveCommandFromClient(data);
});
 
app.Run();