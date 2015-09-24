using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SumoWCFTest
{
    [TestClass]
    public class SumoWCFUnitTest
    {
        // NOTE: These tests assume that the endpoint where the WCF service is hosted is
        // reachable and that SUMO is set properly in order to run with the services that 
        // are tested here.

        [TestMethod]
        public void InitializeSimulation_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            int result;

            client.Open();

            client.EndSimulation();

            result = client.InitializeSimulation();
            Assert.AreEqual(0, result, "InitializeSimulation invalid behaviour in first time call");

            result = client.InitializeSimulation();
            Assert.AreEqual(-1, result, "InitializeSimulation invalid behaviour in second time call");

            client.Close();
        }

        [TestMethod]
        public void EndSimulation_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            int result;

            client.Open();

            client.InitializeSimulation();

            result = client.EndSimulation();
            Assert.AreEqual(0, result, "EndSimulation invalid behaviour in first time call");

            result = client.EndSimulation();
            Assert.AreEqual(-1, result, "EndSimulation invalid behaviour in second time call");

            client.Close();
        }

        [TestMethod]
        public void RestartSimulation_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            int result;

            client.Open();

            client.InitializeSimulation();
            result = client.RestartSimulation();
            Assert.AreEqual(0, result, "RestartSimulation invalid behaviour after the simulation was already initialized");

            client.EndSimulation();
            result = client.RestartSimulation();
            Assert.AreEqual(0, result, "RestartSimulation invalid behaviour after the simulation was ended");

            client.Close();
        }

        [TestMethod]
        public void RunSingleStep_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            int result;

            client.Open();

            client.RestartSimulation();

            for (int i = 0; i < 100; i++)
            {
                result = client.RunSingleStep();
                Assert.AreEqual(0, result, "RunSingleStep " + i + "invalid behaviour");
            }

            client.EndSimulation();
            result = client.RunSingleStep();
            Assert.AreEqual(-1, result, "RunSingleStep after end simulation invalid behaviour");

            client.Close();
        }

        [TestMethod]
        public void RunElapsedTime_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            int result;

            client.Open();

            client.RestartSimulation();

            result = client.RunElapsedTime(200);
            Assert.AreEqual(0, result, "RunSingleStep 200 invalid behaviour");

            result = client.RunElapsedTime(800);
            Assert.AreEqual(0, result, "RunSingleStep 800 invalid behaviour");

            result = client.RunElapsedTime(1000);
            Assert.AreEqual(0, result, "RunElapsedTime 1000 invalid behaviour");

            result = client.RunElapsedTime(10000);
            Assert.AreEqual(0, result, "RunElapsedTime 10000 invalid behaviour");

            result = client.RunElapsedTime(100000);
            Assert.AreEqual(0, result, "RunElapsedTime 100000 invalid behaviour");

            client.EndSimulation();
            result = client.RunElapsedTime(1000);
            Assert.AreEqual(-1, result, "RunElapsedTime after end simulation invalid behaviour");

            client.Close();
        }

        [TestMethod]
        public void GetLastTimeStep_Normal_Behaviour()
        {
            //NOTE: Last TimeStep = current TimeStep - 1;

            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            SumoWCFService.TimeStepTDB result;

            client.Open();

            client.RestartSimulation();

            client.RunElapsedTime(5000);
            result = client.GetLastTimeStep();
            Assert.AreEqual(4.0f, result.time, "GetLastTimeStep in 4 invalid response");

            client.RunElapsedTime(3500);
            result = client.GetLastTimeStep();
            Assert.AreEqual(7.0f, result.time, "GetLastTimeStep in 7 invalid response");

            client.RunElapsedTime(500);
            result = client.GetLastTimeStep();
            Assert.AreEqual(8.0f, result.time, "GetLastTimeStep in 8 invalid response");

            client.RunElapsedTime(999);
            result = client.GetLastTimeStep();
            Assert.AreEqual(8.0f, result.time, "GetLastTimeStep in 8.9 invalid response");

            client.RunElapsedTime(1);
            result = client.GetLastTimeStep();
            Assert.AreEqual(9.0f, result.time, "GetLastTimeStep in 9 invalid response");

            client.EndSimulation();
            result = client.GetLastTimeStep();
            Assert.AreEqual(null, result, "GetLastTimeStep invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void GetTimeStep_Normal_Behaviour()
        {
            //NOTE: Last TimeStep = current TimeStep - 1;

            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            SumoWCFService.TimeStepTDB result;

            client.Open();

            client.RestartSimulation();
            client.RunElapsedTime(20000);

            result = client.GetTimeStep(0);
            Assert.AreEqual(0.0f, result.time, "GetTimeStep(0) invalid response");

            result = client.GetTimeStep(5);
            Assert.AreEqual(5.0f, result.time, "GetTimeStep(5) invalid response");

            result = client.GetTimeStep(10);
            Assert.AreEqual(10.0f, result.time, "GetTimeStep(10) invalid response");

            result = client.GetTimeStep(18);
            Assert.AreEqual(18.0f, result.time, "GetTimeStep(18) invalid response");

            result = client.GetTimeStep(19);
            Assert.AreEqual(19.0f, result.time, "GetTimeStep(19) invalid response");

            result = client.GetTimeStep(20);
            Assert.AreEqual(null, result, "GetTimeStep(20) invalid response");

            client.EndSimulation();
            result = client.GetTimeStep(0);
            Assert.AreEqual(null, result, "GetTimeStep(0) invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void GetNumberOfTimeStep_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            int result;

            client.Open();

            client.RestartSimulation();

            result = client.GetNumberOfTimeSteps();
            Assert.AreEqual(0, result, "GetNumberOfTimeSteps in 0 invalid response");

            client.RunElapsedTime(5000);
            result = client.GetNumberOfTimeSteps();
            Assert.AreEqual(5, result, "GetNumberOfTimeSteps in 5 invalid response");

            client.RunElapsedTime(500);
            result = client.GetNumberOfTimeSteps();
            Assert.AreEqual(5, result, "GetNumberOfTimeSteps in 5.5 invalid response");

            client.RunElapsedTime(500);
            result = client.GetNumberOfTimeSteps();
            Assert.AreEqual(6, result, "GetNumberOfTimeSteps in 6 invalid response");

            client.RunElapsedTime(999);
            result = client.GetNumberOfTimeSteps();
            Assert.AreEqual(6, result, "GetNumberOfTimeSteps in 6.9 invalid response"); 
            
            client.RunElapsedTime(1);
            result = client.GetNumberOfTimeSteps();
            Assert.AreEqual(7, result, "GetNumberOfTimeSteps in 7 invalid response");

            client.EndSimulation();
            result = client.GetNumberOfTimeSteps();
            Assert.AreEqual(-1, result, "GetNumberOfTimeSteps invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void LonLatTo2DPosition_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            Random random = new Random();
            double[] result;

            client.Open();

            client.RestartSimulation();

            result = client.LonLatTo2DPosition(random.NextDouble() * 10, random.NextDouble() * 10);
            Assert.AreNotEqual(null, result, "LonLatTo2DPosition invalid response");

            client.EndSimulation();
            result = client.LonLatTo2DPosition(random.NextDouble() * 10, random.NextDouble() * 10);
            Assert.AreEqual(null, result, "LonLatTo2DPosition invalid response after end simulation");
            client.Close();
        }

        [TestMethod]
        public void GetEdgeList_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            string[] result;

            client.Open();

            client.RestartSimulation();

            result = client.GetEdgeList();
            Assert.AreNotEqual(null, result, "GetEdgeList invalid response");

            client.EndSimulation();
            result = client.GetEdgeList();
            Assert.AreEqual(null, result, "GetEdgeList invalid response after end simulation");
            client.Close();
        }

        [TestMethod]
        public void GetVehicleTypeLength_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            SumoWCFService.TimeStepTDB timestep;
            double result;

            client.Open();

            client.RestartSimulation();
            client.RunElapsedTime(20000);

            timestep = client.GetLastTimeStep();
            result = client.GetVehicleTypeLength(timestep.vehicles[0].type);
            Assert.AreNotEqual(-1, result, "GetVehicleTypeLength invalid response");

            client.EndSimulation();
            result = client.GetVehicleTypeLength(timestep.vehicles[0].type);
            Assert.AreEqual(-1, result, "GetVehicleTypeLength invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void GetVehicleTypeWidth_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            SumoWCFService.TimeStepTDB timestep;
            double result;

            client.Open();

            client.RestartSimulation();
            client.RunElapsedTime(20000);

            timestep = client.GetLastTimeStep();
            result = client.GetVehicleTypeWidth(timestep.vehicles[0].type);
            Assert.AreNotEqual(-1, result, "GetVehicleTypeWidth invalid response");

            client.EndSimulation();
            result = client.GetVehicleTypeWidth(timestep.vehicles[0].type);
            Assert.AreEqual(-1, result, "GetVehicleTypeWidth invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void GetVehicleTypeMaxAccel_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            SumoWCFService.TimeStepTDB timestep;
            double result;

            client.Open();

            client.RestartSimulation();
            client.RunElapsedTime(20000);

            timestep = client.GetLastTimeStep();
            result = client.GetVehicleTypeMaxAccel(timestep.vehicles[0].type);
            Assert.AreNotEqual(-1, result, "GetVehicleTypeMaxAccel invalid response");

            client.EndSimulation();
            result = client.GetVehicleTypeMaxAccel(timestep.vehicles[0].type);
            Assert.AreEqual(-1, result, "GetVehicleTypeMaxAccel invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void GetVehicleTypeMaxSpeed_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            SumoWCFService.TimeStepTDB timestep;
            double result;

            client.Open();

            client.RestartSimulation();
            client.RunElapsedTime(20000);

            timestep = client.GetLastTimeStep();
            result = client.GetVehicleTypeMaxSpeed(timestep.vehicles[0].type);
            Assert.AreNotEqual(-1, result, "GetVehicleTypeMaxSpeed invalid response");

            client.EndSimulation();
            result = client.GetVehicleTypeMaxSpeed(timestep.vehicles[0].type);
            Assert.AreEqual(-1, result, "GetVehicleTypeMaxSpeed invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void GetVehicleTypeMaxDecel_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            SumoWCFService.TimeStepTDB timestep;
            double result;

            client.Open();

            client.RestartSimulation();
            client.RunElapsedTime(20000);

            timestep = client.GetLastTimeStep();
            result = client.GetVehicleTypeMaxDecel(timestep.vehicles[0].type);
            Assert.AreNotEqual(-1, result, "GetVehicleTypeMaxDecel invalid response");

            client.EndSimulation();
            result = client.GetVehicleTypeMaxDecel(timestep.vehicles[0].type);
            Assert.AreEqual(-1, result, "GetVehicleTypeMaxDecel invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void GetVehicleRouteId_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            SumoWCFService.TimeStepTDB timestep;
            string result;

            client.Open();

            client.RestartSimulation();
            client.RunElapsedTime(20000);

            timestep = client.GetLastTimeStep();
            result = client.GetVehicleRouteId(timestep.vehicles[0].id);
            Assert.AreNotEqual(null, result, "GetVehicleRouteId invalid response");

            client.EndSimulation();
            result = client.GetVehicleRouteId(timestep.vehicles[0].id);
            Assert.AreEqual(null, result, "GetVehicleRouteId invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void GetVehicleLaneIndex_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            SumoWCFService.TimeStepTDB timestep;
            int result;

            client.Open();

            client.RestartSimulation();
            client.RunElapsedTime(20000);

            timestep = client.GetLastTimeStep();
            result = client.GetVehicleLaneIndex(timestep.vehicles[0].id);
            Assert.AreNotEqual(-1, result, "GetVehicleLaneIndex invalid response");

            client.EndSimulation();
            result = client.GetVehicleLaneIndex(timestep.vehicles[0].id);
            Assert.AreEqual(-1, result, "GetVehicleLaneIndex invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void GetEdgesInRoute_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            SumoWCFService.TimeStepTDB timestep;
            string routeId;
            string[] result;

            client.Open();

            client.RestartSimulation();
            client.RunElapsedTime(20000);

            timestep = client.GetLastTimeStep();
            routeId = client.GetVehicleRouteId(timestep.vehicles[0].id);
            result = client.GetEdgesInRoute(routeId);
            Assert.AreNotEqual(null, result, "GetEdgesInRoute invalid response");

            client.EndSimulation();
            result = client.GetEdgesInRoute(routeId);
            Assert.AreEqual(null, result, "GetEdgesInRoute invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void ChangeVehicleSpeed_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            SumoWCFService.TimeStepTDB timestep;
            float posx_before, posx_after;
            int result;

            client.Open();

            client.RestartSimulation();
            client.RunElapsedTime(20000);

            timestep = client.GetLastTimeStep();
            result = client.ChangeVehicleSpeed(timestep.vehicles[0].id, 0.0d, 2000);
            Assert.AreNotEqual(-1, result, "ChangeVehicleSpeed invalid response");

            client.RunElapsedTime(2000);
            timestep = client.GetLastTimeStep();
            posx_before = timestep.vehicles[0].latitude;

            client.RunElapsedTime(5000);
            timestep = client.GetLastTimeStep();
            posx_after = timestep.vehicles[0].latitude;

            Assert.AreEqual(posx_before, posx_after,  0.01, "ChangeVehicleSpeed invalid response");

            client.EndSimulation();
            result = client.ChangeVehicleSpeed(timestep.vehicles[0].id, 0.0d, 2000);
            Assert.AreEqual(-1, result, "ChangeVehicleSpeed invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void ChangeVehicleMaxSpeed_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            SumoWCFService.TimeStepTDB timestep;
            int result;

            client.Open();

            client.RestartSimulation();
            client.RunElapsedTime(20000);

            timestep = client.GetLastTimeStep();
            result = client.ChangeVehicleMaxSpeed(timestep.vehicles[0].id, 5.0d);
            Assert.AreNotEqual(-1, result, "ChangeVehicleMaxSpeed invalid response");

            client.EndSimulation();
            result = client.ChangeVehicleMaxSpeed(timestep.vehicles[0].id, 5.0d);
            Assert.AreEqual(-1, result, "ChangeVehicleMaxSpeed invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void ResumeVehicleBehaviour_Normal_Behaviour()
        {
            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();
            SumoWCFService.TimeStepTDB timestep;
            float posx_before;
            float posx_after;

            int result;

            client.Open();

            client.RestartSimulation();
            client.RunElapsedTime(20000);

            timestep = client.GetLastTimeStep();
            result = client.ChangeVehicleSpeed(timestep.vehicles[0].id, 0.0d, 2000);

            client.RunElapsedTime(2000);
            timestep = client.GetLastTimeStep();
            posx_before = timestep.vehicles[0].latitude;

            result = client.ResumeVehicleBehaviour(timestep.vehicles[0].id);
            Assert.AreNotEqual(-1, result, "ResumeVehicleBehaviour invalid response");

            client.RunElapsedTime(5000);
            timestep = client.GetLastTimeStep();
            posx_after = timestep.vehicles[0].latitude;
            Assert.AreNotEqual(posx_before, posx_after, "ResumeVehicleBehaviour invalid response");

            client.EndSimulation();
            result = client.ResumeVehicleBehaviour(timestep.vehicles[0].id);
            Assert.AreEqual(-1, result, "ResumeVehicleBehaviour invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void AddNewVehicle_Normal_Behaviour()
        {
            //NOTE: This test just checks the normal behaviour of the service call, but does not check if the vehicle is successfully added into the SUMO simulation.

            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();

            int result;

            client.Open(); 
            
            client.RestartSimulation();
            client.RunElapsedTime(20000);

            result = client.AddNewVehicle("vehTest", "edgeTest", "routeTest", 25000, 100.0d, 100.0d, 0x00);
            Assert.AreNotEqual(-1, result, "AddNewVehicle invalid response");

            client.EndSimulation();
            result = client.AddNewVehicle("vehTest", "edgeTest", "routeTest", 25000, 100.0d, 100.0d, 0x00); 
            Assert.AreEqual(-1, result, "AddNewVehicle invalid response after end simulation");

            client.Close();
        }

        [TestMethod]
        public void AddStopInVehicle_Normal_Behaviour()
        {
            //NOTE: This test just checks the normal behaviour of the service call, but does not check if the stop is successfully added into the SUMO simulation.

            SumoWCFService.SumoServiceClient client = new SumoWCFService.SumoServiceClient();

            int result;

            client.Open();

            client.RestartSimulation();
            client.RunElapsedTime(20000);

            result = client.AddStopInVehicle("vehTest", "edgeTest", 100.0d, 0x00, 5000);
            Assert.AreNotEqual(-1, result, "AddStopInVehicle invalid response");

            client.EndSimulation();
            result = client.AddStopInVehicle("vehTest", "edgeTest", 100.0d, 0x00, 5000);
            Assert.AreEqual(-1, result, "AddStopInVehicle invalid response after end simulation");

            client.Close();
        }
    }
}
