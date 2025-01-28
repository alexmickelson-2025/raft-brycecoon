using DistributedTDDProject1;
using FluentAssertions;
using NSubstitute;

namespace DistributedTDDTests;

public class RaftElectionTests
{
    //Test 1
    [Fact]
    public async Task Leader_Sends_Hearbeat_Within_50ms()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        node.state = nodeState.CANDIDATE;
        node.numVotesRecieved = 3;
        await node.setElectionResults();
        Assert.Equal(nodeState.LEADER, node.state);

        Thread.Sleep(50);
        await fakeNode1.Received().ReceiveAppendEntryRequest(Arg.Any<AppendEntriesRequestRPC>());
    }

    //Test 2
    [Fact]
    public async Task Recieving_Append_Entries_From_Another_Node_The_Node_Remembers_The_Leader()
    {
        Node n = new();
        Node n2 = new();
        n.neighbors = [n2];
        n2.neighbors = [n];

        Assert.Equal(0, n.term);
        Assert.Equal(0, n2.term);

        AppendEntriesRequestRPC request = new AppendEntriesRequestRPC
        {
            LeaderId = n.id
        };

        await n2.ReceiveAppendEntryRequest(request);

        Assert.Equal(n.id, n2.currentLeader);
    }

    //Test 3
    [Fact]
    public void New_Node_Starts_In_Follower_State()
    {
        Node n = new();
        Assert.Equal(nodeState.FOLLOWER, n.state);
    }


    //test 4
    [Fact]
    public void Follower_Doesnt_Get_A_Message_For_300_ms_Starts_An_Election()
    {
        Node n = new();
        Assert.Equal(nodeState.FOLLOWER, n.state);
        Thread.Sleep(301);
        n.term.Should().BeGreaterThan(0);
        Assert.NotEqual(nodeState.FOLLOWER, n.state);
    }


    //Test 5
    [Fact]
    public void When_Election_Time_Is_Reset_It_Is_A_Random_Value_Between_150_And_300()
    {
        Node n = new();
        List<long> timeoutIntervals = new List<long>();
        for (int i = 0; i < 4; i++)
        {
            Thread.Sleep(300);
            timeoutIntervals.Add(n.timeoutInterval);
        }
        Assert.True(timeoutIntervals.Distinct().Count() > 1);
    }

    //test case 6
    [Fact]
    public void Given_Candidate_UponElectionStart_Increases_Current_Term()
    {
        Node n = new();
        int beforeTerm = n.term;
        Thread.Sleep(400);
        n.term.Should().BeGreaterThan(beforeTerm);
    }

    //test 7
    [Fact]
    public async void Follower_Gets_A_Message_Within_300_ms_Does_Not_Start_An_Election()
    {
        Node n = new();
        Node n2 = new();
        n.neighbors = [n2];
        n2.neighbors = [n];
        Assert.Equal(nodeState.FOLLOWER, n.state);
        for (int i = 0; i < 5; i++)
        {
            Thread.Sleep(50);
            await n2.sendAppendRPCRequest(n);
        }
        Assert.Equal(nodeState.FOLLOWER, n.state);
    }


    //Test 8: part a
    [Fact]
    public async Task Single_Node_Majority_Vote_Becomes_Leader()
    {
        Node n = new();
        await n.startElection();
        await n.setElectionResults();
        Assert.Equal(nodeState.LEADER, n.state);
    }

    //Test 8: part b
    [Fact]
    public async Task Multiple_Node_Majority_Vote_Becomes_Leader()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        node.state = nodeState.CANDIDATE;
        await node.recieveResponseToVoteRequest(true);
        await node.recieveResponseToVoteRequest(true);
        Assert.Equal(nodeState.LEADER, node.state);
    }

    //Test 8: part c
    [Fact]
    public async Task Multiple_Node_Without_Majority_Vote_Does_Not_Become_Leader()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        await node.startElection();
        await node.recieveResponseToVoteRequest(false);
        await node.recieveResponseToVoteRequest(false);
        Assert.NotEqual(nodeState.LEADER, node.state);
    }

    //Test 9
    [Fact]
    public async Task Candidate_Recieves_Majority_Vote_Without_Every_Response_Becomes_Leader()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        await node.startElection();
        await node.recieveResponseToVoteRequest(true);
        Assert.Equal(nodeState.LEADER, node.state);
    }

    //Test 10 a
    [Fact]
    public async Task Follower_Without_Vote_Sends_AppendRPC_With_Yes()
    {
        Node node = new();
        Node node2 = new();
        Node node3 = new();
        node.neighbors = [node2, node3];
        node2.neighbors = [node, node3];
        node3.neighbors = [node, node2];

        await node.startElection();
        await node.RequestVote(node.neighbors);
        Assert.Equal(3, node.numVotesRecieved);
    }

    //Test 10: b
    [Fact]
    public async void Follower_With_Vote_Sends_AppendRPC_With_No()
    {
        Node node = new();
        Node node2 = new();
        Node node3 = new();
        node.neighbors = [node2, node3];
        node2.neighbors = [node, node3];
        node3.neighbors = [node, node2];

        node.state = nodeState.CANDIDATE;
        node2.state = nodeState.CANDIDATE;
        node.term = 1;
        node2.term = 1;

        await node.RequestVote([node3]);
        await node2.RequestVote([node3]);

        Assert.Equal(1, node.numVotesRecieved);
        Assert.Equal(0, node2.numVotesRecieved);
    }

    //Test 11
    [Fact]
    public async Task Candidate_Votes_For_Itself_In_Election()
    {
        Node n = new();
        await n.startElection();
        Assert.Equal(n.voteId, n.id);
    }


    //Test 12: a
    [Fact]
    public async Task Candidate_Loses_To_Leader_With_Later_Term()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];
        fakeNode1.term = 5;


        AppendEntriesRequestRPC request = new AppendEntriesRequestRPC
        {
            LeaderId = fakeNode1.id
        };

        await node.startElection();
        await node.ReceiveAppendEntryRequest(request);
        Assert.Equal(nodeState.FOLLOWER, node.state);
    }

    //Test 12: b
    [Fact]
    public async Task Candidate_Does_Not_Lose_To_Leader_With_Earlier_Term()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];
        fakeNode1.term = 0;


        AppendEntriesRequestRPC request = new AppendEntriesRequestRPC
        {
            LeaderId = fakeNode1.id
        };

        await node.startElection();
        await node.ReceiveAppendEntryRequest(request);
        Assert.Equal(nodeState.CANDIDATE, node.state);
    }

    //Test 13
    [Fact]
    public async Task Candidate_Loses_To_Leader_With_Equal_Term()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];
        fakeNode1.term = 1;


        AppendEntriesRequestRPC request = new AppendEntriesRequestRPC
        {
            LeaderId = fakeNode1.id
        };

        await node.startElection();
        Assert.Equal(1, node.term);
        await node.ReceiveAppendEntryRequest(request);
        Assert.Equal(nodeState.FOLLOWER, node.state);
    }

    //Test 14
    [Fact]
    public async Task Two_Vote_Requests_For_Same_Term_Does_Not_Change_Current_Vote()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];
        fakeNode1.term = 1;

        await node.RecieveVoteRequest(fakeNode1.id, 1);
        Assert.Equal(fakeNode1.id, node.voteId);
        await node.RecieveVoteRequest(fakeNode2.id, 1);
        Assert.Equal(fakeNode1.id, node.voteId);
    }

    //Test 15
    [Fact]
    public async Task Second_Vote_Request_For_Later_Term_Changes_Current_Vote()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];
        fakeNode1.term = 1;

        await node.RecieveVoteRequest(fakeNode1.id, 1);
        Assert.Equal(1, node.voteTerm);
        Assert.Equal(fakeNode1.id, node.voteId);
        await node.RecieveVoteRequest(fakeNode2.id, 2);
        Assert.Equal(2, node.voteTerm);
        Assert.Equal(fakeNode2.id, node.voteId);
    }

    //test 16
    [Fact]
    public void If_Election_Timer_Expires_New_Election_Is_Started()
    {
        Node n = new();
        int beforeTerm = n.term;
        Thread.Sleep(400);
        n.term.Should().BeGreaterThan(beforeTerm);

    }

    //Test 18
    [Fact]
    public void Append_Entries_From_Candidate_With_Previous_Term_Request_Sends_Response()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];
        fakeNode1.term = 1;

        node.term = 10;

        AppendEntriesResponseRPC response = new AppendEntriesResponseRPC
        {
            sendingNode = node.id,
            received = false
                    };

        fakeNode1.sendAppendRPCRequest(node);
        fakeNode1.recieveResponseToAppendEntryRPCRequest(response);
    }

    //Test 19
    [Fact]
    public async Task Candidate_Wins_An_Election_Sends_A_Heartbeat()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        node.state = nodeState.CANDIDATE;
        node.numVotesRecieved = 3;
        await node.setElectionResults();
        Assert.Equal(nodeState.LEADER, node.state);

        Thread.Sleep(10);
        await fakeNode1.Received().ReceiveAppendEntryRequest(Arg.Any<AppendEntriesRequestRPC>());
    }
}
