using DistributedTDDProject1;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LogReplicationTests;

public class LogReplicationTests
{
    //Test 1
    [Fact]
    public async Task Leader_Recieves_Client_Command_Sends_The_Log_To_All_Nodes()
    {
        Node n = new Node();
        n.state = nodeState.LEADER;
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();

        n.neighbors = [fakeNode1, fakeNode2];
        n.recieveCommandFromClient(new clientData{ key = "wow", message = "testingLog" });
          n.StartHeartbeat();

        Thread.Sleep(100);

        Assert.Single(n.logs);
        Assert.Equal("testingLog", n.logs[0].message);

          fakeNode1.Received().RequestAppendEntry(Arg.Is<AppendEntriesRequestRPC>(rpc =>
            rpc.PrevLogIndex == 0 &&
            rpc.PrevLogTerm == 0 &&
            rpc.Entries.SequenceEqual(new List<Log> { n.logs[0] })));
    }

    //Test 2
    [Fact]
    public void Leader_Recieves_Command_From_Client_Gets_Appended_To_Log()
    {
        var node = new Node();
        node.recieveCommandFromClient(new clientData { key = "asdf", message = "testingLog" });
        Assert.Equal("testLog", node.logs[0].message);
    }

    //Test 3
    [Fact]
    public void New_Node_Has_Empty_Log()
    {
        Node n = new();
        Assert.Empty(n.logs);
    }

    //Test 4
    [Fact]
    public async Task Leader_Wins_Election_Initializes_Next_Index_For_Each_Follower()
    {
        var node = new Node();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        node.nextIndex = 5;

        node.state = nodeState.CANDIDATE;
        node.numVotesRecieved = 3;
          node.setElectionResults();
        Assert.Equal(nodeState.LEADER, node.state);

        Assert.Equal(5, node.neighborNextIndexes[fakeNode1.id]);
        Assert.Equal(5, node.neighborNextIndexes[fakeNode2.id]);
    }

    //Test 5
    [Fact]
    public async Task Leaders_Maintain_nextIndex_That_Is_Index_Of_Next_Log_Entry_That_Will_Be_Sent()
    {
        var node = new Node();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        AppendEntriesResponseRPC rpcResponse = new AppendEntriesResponseRPC
        {
            sendingNode = fakeNode1.id,
            received = true,
            followerHighestReceivedIndex = 2
        };

        node.recieveCommandFromClient(new clientData { key = "asdf", message = "testingLog" });
        node.recieveCommandFromClient(new clientData { key = "asdf", message = "testingLog" });
        node.highestCommittedLogIndex = 1;
          node.ReceiveAppendEntryRPCResponse(rpcResponse);

        node.state = nodeState.CANDIDATE;
        node.numVotesRecieved = 3;
          node.setElectionResults();
        Thread.Sleep(100);

        Assert.Equal(1, node.neighborNextIndexes[fakeNode1.id]);
    }

    //Test 6
    [Fact]
    public async Task Highest_Committed_Index_From_Leader_Is_Included_In_AppendRPC()
    {
        Node n = new Node();
        n.state = nodeState.LEADER;
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();

        n.neighbors = [fakeNode1, fakeNode2];

        n.state = nodeState.CANDIDATE;
        n.numVotesRecieved = 3;
          n.setElectionResults();

        n.recieveCommandFromClient(new clientData { key = "wow", message = "testingLog" });
        n.highestCommittedLogIndex = 0;
        n.term = 2;
        n.highestCommittedLogIndex = 10;
          n.StartHeartbeat();

        Thread.Sleep(100);

        Assert.Single(n.logs);
        Assert.Equal("testingLog", n.logs[0].message);

          fakeNode1.Received().RequestAppendEntry(Arg.Is<AppendEntriesRequestRPC>(rpc =>
            rpc.PrevLogIndex == 0 &&
            rpc.PrevLogTerm == 0 &&
            rpc.Term == 2 &&
            rpc.leaderHighestLogCommitted == 10
            ));
    }

