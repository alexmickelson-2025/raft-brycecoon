using DistributedTDDProject1;
using FluentAssertions;
using NSubstitute;

namespace DistributedTDDTests;

public class RaftElectionTests
{
    //Test 1
    [Fact]
    public void Leader_Sends_Hearbeat_Within_50ms()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        node.state = nodeState.CANDIDATE;
        node.numVotesRecieved = 3;
        node.setElectionResults();
        Assert.Equal(nodeState.LEADER, node.state);

        Thread.Sleep(50);
        fakeNode1.Received().ReceiveAppendEntryRequest(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<string>());
    }

    //Test 2
    [Fact]
    public void Recieving_Append_Entries_From_Another_Node_The_Node_Remembers_The_Leader()
    {
        Node n = new();
        Node n2 = new();
        n.neighbors = [n2];
        n2.neighbors = [n];

        Assert.Equal(0, n.term);
        Assert.Equal(0, n2.term);

        n2.ReceiveAppendEntryRequest(n.id, 0, "");

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
        Assert.Equal(timeoutIntervals.Distinct().Count(), timeoutIntervals.Count());
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
            await n2.sendAppendRPCRequest(n, "");
        }
        Assert.Equal(nodeState.FOLLOWER, n.state);
    }


    //Test 8: part a
    [Fact]
    public void Single_Node_Majority_Vote_Becomes_Leader()
    {
        Node n = new();
        n.startElection();
        n.setElectionResults();
        Assert.Equal(nodeState.LEADER, n.state);
    }

    //Test 8: part b
    [Fact]
    public void Multiple_Node_Majority_Vote_Becomes_Leader()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        node.state = nodeState.CANDIDATE;
        node.recieveResponseToVoteRequest(true);
        node.recieveResponseToVoteRequest(true);
        Assert.Equal(nodeState.LEADER, node.state);
    }

    //Test 8: part c
    [Fact]
    public void Multiple_Node_Without_Majority_Vote_Does_Not_Become_Leader()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        node.startElection();
        node.recieveResponseToVoteRequest(false);
        node.recieveResponseToVoteRequest(false);
        Assert.NotEqual(nodeState.LEADER, node.state);
    }

    //Test 9
    [Fact]
    public void Candidate_Recieves_Majority_Vote_Without_Every_Response_Becomes_Leader()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        node.startElection();
        node.recieveResponseToVoteRequest(true);
        Assert.Equal(nodeState.LEADER, node.state);
    }

    //Test 10 a
    [Fact]
    public void Follower_Without_Vote_Sends_AppendRPC_With_Yes()
    {
        Node node = new();
        Node node2 = new();
        Node node3 = new();
        node.neighbors = [node2, node3];
        node2.neighbors = [node, node3];
        node3.neighbors = [node, node2];

        node.startElection();
        node.RequestVote(node.neighbors);
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
    public void Candidate_Votes_For_Itself_In_Election()
    {
        Node n = new();
        n.startElection();
        Assert.Equal(n.voteId, n.id);
    }


    //Test 12: a
    [Fact]
    public void Candidate_Loses_To_Leader_With_Later_Term()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];
        fakeNode1.term = 5;

        node.startElection();
        node.ReceiveAppendEntryRequest(fakeNode1.id, 1, "");
        Assert.Equal(nodeState.FOLLOWER, node.state);
    }

    //Test 12: b
    [Fact]
    public void Candidate_Does_Not_Lose_To_Leader_With_Earlier_Term()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];
        fakeNode1.term = 0;

        node.startElection();
        node.ReceiveAppendEntryRequest(fakeNode1.id, 1, "");
        Assert.Equal(nodeState.CANDIDATE, node.state);
    }

    //Test 13
    [Fact]
    public void Candidate_Loses_To_Leader_With_Equal_Term()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];
        fakeNode1.term = 1;

        node.startElection();
        Assert.Equal(1, node.term);
        node.ReceiveAppendEntryRequest(fakeNode1.id, 1, "");
        Assert.Equal(nodeState.FOLLOWER, node.state);
    }

    //Test 14
    [Fact]
    public void Two_Vote_Requests_For_Same_Term_Does_Not_Change_Current_Vote()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];
        fakeNode1.term = 1;

        node.RecieveVoteRequest(fakeNode1.id, 1);
        Assert.Equal(fakeNode1.id, node.voteId);
        node.RecieveVoteRequest(fakeNode2.id, 1);
        Assert.Equal(fakeNode1.id, node.voteId);
    }

    //Test 15
    [Fact]
    public void Second_Vote_Request_For_Later_Term_Changes_Current_Vote()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];
        fakeNode1.term = 1;

        node.RecieveVoteRequest(fakeNode1.id, 1);
        Assert.Equal(1, node.voteTerm);
        Assert.Equal(fakeNode1.id, node.voteId);
        node.RecieveVoteRequest(fakeNode2.id, 2);
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

        fakeNode1.sendAppendRPCRequest(node, "");
        fakeNode1.recieveResponseToAppendEntryRPCRequest(node.id, false);
    }

    //Test 19
    [Fact]
    public void Candidate_Wins_An_Election_Sends_A_Heartbeat()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        node.state = nodeState.CANDIDATE;
        node.numVotesRecieved = 3;
        node.setElectionResults();
        Assert.Equal(nodeState.LEADER, node.state);

        Thread.Sleep(10);
        fakeNode1.Received().ReceiveAppendEntryRequest(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<string>());
    }
}
