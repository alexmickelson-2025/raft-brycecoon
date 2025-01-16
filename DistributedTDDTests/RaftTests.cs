using DistributedTDDProject1;
using FluentAssertions;
using NSubstitute;

namespace DistributedTDDTests;

public class RaftTests
{
    //Test 2
    [Fact]
    public void Recieving_Append_Entried_From_Another_Node_The_Node_Remembers_The_Leader()
    {
        Node n = new();
        Node n2 = new();
        n.sendAppendRPC(n2);
        Assert.Equal(n.id, n2.currentLeader);
    }

    //Test 3
    [Fact]
    public void New_Node_Starts_In_Follower_State()
    {
        Node n = new();
        Assert.Equal(nodeState.FOLLOWER, n.state);
    }

    //Test 5
    [Fact]
    public void When_Election_Time_Is_Reset_It_Is_Random_Value_Between_150_And_300()
    {
        Node n = new();
        List<long> timeoutIntervals = new List<long>();
        for(int i = 0; i < 4; i++)
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
        Thread.Sleep(300);
        n.term.Should().BeGreaterThan(beforeTerm);
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
        Node n = new();
        Node n2 = new();
        Node n3 = new();
        n.neighbors = [n2,n3];

        n.startElection();
        n.requestVote(n.neighbors);
        n.setElectionResults();
        Assert.Equal(nodeState.LEADER, n.state);
    }

    //Test 8: part c
    [Fact]
    public void Multiple_Node_Without_Majority_Vote_Does_Not_Become_Leader()
    {
        Node n = new();
        Node n2 = new();
        Node n3 = new();
        n.neighbors = [n2, n3];

        n.startElection();
        Assert.NotEqual(n2.voteId, n.id);
        Assert.NotEqual(n3.voteId, n.id);
        n.setElectionResults();
        Assert.Equal(nodeState.CANDIDATE, n.state);
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
        Node n = new();
        Node n2 = new();

        n.startElection();
        n.startElection();
        Assert.Equal(nodeState.CANDIDATE, n.state);

        n2.startElection();

        n.sendAppendRPC(n2);
        Assert.Equal(nodeState.FOLLOWER, n2.state);
    }

    //Test 12: b, inverse of a
    [Fact]
    public void Candidate_Does_Not_Lose_To_Leader_With_Earlier_Term()
    {
        Node n = new();
        Node n2 = new();

        n2.startElection();
        n.sendAppendRPC(n2);
        Assert.Equal(nodeState.CANDIDATE, n2.state);
    }

    //Test 13
    [Fact]
    public void Candidate_Loses_To_Leader_With_Equal_Term()
    {
        Node n = new();
        Node n2 = new();

        n.startElection();
        Assert.Equal(nodeState.CANDIDATE, n.state);

        n2.startElection();
        n.sendAppendRPC(n2);
        Assert.Equal(nodeState.FOLLOWER, n2.state);
    }

    //Test 14
    [Fact]
    public void Two_Vote_Requests_For_Same_Term_Does_Not_Change_Current_Vote()
    {
        Node n = new();
        Node n2 = new();
        Node n3 = new();
        Node[] nodes = { n3 };

        n.startElection();
        n2.startElection();

        n.requestVote(nodes);
        Assert.Equal(n.id, n3.voteId);
        Assert.Equal(1, n3.voteTerm);
        n2.requestVote(nodes);
        Assert.Equal(1, n2.term);
        Assert.Equal(n.id, n3.voteId);
    }

    //Test 15
    [Fact]
    public void Second_Vote_Request_For_Later_Term_Changes_Current_Vote()
    {
        Node n = new();
        Node n2 = new();
        Node n3 = new();
        Node[] nodes = { n3 };

        n.startElection();
        n2.startElection();
        n2.startElection();

        n.requestVote(nodes);
        Assert.Equal(n.id, n3.voteId);
        Assert.Equal(1, n3.voteTerm);
        n2.requestVote(nodes);
        Assert.Equal(n2.id, n3.voteId);
    }

    //Test 17
    [Fact]
    public void Append_Entries_Request_Sends_Response()
    {
        Node n = new();
        Node n2 = new();

        string RPCResponse = n.sendAppendRPC(n2);
        Assert.Equal("recieved", RPCResponse);
    }

    //Test 18
    [Fact]
    public void Append_Entries_From_Candidate_With_Previous_Term_Request_Sends_Response()
    {
        Node n = new();
        Node n2 = new();
        n2.startElection();

        string RPCResponse = n.sendAppendRPC(n2);
        Assert.Equal("rejected", RPCResponse);
    }

    //Test 1
}