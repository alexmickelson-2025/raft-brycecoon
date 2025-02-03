using DistributedTDDProject1;

public class HttpRpcOtherNode : INode
{
  public Guid id { get; set;}
  public string Url { get; }
  private HttpClient client = new();
 
  public HttpRpcOtherNode(Guid id, string url)
  {
    this.id = id;
    this.Url = url;
  }
 
  public async Task RequestAppendEntry(AppendEntriesRequestRPC request)
  {
    try
    {
      await client.PostAsJsonAsync(Url + "/request/appendEntries", request);
    }
    catch (HttpRequestException)
    {
      Console.WriteLine($"node {Url} is down");
    }
  }
 
  public async Task RequestVote(VoteRequestRPC request)
  {
    try
    {
      await client.PostAsJsonAsync(Url + "/request/vote", request);
    }
    catch (HttpRequestException)
    {
      Console.WriteLine($"node {Url} is down");
    }
  }
 
  public async Task ReceiveAppendEntryRPCResponse(AppendEntriesResponseRPC response)
  {
    try
    {
      await client.PostAsJsonAsync(Url + "/response/appendEntries", response);
    }
    catch (HttpRequestException)
    {
      Console.WriteLine($"node {Url} is down");
    }
  }
 
  public async Task ReceiveVoteResponse(VoteResponseRPC response)
  {
    try
    {
      await client.PostAsJsonAsync(Url + "/response/vote", response);
    }
    catch (HttpRequestException)
    {
      Console.WriteLine($"node {Url} is down");
    }
  }
 
  public async Task SendCommand(clientData data)
  {
    await client.PostAsJsonAsync(Url + "/request/command", data);
  }
}