/*
 * 
 * SUMO COMMUNICATION API CLIENT TEST v1.0
 * 
 * Miguel Ramos Carretero (miguelrc@kth.se)
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SumoCommunicationAPI;
using System.Net;

namespace ClientTestSumoAPI
{
    /// <summary>
    /// This program is a dummy to test the communication between the component SumoController 
    /// (from the SumoCommunicationAPI) and the interface SUMO/TraCI. This program will send command requests 
    /// to the simulator. Follow the instructions in the inline menu. 
    /// NOTE 1: In order to run this program correctly, SUMO/TraCI must be running in localhost in port 3456. 
    /// NOTE 2: The program ListenerTestSumoAPI must be already running when this program is launched!
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            //Display a menu to interact with the simulator over command line
            Console.WriteLine("\n SUMO COMMUNICATION API CLIENT TEST v1.1\n" +
                    "\n Enter: run elapsed time step" +
                    "\n Escape: close simulation" +
                    "\n F1: change vehicle speed" +
                    "\n F2: resume vehicle" +
                    "\n F3: convert lon-lat coordinates" +
                    "\n");

            //Create a new sumo controller to get communication with the SUMO/TraCI interface of the simulation.
            //NOTE: SUMO/TraCI must be running in the proper address/port before creating this object.
            SumoController mySumoController = new SumoController("127.0.0.1", 3456);

            //Initializes the listener and the sumo controller
            //NOTE: ListenerTestSumoAPI MUST be running BEFORE the initialization of the communication with SUMO/TraCI
            mySumoController.InitializeCommunication();

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
                    mySumoController.RunElapsedTime(elapsedTime);
                }

                //ESC: Close simulation
                else if (key.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine(" Ending simulation...\n");
                    mySumoController.EndSimulation();
                    break;
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
                    mySumoController.ChangeVehicleSpeed(vehId, speed, ms);
                }

                //F2: Resume vehicle
                else if (key.Key == ConsoleKey.F2)
                {
                    Console.WriteLine(" Vehicle id: ");
                    string vehId = Console.ReadLine();
                    Console.WriteLine("\n Resuming vehicle " + vehId + " \n");
                    mySumoController.ResumeVehicleBehaviour(vehId);
                }

                //F3: Coordinate conversion
                else if (key.Key == ConsoleKey.F3)
                {
                    Console.WriteLine(" Lon: ");
                    double lon = double.Parse(Console.ReadLine());
                    Console.WriteLine(" Lat: ");
                    double lat = double.Parse(Console.ReadLine());
                    double[] resp = mySumoController.LonLatTo2DPosition(lon, lat);
                    Console.WriteLine(" Conversion -> X:" + resp[0] + " Y:" + resp[1]);
                }
            }
        }
    }
}
