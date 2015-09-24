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

namespace SumoWCFService
{
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
        /// for functionality purposes.
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
            timeStep.Add(new TimeStepTDB(time, currentTimeStepIndex+1));
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
                System.Diagnostics.Debug.Write(" Out of range when InsertVehicle");
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
        /// Gets the current timestep in which the simulation is running.
        /// </summary>
        /// <returns>TimeStepTDB object with the full information of the current timestep.</returns>
        public TimeStepTDB GetLastTimeStep()
        {
            try
            {
                return timeStep[currentTimeStepIndex];
            }
            catch
            {
                //No timestep
                return null;
            }
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
                System.Diagnostics.Debug.Write(" Out of range in GetVehicleAt");
                return null;
            }
        }
    }
}