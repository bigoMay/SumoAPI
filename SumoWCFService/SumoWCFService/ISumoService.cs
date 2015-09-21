using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace SumoWCFService
{
    [ServiceContract]
    public interface ISumoService
    {
        [OperationContract]
        int InitializeSumo();

        [OperationContract]
        int EndSimulation();

        [OperationContract]
        TimeStepTDB GetCurrentTimeStep();

        [OperationContract]
        TimeStepTDB GetTimeStep(int index);

        [OperationContract]
        int GetNumberOfTimeSteps();
                
        [OperationContract]
        int RunSingleStep();

        [OperationContract]
        int RunElapsedTime(int elapsedTime);

        [OperationContract]
        int ChangeVehicleSpeed(string vehId, double speed, int ms);

        [OperationContract]
        int ChangeVehicleMaxSpeed(string vehId, double maxSpeed);

        [OperationContract]
        int ResumeVehicleBehaviour(string vehId);

        [OperationContract]
        int AddNewVehicle(string vehId, string type, string routeId, int departTime, double departPosition, double departSpeed, byte departLane);

        [OperationContract]
        int AddStopInVehicle(string vehId, string edgeId, double position, byte laneIndex, int durationInMs);

        [OperationContract]
        double[] LonLatTo2DPosition(double lon, double lat);

        [OperationContract]
        string[] GetEdgeList();

        [OperationContract]
        double GetVehicleTypeLength(string vehType);

        [OperationContract]
        double GetVehicleTypeWidth(string vehType);

        [OperationContract]
        double GetVehicleTypeMaxAccel(string vehType);

        [OperationContract]
        double GetVehicleTypeMaxSpeed(string vehType);

        [OperationContract]
        double GetVehicleTypeMaxDecel(string vehType);

        [OperationContract]
        string GetVehicleRouteId(string vehId);

        [OperationContract]
        int GetVehicleLaneIndex(string vehId);

        [OperationContract]
        string[] GetEdgesInRoute(string routeId);
    }

    /// <summary>
    /// Class that defines a timestep in the Sumo Traffic DB.
    /// A timestep is defined by a time and the list of vehicles of that time. 
    /// </summary>
    /// <seealso cref="SumoTrafficDB"/>
    [DataContract]
    public class TimeStepTDB
    {
        /// <summary>
        /// List of vehicles that populate a certain timestep in the DB.
        /// </summary>
        [DataMember]
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
    /// Class that defines a vehicle in the Sumo Traffic DB. 
    /// </summary>
    /// <seealso cref="SumoTrafficDB"/>
    [DataContract]
    public class VehicleTDB
    {
        /// <summary>
        /// Vehicle id.
        /// </summary>
        [DataMember]
        public string id { get; set; }

        /// <summary>
        /// Latitude position of the vehicle.
        /// </summary>
        [DataMember]
        public float latitude { get; set; }

        /// <summary>
        /// Longitude position of the vehicle.
        /// </summary>
        [DataMember]
        public float longitude { get; set; }

        /// <summary>
        /// Type of the vehicle in SUMO.
        /// </summary>
        [DataMember]
        public string type { get; set; }

        /// <summary>
        /// Angle of the vehicle.
        /// </summary>
        [DataMember]
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
}
