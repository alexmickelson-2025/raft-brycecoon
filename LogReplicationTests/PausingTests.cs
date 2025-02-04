//using DistributedTDDProject1;
//using NSubstitute;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace LogReplicationTests;

//public class PausingTests
//{
//    //--------------PAUSING TESTS----------------//
//    [Fact]
//    public void Node_Is_Leader_With_Election_Loop_Gets_Paused_No_Heartbeats_For_400ms()
//    {
//        Node n = new Node();
//        n.state = nodeState.LEADER;
//        var fakeNode1 = Substitute.For<INode>();
//        var fakeNode2 = Substitute.For<INode>();

//        n.neighbors = [fakeNode1, fakeNode2];
//        //n.recieveCommandFromClient(0, "testingLog");
//        n.StartHeartbeat();

//        n.Pause();
//        Thread.Sleep(400);

//        //fakeNode1.Received(1).ReceiveAppendEntryRequest(n.id, 0, "testingLog");
//        //fakeNode2.Received(1).ReceiveAppendEntryRequest(n.id, 0, "testingLog");
//    }

//    [Fact]
//    public void Ditto_Except_Then_Gets_Unpaused_Heartbeats_Resume()
//    {
//        Node n = new Node();
//        n.state = nodeState.LEADER;
//        var fakeNode1 = Substitute.For<INode>();
//        var fakeNode2 = Substitute.For<INode>();

//        n.neighbors = [fakeNode1, fakeNode2];
//        //n.recieveCommandFromClient(0, "testingLog");
//        n.StartHeartbeat();

//        n.Pause();
//        Thread.Sleep(400);

//        //fakeNode1.Received(1).ReceiveAppendEntryRequest(n.id, 0, "testingLog");
//        //fakeNode2.Received(1).ReceiveAppendEntryRequest(n.id, 0, "testingLog");

//        //n.Resume();

//        //Thread.Sleep(200);
//        //fakeNode1.Received(4).ReceiveAppendEntryRequest(n.id, 0, "testingLog");
//        //fakeNode2.Received(4).ReceiveAppendEntryRequest(n.id, 0, "testingLog");
//    }

//    [Fact]
//    public void Follower_Is_Paused_Does_Not_Time_Out()
//    {

//    }

//    [Fact]
//    public void Follower_Is_Unpaused_Will_Eventually_Become_Candidate()
//    {

//    }
//}
