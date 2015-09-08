/*
 * 
 * SUMO COMMUNICATION API
 * SumoTrafficDB.cs
 * 
 * Miguel Ramos Carretero (miguelrc@kth.se)
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SumoCommunicationAPI
{
    /// <summary>
    /// Class that defines a vehicle in the Sumo Traffic DB. 
    /// </summary>
    /// <seealso cref="SumoTrafficDB"/>
    public class VehicleTDB
    {
        /// <summary>
        /// Vehicle id.
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// Latitude position of the vehicle.
        /// </summary>
        public float latitude { get; set; }

        /// <summary>
        /// Longitude position of the vehicle.
        /// </summary>
        public float longitude { get; set; }

        /// <summary>
        /// Type of the vehicle in SUMO.
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// Angle of the vehicle.
        /// </summary>
        public float angle { get; set; }

        /// <summary>
        /// Constructor of the class.
        /// </summary>
        /// <param name="id">Id of the vehicle.</param>
        /// <param name="lat">Latitude position.</param>
        /// <param name="lon">Longitude position.</param>
        /// <param name="type">Type of the vehicle.</param>
        /// <param name="angle">Angle of the vehicle</param>
        internal VehicleTDB(string id, string lat, string lon, string type, string angle)
        {
            this.id = id;
            this.latitude = float.Parse(lat);
            this.longitude = float.Parse(lon);
            this.type = type;
            this.angle = float.Parse(angle);
        }

        /// <summary>
        /// Prints the information of the vehicle. 
        /// </summary>
        /// <returns>String with the information.</returns>
        public override string ToString()
        {
            return (" Id: " + this.id + "\n" +
                " Latitude: " + this.latitude + "\n" +
                " Longitude: " + this.longitude + "\n" +
                " Type: " + this.type + "\n" +
                " Angle: " + this.angle + "\n");
        }
    }

    /// <summary>
    /// Class that defines a timestep in the Sumo Traffic DB.
    /// A timestep is defined by a time and the list of vehicles of that time. 
    /// </summary>
    /// <seealso cref="SumoTrafficDB"/>
    public class TimeStepTDB
    {
        /// <summary>
        /// List of vehicles that populate a certain timestep in the DB.
        /// </summary>
        public List<VehicleTDB> vehicles { get; set; }
        
        private float time;
        private int index;
        
        /// <summary>
        /// Constructor of the class.
        /// </summary>
        /// <param name="time">Time of the simulation.</param>
        /// <param name="index">Index position in the Sumo Traffic DB.</param>
        internal TimeStepTDB(float time, int index)
        {
            this.time = time;
            this.index = index;
            vehicles = new List<VehicleTDB>();
        }

        /// <summary>
        /// Adds a vehicle to this timestep.
        /// </summary>
        /// <param name="v">VehicleTDB object to add.</param>
        /// <seealso cref="VehicleTDB"/>
        internal void AddVehicle(VehicleTDB v)
        {
            vehicles.Add(v);
        }
    }

    /// <summary>
    /// Implements the traffic DB generated from SUMO. 
    /// The traffic DB is composed of a list of <see cref=" TimeStepTDB"/>, each of them containing
    /// a list of <see cref="VehicleTDB"/>.
    /// </summary>
    public class SumoTrafficDB
    {
        /// <summary>
        /// List of timesteps that populate the DB.
        /// </summary>
        public List<TimeStepTDB> timeStep { get; set; }
        
        private int currentTimeStepIndex { get; set; }

        /// <summary>
        /// Constructor of the class.
        /// </summary>
        /// <remarks> 
        /// The field <see cref="SumoTrafficDB.currentTimeStepIndex"/> is set to -1 by default 
        /// for functionality purposes related to <see cref="SumoTrafficSpawner"/>.
        /// </remarks>
        public SumoTrafficDB()
        {
            this.currentTimeStepIndex = -1;
            timeStep = new List<TimeStepTDB>();
        }

        /// <summary>
        /// Creates a new timestep to the DB. 
        /// </summary>
        /// <param name="time">Time of the simulation.</param>
        internal void InsertNewTimeStep(float time)
        {
            timeStep.Add(new TimeStepTDB(time, currentTimeStepIndex));
            this.currentTimeStepIndex++;
        }

        /// <summary>
        /// Inserts a new vehicle in the current timestep of the DB.
        /// </summary>
        /// <param name="id">Id of the vehicle.</param>
        /// <param name="lon">Longitude position.</param>
        /// <param name="lat">Latitude position.</param>
        /// <param name="type">Type of the vehicle.</param>
        /// <param name="angle">Angle of the vehicle.</param>
        internal void InsertVehicle(string id, string lon, string lat, string type, string angle)
        {
            try
            {
                VehicleTDB v = new VehicleTDB(id, lon, lat, type, angle);
                timeStep[currentTimeStepIndex].AddVehicle(v);
            }
            catch
            {
                Console.Write(" Out of range when InsertVehicle");
            }
        }

        /// <summary>
        /// Gets the total number of timesteps in the DB.
        /// </summary>
        /// <returns>Integer with the number of timesteps in the DB.</returns>
        public int GetNumberOfTimeSteps()
        {
            return timeStep.Count;
        }

        /// <summary>
        /// Gets the total number of vehicles in a certain timestep of the DB.
        /// </summary>
        /// <param name="index">Index of the timestep.</param>
        /// <returns>
        /// Returns an integer with the number of vehicles in the timestep requested,
        /// or -1 if there are no vehicles in that timestep.
        /// </returns>
        public int GetNumberOfVehiclesInTimeStep(int index)
        {
            try
            {
                return timeStep[index].vehicles.Count;
            }
            catch
            {
                //No vehicles
                return -1;
            }
        }

        /// <summary>
        /// Gets a <see cref="VehicleTDB"/> from the DB given the timestep and the vehicle indices.
        /// </summary>
        /// <param name="timeStepIndex">Index of the timestep.</param>
        /// <param name="vehicleIndex">Index of the vehicle.</param>
        /// <returns>Returns the vehicle requested or null if there is no vehicle in that position of the DB.</returns>
        public VehicleTDB GetVehicleAt(int timeStepIndex, int vehicleIndex)
        {
            try
            {
                return timeStep[timeStepIndex].vehicles[vehicleIndex];
            }
            catch
            {
                Console.Write(" Out of range in GetVehicleAt");
                return null;
            }
        }
    }
}