    //Test 7
    [Fact]
    public async Task Follower_Learns_Log_Is_Committed_Applies_To_Local_State_Machine()
    {
        Node n = new Node();
        var fakeNode1 = Substitute.For<INode>();
        n.neighbors = [n, fakeNode1];

        AppendEntriesRequestRPC rpc = new AppendEntriesRequestRPC
        {
            LeaderId = n.id,
            Term = 5,
            PrevLogIndex = 0,
            PrevLogTerm = 2,
            leaderHighestLogCommitted = -1,
            Entries = [new Log { key = "asdf", message = "wowee", term = 2 }]
        };

          n.RequestAppendEntry(rpc);
        Assert.Single(n.logs);
        Assert.Equal(-1, n.highestCommittedLogIndex);

        rpc.leaderHighestLogCommitted = 0;
          n.RequestAppendEntry(rpc);
        Assert.Single(n.stateMachine);
        Assert.Equal(0, n.highestCommittedLogIndex);
    }

    //Test 8
    [Fact]
    public async Task Leader_Recieves_Majority_Confirmation_Of_A_Log_Commits_It()
    {
        Node n = new Node();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        n.neighbors = [fakeNode1, fakeNode2];

        n.recieveCommandFromClient(new clientData { key = "wow", message = "testingLog" });
        Assert.Equal(-1, n.highestCommittedLogIndex);
        Assert.Equal(0, n.prevIndex);
        Assert.Equal("testLog", n.logs[0].message);

        AppendEntriesResponseRPC response = new AppendEntriesResponseRPC
        {
            sendingNode = fakeNode1.id,
            received = true,
            followerHighestReceivedIndex = -1
        };

          n.ReceiveAppendEntryRPCResponse(response);

        Assert.Equal(-1, n.highestCommittedLogIndex);
        Assert.Equal(1, n.LogToTimesReceived[0]);

        response.sendingNode = fakeNode2.id;
          n.ReceiveAppendEntryRPCResponse(response);

        Assert.Equal(0, n.highestCommittedLogIndex);
        Assert.Equal(2, n.LogToTimesReceived[0]);

    }

    //Test 9
    [Fact]
    public async Task Leader_Commits_Logs_By_Incrementing_Committed_Log_Index()
    {
        Node n = new Node();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        n.neighbors = [fakeNode1, fakeNode2];

        n.recieveCommandFromClient(new clientData { key = "wow", message = "testingLog" });

        AppendEntriesResponseRPC response = new AppendEntriesResponseRPC
        {
            sendingNode = fakeNode1.id,
            received = true,
            followerHighestReceivedIndex = -1
        };

          n.ReceiveAppendEntryRPCResponse(response);

        Assert.Equal(-1, n.highestCommittedLogIndex);
        Assert.Equal(1, n.LogToTimesReceived[0]);

        response.sendingNode = fakeNode2.id;
          n.ReceiveAppendEntryRPCResponse(response);

        Assert.Equal(0, n.highestCommittedLogIndex);
        Assert.Equal(2, n.LogToTimesReceived[0]);
    }

    //Test 10
    [Fact]
    public async Task Follower_Recieves_AppendEntry_With_Log_Will_Add_To_Personal_Log()
    {
        Node n = new Node();
        n.neighbors = [n];

        AppendEntriesRequestRPC rpc = new AppendEntriesRequestRPC
        {
            LeaderId = n.id,
            Entries = [new Log { key = "asdf", message = "wowee", term = 2 }]
        };
          n.RequestAppendEntry(rpc);

        Assert.Single(n.logs);
        Assert.Equal("wowee", n.logs[0].message);
    }

