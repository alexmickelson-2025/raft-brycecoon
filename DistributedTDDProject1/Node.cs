namespace DistributedTDDProject1;

public class Node
{
    public bool vote;
    public int term;

    public Node()
    {
        term = 0;
    }

    public void startElection()
    {
        term++;
    }
}
