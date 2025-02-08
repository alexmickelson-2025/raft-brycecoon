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
        fakeNode1.Received().RequestAppendEntry(Arg.Any<AppendEntriesRequestRPC>());
    }

    //Test 2
    [Fact]
    public void Recieving_Append_Entries_From_Another_Node_The_Node_Remembers_The_Leader()
    {
        Node n = new();
        var fakeNode1 = Substitute.For<INode>();
        n.neighbors = [fakeNode1];

        AppendEntriesRequestRPC request = new AppendEntriesRequestRPC
        {
            LeaderId = fakeNode1.id,
            Term = 1000
        };

        n.RequestAppendEntry(request);

        Assert.Equal(fakeNode1.id, n.currentLeader);
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
    public void Follower_Gets_A_Message_Within_300_ms_Does_Not_Start_An_Election()
    {
        Node n = new();
        INode n2 = Substitute.For<INode>();
        n.neighbors = [n2];
        Assert.Equal(nodeState.FOLLOWER, n.state);

        for (int i = 0; i < 5; i++)
        {
            Thread.Sleep(50);
            n.RequestAppendEntry(new AppendEntriesRequestRPC { LeaderId = n2.id});
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
        node.ReceiveVoteResponse(new VoteResponseRPC { response = true });
        node.ReceiveVoteResponse(new VoteResponseRPC { response = true });
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
        node.ReceiveVoteResponse(new VoteResponseRPC { response = false });
        node.ReceiveVoteResponse(new VoteResponseRPC { response = false });
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
        node.ReceiveVoteResponse(new VoteResponseRPC { response = true });
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
        node.RequestVoteFromEachNeighbor();
        Assert.Equal(3, node.numVotesRecieved);
    }

    //Test 10: b
    [Fact]
    public void Follower_With_Vote_Sends_AppendRPC_With_No()
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

        node3.RequestVote(new VoteRequestRPC {candidateId = node.id, candidateTerm = node.term});
        node3.RequestVote(new VoteRequestRPC { candidateId = node2.id, candidateTerm = node.term });

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
        node.neighborNextIndexes[fakeNode1.id] = 0;

        AppendEntriesRequestRPC request = new AppendEntriesRequestRPC
        {
            LeaderId = fakeNode1.id,
            Term = 10
        };

        node.startElection();
        node.RequestAppendEntry(request);
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


        AppendEntriesRequestRPC request = new AppendEntriesRequestRPC
        {
            LeaderId = fakeNode1.id,
            Term = 0
        };

        node.startElection();
        node.RequestAppendEntry(request);
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
        node.neighborNextIndexes[fakeNode1.id] = 1;

        AppendEntriesRequestRPC request = new AppendEntriesRequestRPC
        {
            LeaderId = fakeNode1.id,
            Term = 10
        };

        node.startElection();
        node.RequestAppendEntry(request);
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

        node.RequestVote(new VoteRequestRPC { candidateId = fakeNode1.id, candidateTerm = 1 });
        Assert.Equal(fakeNode1.id, node.voteId);
        node.RequestVote(new VoteRequestRPC { candidateId = fakeNode2.id, candidateTerm = 1 });
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

        node.RequestVote(new VoteRequestRPC { candidateId = fakeNode1.id, candidateTerm = 1 });
        Assert.Equal(fakeNode1.id, node.voteId);
        node.RequestVote(new VoteRequestRPC { candidateId = fakeNode2.id, candidateTerm = 2 });
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

    //test 17
    [Fact]
    public void follower_Receives_Request_sends_Reponse()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        node.neighbors = [fakeNode1];

        AppendEntriesRequestRPC request = new AppendEntriesRequestRPC
        {
            LeaderId = fakeNode1.id,
            Term = 10
        };

        node.RequestAppendEntry(request);
        fakeNode1.Received().ReceiveAppendEntryRPCResponse(Arg.Any<AppendEntriesResponseRPC>());
    }

    //Test 18
    [Fact]
    public void Append_Entries_From_Candidate_With_Previous_Term_Request_Sends_Response()
    {
        Node node = new();
        var fakeNode1 = Substitute.For<INode>();
        var fakeNode2 = Substitute.For<INode>();
        node.neighbors = [fakeNode1, fakeNode2];

        node.term = 10;

        AppendEntriesResponseRPC response = new AppendEntriesResponseRPC
        {
            sendingNode = node.id,
            received = false
        };

        fakeNode1.RequestAppendEntry(new AppendEntriesRequestRPC { LeaderId = node.id });
        fakeNode1.ReceiveAppendEntryRPCResponse(response);
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
        fakeNode1.Received().RequestAppendEntry(Arg.Any<AppendEntriesRequestRPC>());
    }
}