    //Test 11
    [Fact]
    public async Task Response_To_AppendEntry_Includes_The_Followers_Term_And_Entry_Index()
    {
        var fakeNode1 = Substitute.For<INode>();

        AppendEntriesResponseRPC request = new AppendEntriesResponseRPC
        {
            term = 2,
            followerHighestReceivedIndex = 5
        };

          fakeNode1.ReceiveAppendEntryRPCResponse(request);
          fakeNode1.Received().ReceiveAppendEntryRPCResponse(Arg.Is<AppendEntriesResponseRPC>(rpc =>
            rpc.term == 2 &&
            rpc.followerHighestReceivedIndex == 5
        ));
    }

    //Test 12
    //[Fact]
    //public async Task Leader_Commits_Sends_Response_To_Client()
    //{
    //    Node n = new Node();
    //    var fakeNode1 = Substitute.For<INode>();
    //    var fakeNode2 = Substitute.For<INode>();
    //    var fakeClient = Substitute.For<INode>();
    //    n.neighbors = [fakeNode1, fakeNode2];
    //    n.Client = fakeClient;

    //    n.recieveCommandFromClient("wow", "testLog");


    //    AppendEntriesResponseRPC response = new AppendEntriesResponseRPC
    //    {
    //        sendingNode = fakeNode1.id,
    //        received = true,
    //        followerHighestReceivedIndex = -1
    //    };

    //      n.ReceiveAppendEntryRPCResponse(response);
    //    response.sendingNode = fakeNode2.id;
    //      n.ReceiveAppendEntryRPCResponse(response);
    //      fakeClient.Received().ReceiveClientResponse(Arg.Any<ClientResponseArgs>());
    //}

    //Test 13
    [Fact]
    public async Task Leader_Node_Commits_Log_Goes_To_Internal_State_Machine()
    {
        Node n = new Node();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        n.neighbors = [fakeNode1, fakeNode2];

        n.recieveCommandFromClient(new clientData { key = "wow", message = "testingLog" });

        AppendEntriesResponseRPC response = new AppendEntriesResponseRPC
        {
            sendingNode = fakeNode1.id,
            received = true,
            followerHighestReceivedIndex = -1
        };

          n.ReceiveAppendEntryRPCResponse(response);
        response.sendingNode = fakeNode2.id;
          n.ReceiveAppendEntryRPCResponse(response);

        Assert.Equal(0, n.highestCommittedLogIndex);
        Assert.Equal(2, n.LogToTimesReceived[0]);
        Assert.Equal("testLog", n.stateMachine["wow"]);
    }

    //Test 14: a
    [Fact]
    public async Task Follower_Receives_Heartbeat_Increases_Commit_Index_To_Match()
    {
        Node n = new Node();
        n.neighbors = [n];

        AppendEntriesRequestRPC rpc = new AppendEntriesRequestRPC
        {
            LeaderId = n.id,
            PrevLogIndex = 0,
            PrevLogTerm = 2,
            leaderHighestLogCommitted = 0,
            Entries = [new Log { key = "asdf", message = "wowee", term = 2 }]
        };

          n.RequestAppendEntry(rpc);

        Assert.Equal(0, n.highestCommittedLogIndex);
    }

    //Test 14: b
    [Fact]
    public async Task Follower_Receives_Invalid_Heartbeat_Rejects_It()
    {
        Node n = new Node();
        n.highestCommittedLogIndex = 2;
        n.logs.Add(new Log { term = 1, message = "Log1" });
        n.neighbors = [n];

        AppendEntriesRequestRPC rpc = new AppendEntriesRequestRPC
        {
            
            leaderHighestLogCommitted = 5,
            LeaderId = n.id
        };

          n.RequestAppendEntry(rpc);

        Assert.Equal(2, n.highestCommittedLogIndex);
    }

    //Test 15: 1.1 A
    [Fact]
    public async Task Follower_Rejects_New_Entries_If_Term_Is_Not_Same_Or_Newer()
    {
        Node n = new Node();
        n.neighbors = [n];
        n.neighborNextIndexes[n.id] = 0;


        AppendEntriesRequestRPC rpc = new AppendEntriesRequestRPC
        {
            leaderHighestLogCommitted = 5,
            LeaderId = n.id,
            PrevLogIndex = 0,
            Term = 1,
            Entries = [new Log { key = "asdf", message = "wowee", term = 1 }]
        };

        n.term = 8;
          n.RequestAppendEntry(rpc);

        Assert.Empty(n.logs);
    }

