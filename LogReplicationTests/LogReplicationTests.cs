using DistributedTDDProject1;
using NSubstitute;
using System.Collections;
using System.Collections.Generic;

namespace LogReplicationTests;

public class LogReplicationTests
{
    //Test 1
    [Fact]
    public void Leader_Recieves_Client_Command_Sends_The_Log_To_All_Nodes()
    {
        Node n = new Node();
        n.state = nodeState.LEADER;
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();

        n.neighbors = [fakeNode1, fakeNode2];
        n.recieveCommandFromClient(0, "testingLog");
        n.StartHeartbeat();

        Thread.Sleep(100);

        Assert.Equal("testingLog", n.logs[0].message);
        fakeNode1.Received().ReceiveAppendEntryRequest(n.id, 0, "testingLog");
        fakeNode2.Received().ReceiveAppendEntryRequest(n.id, 0, "testingLog");
    }

    //Test 2
    [Fact]
    public void Leader_Recieves_Command_From_Client_Gets_Appended_To_Log()
    {
        var node = new Node();
        node.recieveCommandFromClient(1, "testLog");
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
    public void Leader_Wins_Election_Initializes_Next_Index_For_Each_Follower()
    {
        var node = new Node();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        node.ReceiveAppendEntryRequest(node.id, 0, "Hello");
        node.ReceiveAppendEntryRequest(node.id, 0, "Testing");

        node.state = nodeState.CANDIDATE;
        node.numVotesRecieved = 3;
        node.setElectionResults();
        Assert.Equal(nodeState.LEADER, node.state);

        Assert.Equal(2, node.neighborNextIndexes[fakeNode1.id]);
        Assert.Equal(2, node.neighborNextIndexes[fakeNode2.id]);
    }

    //Test 5
    [Fact]
    public void Leaders_Maintain_nextIndex_That_Is_Index_Of_Next_Log_Entry_That_Will_Be_Sent()
    {
        var node = new Node();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        node.ReceiveAppendEntryRequest(node.id, 0, "Hello");
        node.ReceiveAppendEntryRequest(node.id, 0, "Testing");

        node.state = nodeState.CANDIDATE;
        node.numVotesRecieved = 3;
        node.setElectionResults();

        Assert.Equal(2, node.neighborNextIndexes[fakeNode1.id]);
        Assert.Equal(2, node.neighborNextIndexes[fakeNode2.id]);
    }

    //Test 6
    [Fact]
    public void Highest_Committed_Index_From_Leader_Is_Included_In_AppendRPC()
    {
        Node node = new Node();
        var fakeNode1 = Substitute.For<INode>();
        node.neighbors = [fakeNode1];
        node.recieveCommandFromClient(1, "Hello");
        node.sendAppendRPCRequest(fakeNode1, "Hello");
        fakeNode1.Received().ReceiveAppendEntryRequest(node.id,0, "Hello");

        node.commitLogToStateMachine(1);
        node.sendAppendRPCRequest(fakeNode1, "Hello");
        fakeNode1.Received().ReceiveAppendEntryRequest(node.id, 1, "Hello");
    }

    //Test 7
    [Fact]
    public void Follower_Learns_Log_Is_Committed_Applies_To_Local_State_Machine()
    {

    }

    //Test 8: a
    [Fact]
    public void Leader_Recieves_Majority_Confirmation_Of_A_Log_Commits_It()
    {
        Node n = new Node();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        n.neighbors = [fakeNode1, fakeNode2];

        n.recieveCommandFromClient(8, "testLog");
        n.recieveResponseToAppendEntryRPCRequest(fakeNode1.id, true);  
        n.recieveResponseToAppendEntryRPCRequest(fakeNode2.id, true);

        Assert.Equal(8, n.logs[n.prevIndex].key);
        Assert.Equal(3, n.LogToTimesReceived[0]);
        Assert.Equal("testLog", n.stateMachine[1]);
    }

    //Test 8: b
    [Fact]
    public void Leader_Does_not_Recieve_Majority_Confirmation_Of_A_Log_Does_Not_Commit_It()
    {
        Node n = new Node();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        n.neighbors = [fakeNode1, fakeNode2];

        n.LogToTimesReceived[0] = 0; 
        n.recieveCommandFromClient(9, "testLog2");
        n.recieveResponseToAppendEntryRPCRequest(fakeNode1.id, false);
        n.recieveResponseToAppendEntryRPCRequest(fakeNode2.id, false);

        Assert.Equal(9, n.logs[n.prevIndex].key);
        Assert.Equal(0, n.LogToTimesReceived[0]);
        Assert.Empty(n.stateMachine);
    }

    //Test 8: c
    [Fact]
    public void Two_Out_Of_Three_Is_Still_Majority()
    {
        Node n = new Node();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        n.neighbors = [fakeNode1, fakeNode2];

        //n.LogToTimesReceived[0] = 0;
        n.recieveCommandFromClient(9, "testLog2");
        n.recieveResponseToAppendEntryRPCRequest(fakeNode1.id, true);
        n.recieveResponseToAppendEntryRPCRequest(fakeNode2.id, false);

        Assert.Equal(9, n.logs[n.prevIndex].key);
        Assert.Equal(2, n.LogToTimesReceived[0]);
        Assert.NotEmpty(n.stateMachine);
    }

    //Test 9
    [Fact]
    public void Leader_Commits_Logs_By_Incrementing_Committed_Log_Index()
    {
        Node node = new Node();
        node.recieveCommandFromClient(1, "hello");
        node.commitLogToStateMachine(1);

        Assert.Equal(1, node.highestCommittedLogIndex);
        Assert.Equal("hello", node.stateMachine[1]);
    }

    //Test 10
    [Fact]
    public void Follower_Recieves_AppendEntry_With_Log_Will_Add_To_Personal_Log()
    {
        var node = new Node();
        node.ReceiveAppendEntryRequest(node.id, 0, "Hello");
        Assert.Equal("Hello", node.logs[0].message);
    }

    //Test 13
    [Fact]
    public void Leader_Node_Commits_Log_Goes_To_Internal_State_Machine()
    {
        Node node = new Node();
        node.recieveCommandFromClient(1, "hello");
        node.commitLogToStateMachine(1);

        Assert.Equal(1, node.highestCommittedLogIndex);
        Assert.Equal("hello", node.stateMachine[1]);
    }
}