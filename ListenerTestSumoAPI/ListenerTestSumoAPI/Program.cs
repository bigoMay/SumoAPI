/*
 * 
 * SUMO COMMUNICATION API LISTENER TEST v1.0
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
    /// This program is a dummy to test the communication between the components SumoListener
    /// (from the SumoCommunicationAPI) and the interface SUMO/TraCI. This program will listen from the 
    /// FCD output of SUMO and it will populate the component SumoTrafficDB accordingly. Follow the instructions 
    /// in the inline menu. 
    /// NOTE 1: In order to run this program correctly, SUMO must have the FCD output option active in localhost for port 3654. 
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {

            //Display a menu to interact with the simulator over command line
            Console.WriteLine("\n SUMO COMMUNICATION API LISTENER TEST v1.1\n");
            Console.WriteLine(" -----\n");

            //Create a traffic DB where the information from SUMO will be stored.
            SumoTrafficDB myTrafficDB = new SumoTrafficDB();

            //Create a listener to receive the data from SUMO over the port 3654. 
            //The data received will be stored in myTrafficDB.
            SumoListener myListener = new SumoListener(3654, myTrafficDB);

            //Initializes the listener and the sumo controller
            //NOTE: The listener MUST be initialized BEFORE the initialization of the communication with SUMO/TraCI
            myListener.StartListening();

            int timeStepIndex = 0;

            //Command loop (for the client: uncomment one of the alternatives, comment the other one)
            while (true)
            {
                //--- ALTERNATIVE 1: The user selects when to read timesteps ---

                Console.WriteLine(" Press ENTER to query the traffic DB...\n");

                ConsoleKeyInfo key = Console.ReadKey();

                if (key.Key == ConsoleKey.Enter)
                {

                    Console.WriteLine(" The current database has a total of: " + (myTrafficDB.GetNumberOfTimeSteps()-2) + " simulation timesteps stored.\n");
                    Console.WriteLine(" Choose a timestep number: ");
                    timeStepIndex = int.Parse(Console.ReadLine());

                    if (timeStepIndex < myTrafficDB.GetNumberOfTimeSteps() - 1)
                        PrintTimeStepInfo(myTrafficDB, timeStepIndex); 
                    else
                        Console.WriteLine(" That timestep does not exist in the traffic DB...");
                }
                
                //--- 


                //--- ALTERNATIVE 2: Display timesteps as soon as they are stored in the traffic DB ---
                
                //if (timeStepIndex < myTrafficDB.GetNumberOfTimeSteps() - 1)
                //{
                //    PrintTimeStepInfo(myTrafficDB, timeStepIndex);
                //    timeStepIndex++;
                //}

                //--- 
            }
        }

        /// <summary>
        /// Print the information contained in the trafficDB in the timestep given.
        /// </summary>
        /// <param name="timeStepIndex">Timestep of the simulation.</param>
        private static void PrintTimeStepInfo(SumoTrafficDB tdb, int timeStepIndex)
        {
            TimeStepTDB currentTimeStep = tdb.timeStep[timeStepIndex];
            int numberOfVehicles = tdb.GetNumberOfVehiclesInTimeStep(timeStepIndex);

            Console.WriteLine(" --- timestep " + timeStepIndex + " ---\n");

            for (int i = 0; i < numberOfVehicles; i++)
            {
                VehicleTDB veh = currentTimeStep.vehicles[i];

                Console.WriteLine(" Veh ID: " + veh.id + ", latitude: " + veh.latitude  + ", longitude: " + veh.longitude + "\n"); 
                //Other possible parameters to display: veh.type, veh.angle;
            }

            Console.WriteLine(" --- --- -- --- --- --- --- -- --- ---\n");
        }
    }
}