    //Test 15: 1.1 B
    [Fact]
    public async Task Follower_Accepts_New_Entries_If_Term_Is_Same_Or_Newer()
    {
        Node n = new Node();
        n.neighbors = [n];

        AppendEntriesRequestRPC rpc = new AppendEntriesRequestRPC
        {
            leaderHighestLogCommitted = 5,
            LeaderId = n.id,
            PrevLogIndex = 0,
            Term = 2,
            Entries = [new Log { key = "asdf", message = "wowee", term = 2 }]
        };

        n.term = 2;
          n.RequestAppendEntry(rpc);

        Assert.NotEmpty(n.logs);
    }

    //Test 15: 1.2 And 2.
    [Fact]
    public async Task Follower_Rejects_New_Entries_Index_Is_Greater_Decreased_By_Leader()
    {
        Node n = new Node();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        n.neighbors = [fakeNode1, fakeNode2];

        AppendEntriesResponseRPC response = new AppendEntriesResponseRPC
        {
            sendingNode = fakeNode1.id,
            followerHighestReceivedIndex = 1,
            received = false
        };

        n.neighborNextIndexes[fakeNode1.id] = 8;
          n.ReceiveAppendEntryRPCResponse(response);

        Assert.Equal(7, n.neighborNextIndexes[fakeNode1.id]);
    }

    //Test 15: 1.3
    [Fact]
    public async Task Follower_Rejects_New_Entries_Index_Is_Less_Delete_What_We_Have()
    {
        Node n = new Node();
        n.neighbors = [n];
        n.neighborNextIndexes[n.id] = 0;


        n.logs.Add(new Log { key = "wow", message = "testingLog" });
        n.logs.Add(new Log { key = "wowee", message = "testingLog2" });
        n.logs.Add(new Log { key = "wowzers", message = "testingLog3" });
        Assert.Equal(3, n.logs.Count);

        AppendEntriesRequestRPC rpc = new AppendEntriesRequestRPC
        {
            leaderHighestLogCommitted = 5,
            LeaderId = n.id,
            PrevLogIndex = 0,
            PrevLogTerm = 3,
            Term = 3,
        };

        n.term = 2;
        //n.prevIndex = 2;
          n.RequestAppendEntry(rpc);

        Assert.Single(n.logs);
    }

    //Test 16
    [Fact]
    public async Task Leader_Sends_HeartBeat_With_Log_Does_Not_Receive_Majority_Is_Uncommitted()
    {
        Node n = new Node();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        n.neighbors = [fakeNode1, fakeNode2];

        n.recieveCommandFromClient(new clientData { key = "wow", message = "testingLog" });
        Assert.Equal(-1, n.highestCommittedLogIndex);
        Assert.Equal(0, n.prevIndex);
        Assert.Equal("testLog", n.logs[0].message);

        AppendEntriesResponseRPC response = new AppendEntriesResponseRPC
        {
            sendingNode = fakeNode1.id,
            received = true,
            followerHighestReceivedIndex = -1
        };

          n.ReceiveAppendEntryRPCResponse(response);

        Assert.Equal(-1, n.highestCommittedLogIndex);
        Assert.Equal(1, n.LogToTimesReceived[0]);
        Assert.False(n.stateMachine.ContainsKey("wow"));
    }

