using DistributedTDDProject1;

namespace DistributedTDDTests;
//test case 3
public class UnitTest1
{
    [Fact]
    public void Given_Candidate_UponElectionStart_Increases_Current_Term()
    {
        Node n = new();
        n.startElection();

        Assert.Equal(1, n.term);
    }
}