using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace SumoWCFService
{
    [ServiceBehavior(AddressFilterMode = AddressFilterMode.Any)]
    public class SumoService : ISumoService
    {
        private static string traciIp = "127.0.0.1";
        private static int traciPort = 3456;
        private static int fcdOutputPort = 3654;
        private static SumoTraciConnection traciCom;
        private static SumoListener fcdListener;
        private static SumoTrafficDB trafficDB;
        private static bool isSimulationStarted = false;

        /// <summary>
        /// Starts the communication with the interface SUMO/TraCI and starts listening 
        /// from the fcd output.
        /// </summary>
        /// <returns>0 if operation succeeded, -1 otherwise.</returns>
        public int InitializeSimulation()
        {
            if (!isSimulationStarted)
            {
                traciCom = new SumoTraciConnection(traciIp, traciPort);
                trafficDB = new SumoTrafficDB();
                fcdListener = new SumoListener(fcdOutputPort, trafficDB);

                if (traciCom.StartCommunication() == -1)
                    return -1;
                if (fcdListener.StartListening() == -1)
                    return -1;

                isSimulationStarted = true;

                return 0;
            }
            else
                return -1;
        }

        /// <summary>
        /// Requests SUMO to end the current simulation.
        /// </summary>
        /// <returns>0 if operation succeeded, -1 if the simulation is not running.</returns>
        public int EndSimulation()
        {
            if (isSimulationStarted)
            {
                traciCom.EndSimulation();
                fcdListener.StopListening();
                isSimulationStarted = false;
                return 0;
            }
            else
                return -1;
        }

        /// <summary>
        /// Requests SUMO to restart the current simulation. All the data from the previous simulation will be lost.
        /// </summary>
        /// <returns>0 if operation succeeded, -1 otherwise.</returns>
        public int RestartSimulation()
        {
            if (isSimulationStarted)
            {
                traciCom.EndSimulation();
                fcdListener.StopListening();
                isSimulationStarted = false;
            }

            return InitializeSimulation();
        }

        /// <summary>
        /// Gets the current timestep of the SUMO simulation.
        /// </summary>
        /// <returns>Current timestep or null if the simulation is not running.</returns>
        public TimeStepTDB GetCurrentTimeStep()
        {
            if (isSimulationStarted)
            {
                return trafficDB.GetCurrentTimeStep();
            }
            else
                return null;
        }

        /// <summary>
        /// Gets the timestep requested from the SUMO simulation.
        /// </summary>
        /// <param name="index">Index of the timestep requested.</param>
        /// <returns>A TimeStepTDB if the timestep requested exists, null otherwise.</returns>
        public TimeStepTDB GetTimeStep(int index)
        {
            if (isSimulationStarted)
            {
                try
                {
                    return trafficDB.timeStep[index];
                }
                catch
                {
                    return null;
                }
            }
            else
                return null;
        }

        /// <summary>
        /// Gets the number of timesteps that have already been run in the SUMO simulation.
        /// </summary>
        /// <returns>The number of timesteps, or -1 if the simulation is not running.</returns>
        public int GetNumberOfTimeSteps()
        {
            if (isSimulationStarted)
            {
                return trafficDB.GetNumberOfTimeSteps();
            }
            else
                return -1;
        }

        /// <summary>
        /// Runs a single timestep in SUMO (1000 ms). 
        /// </summary>
        /// <returns>0 if operation succeeded, -1 if the simulation is not running.</returns>
        public int RunSingleStep()
        {
            if (isSimulationStarted)
            {
                traciCom.RunSingleSimulationStep();
                return 0;
            }
            else
                return -1;
        }

        /// <summary>
        /// Run a step of a certain elapsed time in SUMO. 
        /// </summary>
        /// <param name="elapsedTime">Elapsed time of the simulation step.</param>
        /// <returns>0 if operation succeeded, -1 if the simulation is not running.</returns>
        public int RunElapsedTime(int elapsedTime)
        {
            if (isSimulationStarted)
            {
                traciCom.RunElapsedSimulationTime(elapsedTime);
                return 0;
            }
            else
                return -1;
        }

        /// <summary>
        /// Requests SUMO to change the speed of a certain vehicle in the current simulation. Doing
        /// this, the vehicle will keep the same speed until its car-following behaviour is resumed.
        /// If the speed is 0, the vehicle will stop.
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <param name="speed">New speed for the vehicle.</param>
        /// <param name="ms">Milliseconds to reach the new speed.</param>
        /// <seealso cref="ResumeVehicleBehaviour"/>
        /// <returns>0 if operation succeeded, -1 if the simulation is not running.</returns>
        public int ChangeVehicleSpeed(string vehId, double speed, int ms)
        {
            if (isSimulationStarted)
            {
                traciCom.ChangeVehicleSpeed(vehId, speed, ms);
                return 0;
            }
            else
                return -1;
        }

        /// <summary>
        /// Requests SUMO to change the max speed of a certain vehicle in the current simulation.
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <param name="maxSpeed">New max speed for the vehicle.</param>
        /// <returns>0 if operation succeeded, -1 if the simulation is not running.</returns>
        public int ChangeVehicleMaxSpeed(string vehId, double maxSpeed)
        {
            if (isSimulationStarted)
            {
                traciCom.ChangeVehicleMaxSpeed(vehId, maxSpeed);
                return 0;
            }
            else
                return -1;
        }

        /// <summary>
        /// Requests SUMO to resume the car-following behaviour of a certain vehicle in the current
        /// simulation.
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <seealso cref="ChangeVehicleSpeed"/>
        /// <returns>0 if operation succeeded, -1 if the simulation is not running.</returns>
        public int ResumeVehicleBehaviour(string vehId)
        {
            if (isSimulationStarted)
            {
                traciCom.ResumeVehicleBehaviour(vehId);
                return 0;
            }
            else
                return -1;
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
        /// <returns>0 if operation succeeded, -1 if the simulation is not running.</returns>
        public int AddNewVehicle(string vehId, string type, string routeId, int departTime, double departPosition, double departSpeed, byte departLane)
        {
            if (isSimulationStarted)
            {
                traciCom.AddNewVehicle(vehId, type, routeId, departTime, departPosition, departSpeed, departLane);
                return 0;
            }
            else
                return -1;
        }

        /// <summary>
        /// Requests SUMO to add a stop in an existing vehicle of the simulation.
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <param name="edgeId">String containing the edge where the vehicle should stop.</param>
        /// <param name="position">Position in the edge where the vehicle should stop.</param>
        /// <param name="laneIndex">Index of the lane.</param>
        /// <param name="durationInMs">Number of milliseconds the vehicle will stop before continuing its trip.</param>
        /// <returns>0 if operation succeeded, -1 if the simulation is not running.</returns>
        public int AddStopInVehicle(string vehId, string edgeId, double position, byte laneIndex, int durationInMs)
        {
            if (isSimulationStarted)
            {
                traciCom.AddStopInVehicle(vehId, edgeId, position, laneIndex, durationInMs);
                return 0;
            }
            else
                return -1;
        }

        /// <summary>
        /// Converts a Lon-Lat coordinate to a 2D Position (x,y)
        /// </summary>
        /// <param name="lon">Longitude to convert</param>
        /// <param name="lat">Latitude to convert</param>
        /// <returns>Array of doubles containing the conversion (x,y) in positions [0] and [1], respectively.</returns>
        public double[] LonLatTo2DPosition(double lon, double lat)
        {
            if (isSimulationStarted)
            {
                return traciCom.LonLatTo2DPosition(lon, lat);
            }
            else
                return null;
        }

        /// <summary>
        /// Requests all the edge ids existing in the simulation.
        /// </summary>
        /// <returns>List of strings with all the edge ids.</returns>
        public string[] GetEdgeList()
        {
            if (isSimulationStarted)
            {
                return traciCom.GetEdgeList();
            }
            else
                return null;
        }

        /// <summary>
        /// Requests the length of a vehicle type. 
        /// </summary>
        /// <param name="vehType">String containing the type of the vehicle.</param>
        /// <returns>Length of the vehicle type given.</returns>
        public double GetVehicleTypeLength(string vehType)
        {
            if (isSimulationStarted)
            {
                return traciCom.GetVehicleTypeLength(vehType);
            }
            else
                return -1;
        }

        /// <summary>
        /// Requests the width of a vehicle type. 
        /// </summary>
        /// <param name="vehType">String containing the type of the vehicle.</param>
        /// <returns>Width of the vehicle type given.</returns>
        public double GetVehicleTypeWidth(string vehType)
        {
            if (isSimulationStarted)
            {
                return traciCom.GetVehicleTypeWidth(vehType);
            }
            else
                return -1;
        }

        /// <summary>
        /// Requests the maximum acceleration of a vehicle type. 
        /// </summary>
        /// <param name="vehType">String containing the type of the vehicle.</param>
        /// <returns>Maximum acceleration of the vehicle type given.</returns>
        public double GetVehicleTypeMaxAccel(string vehType)
        {
            if (isSimulationStarted)
            {
                return traciCom.GetVehicleTypeMaxAccel(vehType);
            }
            else
                return -1;
        }

        /// <summary>
        /// Requests the maximum speed of a vehicle type. 
        /// </summary>
        /// <param name="vehType">String containing the type of the vehicle.</param>
        /// <returns>Maximum speed of the vehicle type given.</returns>
        public double GetVehicleTypeMaxSpeed(string vehType)
        {
            if (isSimulationStarted)
            {
                return traciCom.GetVehicleTypeMaxSpeed(vehType);
            }
            else
                return -1;
        }

        /// <summary>
        /// Requests the maxixum deceleration of a vehicle type. 
        /// </summary>
        /// <param name="vehType">String containing the type of the vehicle.</param>
        /// <returns>Maximum deceleration of the vehicle type given.</returns>
        public double GetVehicleTypeMaxDecel(string vehType)
        {
            if (isSimulationStarted)
            {
                return traciCom.GetVehicleTypeMaxDecel(vehType);
            }
            else
                return -1;
        }

        /// <summary>
        /// Requests the route id of a certain vehicle. 
        /// </summary>
        /// <param name="vehId">String with the vehicle id.</param>
        /// <returns>Route id of the vehicle.</returns>
        public string GetVehicleRouteId(string vehId)
        {
            if (isSimulationStarted)
            {
                return traciCom.GetVehicleRouteId(vehId);
            }
            else
                return null;
        }

        /// <summary>
        /// Requests the lane index of a certain vehicle.
        /// </summary>
        /// <param name="vehId">String with the vehicle id.</param>
        /// <returns>Lane index where the vehicle stands.</returns>
        public int GetVehicleLaneIndex(string vehId)
        {
            if (isSimulationStarted)
            {
                return traciCom.GetVehicleLaneIndex(vehId);
            }
            else
                return -1;
        }

        /// <summary>
        /// Requests the edge of a certain route. 
        /// </summary>
        /// <param name="routeId">String with the route id.</param>
        /// <returns>List of edges that are part of the route given.</returns>
        public string[] GetEdgesInRoute(string routeId)
        {
            if (isSimulationStarted)
            {
                return traciCom.GetEdgesInRoute(routeId);
            }
            else
                return null;
        }
    }
}