    //Test 17
    [Fact]
    public async Task Leader_Receives_No_Response_Continues_Sending_Log_Entries()
    {
        Node n = new Node();
        n.state = nodeState.LEADER;
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        n.neighborNextIndexes[n.id] = 0;


        n.neighbors = [fakeNode1, fakeNode2];

        n.state = nodeState.CANDIDATE;
        n.numVotesRecieved = 3;
          n.setElectionResults();


        n.logs.Add(new Log { key = "wow", message = "testingLog" });
        n.logs.Add(new Log { key = "wowee", message = "testingLog2" });
        n.logs.Add(new Log { key = "wowzers", message = "testingLog3" });

        Thread.Sleep(301);

        Assert.Equal(3, n.logs.Count);

          fakeNode1.Received(Quantity.Within(6, 20)).RequestAppendEntry(Arg.Is<AppendEntriesRequestRPC>(rpc =>
            rpc.Entries[0].key == "wow" &&
            rpc.Entries[0].message == "testingLog" &&
            rpc.Entries[1].key == "wowee" &&
            rpc.Entries[1].message == "testingLog2" &&
            rpc.Entries[2].key == "wowzers" &&
            rpc.Entries[2].message == "testingLog3"
            ));
    }

    //Test 18
    //[Fact]
    //public async Task Leader_Doesnt_Commit_Entry_Doesnt_Call_To_Client()
    //{
    //    Node n = new Node();
    //    var fakeNode1 = Substitute.For<INode>();
    //    var fakeNode2 = Substitute.For<INode>();
    //    var fakeClient = Substitute.For<INode>();
    //    n.neighbors = [fakeNode1, fakeNode2];
    //    n.Client = fakeClient;
    //    n.neighborNextIndexes[fakeNode1.id] = 0;


    //    n.recieveCommandFromClient("wow", "testLog");


    //    AppendEntriesResponseRPC response = new AppendEntriesResponseRPC
    //    {
    //        sendingNode = fakeNode1.id,
    //        received = false,
    //        followerHighestReceivedIndex = -1
    //    };

    //      n.ReceiveAppendEntryRPCResponse(response);

    //      fakeClient.DidNotReceive().ReceiveClientResponse(Arg.Any<ClientResponseArgs>());
    //}

    //Test 19
    [Fact]
    public async Task Node_Receives_AppendEntry_With_Logs_Too_Far_In_Future_Rejects_Logs()
    {
        Node n = new Node();
        n.neighbors = [n];
        n.neighborNextIndexes[n.id] = 0;

        AppendEntriesRequestRPC rpc = new AppendEntriesRequestRPC
        {
            leaderHighestLogCommitted = 5,
            LeaderId = n.id,
            PrevLogIndex = 4,
            Entries = [new Log { key = "asdf", message = "wowee", term = 2 }]
        };

          n.RequestAppendEntry(rpc);

        Assert.Empty(n.logs);
    }

    //Test 20
    //[Fact]
    //public async Task Node_Fails_Consistency_Check_Will_Reject_Until_You_Find_Matching_Log()
    //{
    //    Node n = new Node();
    //    n.neighbors = [n];
    //    n.neighborNextIndexes[n.id] = 0;

    //    AppendEntriesRequestRPC rpc = new AppendEntriesRequestRPC
    //    {
    //        leaderHighestLogCommitted = 5,
    //        LeaderId = n.id,
    //        PrevLogIndex = 10,
    //        PrevLogTerm = 3,
    //        Term = 3,
    //        Entries = [new Log { key = "asdf", message = "wowee", term = 2 }]
    //    };

    //    n.prevIndex = 2;
    //    n.term = 2;
    //      n.RequestAppendEntry(rpc);

    //    n.prevIndex = 5;
    //    Assert.Empty(n.logs);
    //      n.RequestAppendEntry(rpc);

    //    n.prevIndex = 9;
    //    Assert.Empty(n.logs);
    //      n.RequestAppendEntry(rpc);

    //    n.prevIndex = 10;
    //    Assert.Empty(n.logs);
    //      n.RequestAppendEntry(rpc);

    //    Assert.NotEmpty(n.logs);

    //}

    //Test 21
    [Fact]
    public async Task Node_Gets_As_Many_Logs_As_It_Needs()
    {

    }
}