/*
 * 
 * SUMO COMMUNICATION API
 * SumoController.cs
 * 
 * Miguel Ramos Carretero (miguelrc@kth.se)
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SumoCommunicationAPI
{
    /// <summary>
    /// Provides an interface for communication with SUMO/TraCI.
    /// </summary>
    public class SumoController
    {
        //private int timestepsPerSecond = 1;
        private Thread simulationThread;
        private SumoTCPCommunication tcpComScript;
        private bool isSimulationStarted = false;
        private static bool isPause = false;

        /// <summary>
        /// Constructor of the class.
        /// </summary>
        /// <param name="traciRemoteIp">Ip address where SUMO/TraCI is running.</param>
        /// <param name="traciRemotePort">Port where SUMO/TraCI is enabling communication.</param>
        public SumoController(string traciRemoteIp, int traciRemotePort)
        {
            tcpComScript = new SumoTCPCommunication(traciRemoteIp, traciRemotePort);
        }

        /// <summary>
        /// Starts the communication with the interface SUMO/TraCI.
        /// </summary>
        public void InitializeCommunication()
        {
            tcpComScript.StartCommunication();
            isSimulationStarted = true;
        }

        ///// <summary>
        ///// Starts SUMO simulation running an infinite loop of timesteps. 
        ///// If simulation is in pause, this command will resume the simulation.
        ///// </summary>
        ///// <remarks>
        ///// While running the simulation, the time between timesteps is set 
        ///// by the field <see cref="MainController.timestepsPerSecond"/> (in ms). 
        ///// A thread is created to run the simulation in loops. For each timestep, 
        ///// the thread request a new simulation timestep to <see cref="SumoTCPCommunication"/>.
        ///// </remarks>
        ///// <seealso cref="RunSimThread"/>
        //public void RunSimulation()
        //{
        //    if (!isSimulationStarted)
        //    {
        //        //Create a thread for the loop
        //        ThreadStart ts = new ThreadStart(RunSimThread);
        //        simulationThread = new Thread(ts);
        //        simulationThread.Start();
        //        //Console.Write("Thread for running simulation created");
        //        isSimulationStarted = true;
        //    }
        //    if (isPause)
        //    {
        //        isPause = false;
        //        simulationThread.Interrupt();
        //    }
        //}

        ///// <summary>
        ///// If SUMO simulation is running, pauses the simulation. 
        ///// </summary>
        //public void PauseSimulation()
        //{
        //    if (isSimulationStarted)
        //    {
        //        isPause = true;
        //    }
        //}

        /// <summary>
        /// Runs a single timestep in SUMO (1000 ms). 
        /// </summary>
        public void RunSingleStep()
        {
            if (isSimulationStarted)
            {
                tcpComScript.RunSingleSimulationStep();
            }
        }

        /// <summary>
        /// Run a step of a certain elapsed time in SUMO. 
        /// </summary>
        /// <param name="elapsedTime">Elapsed time of the simulation step.</param>
        public void RunElapsedTime(int elapsedTime)
        {
            if (isSimulationStarted)
            {
                tcpComScript.RunElapsedSimulationTime(elapsedTime);
            }
        }

        /// <summary>
        /// Requests SUMO to end the current simulation.
        /// </summary>
        public void EndSimulation()
        {
            if (isSimulationStarted)
            {
                tcpComScript.EndSimulation();
            }
        }

        ///// <summary>
        ///// Specify a new velocity for the simulation.
        ///// </summary>
        ///// <param name="timestepsPerSecond">Number of timesteps per second.</param>
        //public void ChangeTimeStepVelocity(int timestepsPerSecond)
        //{
        //    this.timestepsPerSecond = timestepsPerSecond;
        //}

        /// <summary>
        /// Requests SUMO to change the speed of a certain vehicle in the current simulation. Doing
        /// this, the vehicle will keep the same speed until its car-following behaviour is resumed.
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <param name="speed">New speed for the vehicle.</param>
        /// <param name="ms">Milliseconds to reach the new speed.</param>
        /// <seealso cref="ResumeVehicleBehaviour"/>
        public void ChangeVehicleSpeed(string vehId, double speed, int ms)
        {
            if (isSimulationStarted && !isPause)
            {
                tcpComScript.ChangeVehicleSpeed(vehId, speed, ms);
            }
        }

        /// <summary>
        /// Requests SUMO to resume the car-following behaviour of a certain vehicle in the current
        /// simulation.
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <seealso cref="ChangeVehicleSpeed"/>
        public void ResumeVehicleBehaviour(string vehId)
        {
            if (isSimulationStarted && !isPause)
            {
                tcpComScript.ResumeVehicleBehaviour(vehId);
            }
        }

        /// <summary>
        /// Requests SUMO to add a new vehicle in the simulation.
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <param name="type">String containing the type of the vehicle.</param>
        /// <param name="routeId">String containing the route id of the vehicle.</param>
        /// <param name="departTime">Specify at which timestep of the simulation the vehicle should depart.</param>
        /// <param name="departPosition">Specify the position from which the vehicle should depart.</param>
        /// <param name="departSpeed">Specify the initial speed of the vehicle.</param>
        /// <param name="departLane">Specify the lane where the vehicle should start.</param>
        public void AddNewVehicle(string vehId, string type, string routeId, int departTime, double departPosition, double departSpeed, byte departLane)
        {
            if (isSimulationStarted && !isPause)
            {
                tcpComScript.AddNewVehicle(vehId, type, routeId, departTime, departPosition, departSpeed, departLane);
            }
        }

        /// <summary>
        /// Requests SUMO to add a stop in an existing vehicle of the simulation.
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <param name="edgeId">String containing the edge where the vehicle should stop.</param>
        /// <param name="position">Position in the edge where the vehicle should stop.</param>
        /// <param name="laneIndex">Index of the lane.</param>
        /// <param name="durationInMs">Number of milliseconds the vehicle will stop before continuing its trip.</param>
        public void AddStopInVehicle(string vehId, string edgeId, double position, byte laneIndex, int durationInMs)
        {
            if (isSimulationStarted && !isPause)
            {
                tcpComScript.AddStopInVehicle(vehId, edgeId, position, laneIndex, durationInMs);
            }
        }

        /// <summary>
        /// Gets the current timestep of the SUMO simulation.
        /// </summary>
        /// <returns>Return the current timestep or -1 if the simulation is not running.</returns>
        public int GetCurrentTimeStep()
        {
            return tcpComScript.GetCurrentTimeStep();
        }

        /// <summary>
        /// Converts a Lon-Lat coordinate to a 2D Position (x,y)
        /// </summary>
        /// <param name="lon">Longitude to convert</param>
        /// <param name="lat">Latitude to convert</param>
        /// <returns>Array of doubles containing the conversion (x,y) in positions [0] and [1], respectively.</returns>
        public double[] LonLatTo2DPosition(double lon, double lat)
        {
            return tcpComScript.LonLatTo2DPosition(lon, lat);
        }

        /// <summary>
        /// Requests all the edge ids existing in the simulation.
        /// </summary>
        /// <returns>List of strings with all the edge ids.</returns>
        public string[] GetEdgeList()
        {
            return tcpComScript.GetEdgeList();
        }

        /// <summary>
        /// Requests the length of a vehicle type. 
        /// </summary>
        /// <param name="vehType">String containing the type of the vehicle.</param>
        /// <returns></returns>
        public double GetVehicleTypeLength(string vehType)
        {
            return tcpComScript.GetVehicleTypeLength(vehType);
        }

        /// <summary>
        /// Requests the width of a vehicle type. 
        /// </summary>
        /// <param name="vehType">String containing the type of the vehicle.</param>
        /// <returns></returns>
        public double GetVehicleTypeWidth(string vehType)
        {
            return tcpComScript.GetVehicleTypeWidth(vehType);
        }

        /// <summary>
        /// Requests the maximum acceleration of a vehicle type. 
        /// </summary>
        /// <param name="vehType">String containing the type of the vehicle.</param>
        /// <returns></returns>
        public double GetVehicleTypeMaxAccel(string vehType)
        {
            return tcpComScript.GetVehicleTypeMaxAccel(vehType);
        }

        /// <summary>
        /// Requests the maximum speed of a vehicle type. 
        /// </summary>
        /// <param name="vehType">String containing the type of the vehicle.</param>
        /// <returns></returns>
        public double GetVehicleTypeMaxSpeed(string vehType)
        {
            return tcpComScript.GetVehicleTypeMaxSpeed(vehType);
        }

        /// <summary>
        /// Requests the maxixum deceleration of a vehicle type. 
        /// </summary>
        /// <param name="vehType">String containing the type of the vehicle.</param>
        /// <returns></returns>
        public double GetVehicleTypeMaxDecel(string vehType)
        {
            return tcpComScript.GetVehicleTypeMaxDecel(vehType);
        }

        /// <summary>
        /// Requests the route id of a certain vehicle. 
        /// </summary>
        /// <param name="vehId">String with the vehicle id.</param>
        /// <returns></returns>
        public string GetVehicleRouteId(string vehId)
        {
            return tcpComScript.GetVehicleRouteId(vehId);
        }

        /// <summary>
        /// Requests the lane index of a certain vehicle.
        /// </summary>
        /// <param name="vehId">String with the vehicle id.</param>
        /// <returns></returns>
        public int GetVehicleLaneIndex(string vehId)
        {
            return tcpComScript.GetVehicleLaneIndex(vehId);
        }

        /// <summary>
        /// Requests the edge of a certain route. 
        /// </summary>
        /// <param name="routeId">String with the route id.</param>
        /// <returns></returns>
        public string[] GetEdgesInRoute(string routeId)
        {
            return tcpComScript.GetEdgesInRoute(routeId);
        }
         
        ///// <summary>
        ///// Auxiliar method for threading. 
        ///// Runs an infinite loop of simulation timesteps in SUMO. 
        ///// </summary>
        ///// <remarks>
        ///// While running the simulation, the time between timesteps is set 
        ///// by the public field <see cref="timeStepVelocityInMs"/> (in ms). 
        ///// </remarks>
        //private void RunSimThread()
        //{
        //    while (true)
        //    {
        //        try
        //        {
        //            if (!isPause)
        //            {
        //                tcpComScript.RunSingleSimulationStep();
        //                Thread.Sleep(1000 / timestepsPerSecond); // Check this
        //            }
        //            else
        //                Thread.Sleep(Timeout.Infinite);
        //        }
        //        catch (ThreadAbortException)
        //        {
        //            //Console.Write("Thread for loop simulation aborted");
        //            return;
        //        }
        //        catch (ThreadInterruptedException)
        //        {
        //            //Do nothing
        //            return;
        //        }
        //    }
        //}

        /// <summary>
        /// When application quits, ends SUMO simulation. 
        /// </summary>
        private void OnApplicationQuit()
        {
            //Console.Write("Application quitting! Finish TCP Communication");
            tcpComScript.EndSimulation();
        }
    }
}

