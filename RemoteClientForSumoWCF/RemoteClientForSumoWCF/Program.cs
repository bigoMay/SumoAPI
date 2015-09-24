using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteClientForSumoWCF
{
    class Program
    {
        static void Main(string[] args)
        {
            //Display a menu to interact with the simulator over command line
            Console.WriteLine("\n SUMO COMMUNICATION API CLIENT TEST v2.1\n" +
                    "\n F12: initialize the simulator (first thing to do)" +
                    "\n Enter: run elapsed time step" +
                    "\n Escape: close simulation" +
                    "\n Spacebar: display timestep info" +
                    "\n F1: change vehicle speed" +
                    "\n F2: resume vehicle" +
                    "\n F3: convert lon-lat coordinates" +
                    "\n F4: get all the edge ids" +
                    "\n F5: Get vehicle type properties" +
                    "\n F6: Get vehicle route information" +
                    "\n");

            //Create a new sumo controller to get communication with the SUMO/TraCI interface of the simulation.
            //NOTE: SUMO/TraCI must be running in the proper address/port before creating this object.
            SumoWCFService.SumoServiceClient sumoClient = new SumoWCFService.SumoServiceClient();
            sumoClient.Open();

            //Command loop (for the client)
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey();

                //Enter: Step simulation
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine(" Elapsed time: ");
                    int elapsedTime = int.Parse(Console.ReadLine());
                    Console.WriteLine(" Running elapsed time step...");
                    int result = sumoClient.RunElapsedTime(elapsedTime);
                    if (result == 0)
                        Console.WriteLine("Operation performed successfully.");
                    else
                        Console.WriteLine("Error in the service, the operation could not be completed.");
                }

                //ESC: Close simulation
                else if (key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine(" Ending simulation...\n");
                    sumoClient.EndSimulation();
                    Console.WriteLine(" Simulation finished, to initialize it again, press F12.\n");
                }

                //SPACE: Display timestep info
                else if (key.Key == ConsoleKey.Spacebar)
                {
                    Console.WriteLine(" The current database has a total of: " + (sumoClient.GetNumberOfTimeSteps() - 2) + " simulation timesteps stored.\n");
                    Console.WriteLine(" Choose a timestep number: ");
                    int timeStepIndex = int.Parse(Console.ReadLine());

                    if (timeStepIndex < sumoClient.GetNumberOfTimeSteps() - 1)
                        PrintTimeStepInfo(sumoClient.GetTimeStep(timeStepIndex));
                    else
                        Console.WriteLine(" That timestep does not exist in the traffic DB...");
                }

                //F1: Change vehicle speed
                else if (key.Key == ConsoleKey.F1)
                {
                    Console.WriteLine(" Vehicle id: ");
                    string vehId = Console.ReadLine();
                    Console.WriteLine(" New speed: ");
                    double speed = double.Parse(Console.ReadLine());
                    Console.WriteLine(" Amount of time to change speed (in ms): ");
                    int ms = int.Parse(Console.ReadLine());
                    Console.WriteLine("\n Changing velocity of " + vehId + " \n");
                    sumoClient.ChangeVehicleSpeed(vehId, speed, ms);
                }

                //F2: Resume vehicle
                else if (key.Key == ConsoleKey.F2)
                {
                    Console.WriteLine(" Vehicle id: ");
                    string vehId = Console.ReadLine();
                    Console.WriteLine("\n Resuming vehicle " + vehId + " \n");
                    sumoClient.ResumeVehicleBehaviour(vehId);
                }

                //F3: Coordinate conversion
                else if (key.Key == ConsoleKey.F3)
                {
                    Console.WriteLine(" Lon: ");
                    double lon = double.Parse(Console.ReadLine());
                    Console.WriteLine(" Lat: ");
                    double lat = double.Parse(Console.ReadLine());
                    double[] resp = sumoClient.LonLatTo2DPosition(lon, lat);
                    Console.WriteLine(" Conversion -> X:" + resp[0] + " Y:" + resp[1]);
                }

                //F4: Get edge ids
                else if (key.Key == ConsoleKey.F4)
                {
                    string[] resp = sumoClient.GetEdgeList();
                    Console.WriteLine(" Edge ids: ");
                    for (int i = 0; i < resp.Length; i++)
                    {
                        Console.Write(resp[i] + ", ");
                    }
                    Console.WriteLine(" \n All edges listed \n");
                }

                //F5: Get the vehicle type properties
                else if (key.Key == ConsoleKey.F5)
                {
                    Console.WriteLine(" Vehicle type: ");
                    string vehType = Console.ReadLine();

                    double length = sumoClient.GetVehicleTypeLength(vehType);
                    double width = sumoClient.GetVehicleTypeWidth(vehType);
                    double maxAccel = sumoClient.GetVehicleTypeMaxAccel(vehType);
                    double maxDecel = sumoClient.GetVehicleTypeMaxDecel(vehType);
                    double maxSpeed = sumoClient.GetVehicleTypeMaxSpeed(vehType);

                    Console.WriteLine("\n Properties of " + vehType + ":\n" +
                        " Length: " + length + "  Width: " + width + "  Max Accel: " + maxAccel +
                            "  Max Decel: " + maxDecel + "  Max Speed: " + maxSpeed + "\n");
                }

                //F6: Get the vehicle route information
                else if (key.Key == ConsoleKey.F6)
                {
                    Console.WriteLine(" Vehicle id: ");
                    string vehId = Console.ReadLine();

                    string routeId = sumoClient.GetVehicleRouteId(vehId);
                    int laneIndex = sumoClient.GetVehicleLaneIndex(vehId);
                    string[] edgeList = sumoClient.GetEdgesInRoute(routeId);

                    Console.WriteLine("\n Route information of " + vehId + ":\n" +
                        " Route Id: " + routeId + "  Lane Index: " + laneIndex + "\n");

                    Console.WriteLine(" Edge ids: ");
                    for (int i = 0; i < edgeList.Length; i++)
                    {
                        Console.Write(edgeList[i] + ", ");
                    }
                    Console.WriteLine(" \n All edges listed \n");
                }

                //F12: Initialize the simulation
                else if (key.Key == ConsoleKey.F12)
                {
                    int result = sumoClient.InitializeSimulation();
                    if (result == 0)
                        Console.WriteLine("SUMO simulator initialized correctly, ready for use.");
                    else
                        Console.WriteLine("Error initializing SUMO: Is the simulator initialized already?");
                }
            }
        }

        /// <summary>
        /// Print the information contained in the trafficDB in the timestep given.
        /// </summary>
        /// <param name="currentTimeStep">Timestep of the simulation.</param>
        private static void PrintTimeStepInfo(SumoWCFService.TimeStepTDB currentTimeStep)
        {
            int numberOfVehicles = currentTimeStep.vehicles.Length;

            Console.WriteLine(" --- displaying timestep info ---\n");

            for (int i = 0; i < numberOfVehicles; i++)
            {
                SumoWCFService.VehicleTDB veh = currentTimeStep.vehicles[i];

                Console.WriteLine(" Veh ID: " + veh.id + ", latitude: " + veh.latitude + ", longitude: " + veh.longitude + "\n");
                //Other possible parameters to display: veh.type, veh.angle;
            }

            Console.WriteLine(" --- --- -- --- --- --- --- -- --- ---\n");
        }
    }
}
