﻿/*
 * 
 * SUMO COMMUNICATION API
 * SumoTCPCommunication.cs
 * 
 * Miguel Ramos Carretero (miguelrc@kth.se)
 * 
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;

/*
 *
 */
namespace SumoCommunicationAPI
{
    /// <summary>
    /// Handles the TCP communication with SUMO simulator, including connection and command requests. 
    /// </summary>
    internal class SumoTCPCommunication
    {
        private string ip = "127.0.0.1";
        private int port = 3456;
        private int stepNumber = -1;
        private TcpClient sumoClient = new TcpClient();
        private ASCIIEncoding encoder = new ASCIIEncoding();
        private IPEndPoint sumoServerEndPoint;
        private NetworkStream sumoClientStream;
        private bool simulationStarted = false;
        private int millisecondsInSumo = 0;
        private Dictionary<String, Tuple<int, double>> velocities;

        /// <summary>
        /// Constructor of the class.
        /// </summary>
        internal SumoTCPCommunication(string traciRemoteIp, int traciRemotePort)
        {
            this.ip = traciRemoteIp;
            this.port = traciRemotePort;
        }

        /// <summary>
        /// Starts the TCP connection with SUMO. The server end point is defined by the public
        /// fields <see cref="SumoTCPCommunication.ip"/> and <see cref="SumoTCPCommunication.port"/>.
        /// If field <see cref="useLocalHost"/> is true, the server end point will be the local host.
        /// </summary>
        /// <seealso cref="OnConnect"/>
        internal void StartCommunication()
        {
            try
            {
                sumoServerEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                //Console.Write("Making a TCP connection to SUMO (TraCI)");
                sumoClient.BeginConnect(ip, port, new AsyncCallback(OnConnect), null);
            }
            catch (Exception e)
            {
                Console.Write(" Error trying to connect with SUMO, is the server running?");
                Console.Write(e);
                if (sumoClientStream != null)
                    sumoClientStream.Close();
                sumoClient.Close();
                return;
            }
        }

        /// <summary>
        /// Auxiliar method. Handles the asynchronous connection with SUMO. 
        /// </summary>
        /// <param name="result">Status of the connection.</param>
        internal void OnConnect(IAsyncResult result)
        {
            sumoClient.EndConnect(result);

            //Open a socket for communication
            sumoClientStream = sumoClient.GetStream();

            Console.Write(" Connected with SUMO, ready for communication... \n\n");

            simulationStarted = true;
            stepNumber = 0;

            //Initialize the timer for handling vehicles speed and set the imperfection to 0
            velocities = new Dictionary<String, Tuple<int, double>>();
            byte[] resp = ChangeVehTypeState("DEFAULT_VEHTYPE", "imperfection", "", 0.0d);
        }

        /// <summary>
        /// Requests SUMO simulator to perform a single simulation step.
        /// </summary>
        /// <seealso cref="StepSimulation"/>
        internal void RunSingleSimulationStep()
        {
            if (simulationStarted)
            {
                byte[] resp = StepSimulation(0);
                OnTimeStepEvent();
                millisecondsInSumo += 1000;
            }
        }

        /// <summary>
        /// Requests SUMO simulator to perform a step of a certain elapsed time.
        /// </summary>
        /// <remarks>Given the time-discrete nature of the simulation, elapsed times 
        /// values that do not reach the next timestep of the simulation will be stored 
        /// and taken into account in the next call(s) to this method.
        /// </remarks>
        /// <param name="elapsedTime">Elapsed time of the simulation step.</param>
        /// <seealso cref="StepSimulation"/>
        internal void RunElapsedSimulationTime(int elapsedTime)
        {
            if (simulationStarted)
            {
                millisecondsInSumo += elapsedTime;

                //Console.Write(" millisecondsInSumo:" + millisecondsInSumo + "\n");

                // Calculates the number of timesteps that the simulation needs to run 
                // according to the elapsed time given
                int numberOfTimeStepsToRun = (int)Math.Floor((millisecondsInSumo / 1000.0 - stepNumber));

                if (numberOfTimeStepsToRun > 0)
                {
                    for (int i = 0; i < numberOfTimeStepsToRun; i++)
                    {
                        byte[] resp = StepSimulation(0);
                        OnTimeStepEvent();
                    }
                }

                //Console.Write(" stepNumber:" + stepNumber + "\n");

            }
        }

        /// <summary>
        /// Gets the current timestep of the SUMO simulation.
        /// </summary>
        /// <returns>Return the current timestep or -1 if the simulation is not running.</returns>
        internal int GetCurrentTimeStep()
        {
            return stepNumber;
        }

        /// <summary>
        /// Requests SUMO to change the speed of a certain vehicle in the current simulation. Doing
        /// this, the vehicle will keep the same speed until its car-following behaviour is resumed.
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <param name="speed">New speed for the vehicle.</param>
        /// <param name="ms">Milliseconds to reach the new speed.</param>
        /// <seealso cref="ResumeVehicleBehaviour"/>
        internal void ChangeVehicleSpeed(string vehId, double speed, int ms)
        {
            if (simulationStarted)
            {
                byte[] resp = AccelerateVehicle(vehId, speed, ms);

                //Update the velocities
                try
                {
                    velocities.Add(vehId,
                        new Tuple<int, double>(ms + millisecondsInSumo, speed));
                }
                catch (ArgumentException)
                {
                    velocities.Remove(vehId);
                    velocities.Add(vehId,
                        new Tuple<int, double>(ms + millisecondsInSumo, speed));
                }
            }
        }

        /// <summary>
        /// Requests SUMO to resume the car-following behaviour of a certain vehicle in the current
        /// simulation.
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        internal void ResumeVehicleBehaviour(string vehId)
        {
            if (simulationStarted)
            {
                byte[] resp = ChangeVehicleSpeed(vehId, -1.0d);

                if (velocities.ContainsKey(vehId))
                {
                    velocities.Remove(vehId);
                }
            }
        }

        /// <summary>
        /// Requests SUMO simulator to end the simulation. 
        /// </summary>
        /// <seealso cref="CloseSimulation"/>
        internal void EndSimulation()
        {
            if (simulationStarted)
            {
                Console.WriteLine(" Ending simulation...\n");
                byte[] resp = CloseSimulation();
                Console.WriteLine(" End of the simulation\n");
                sumoClientStream.Close();
                sumoClient.Close();
                simulationStarted = false;
            }
        }

        /// <summary>
        /// Adds a new vehicle in the simulation.
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <param name="type">String containing the type of the vehicle.</param>
        /// <param name="routeId">String containing the route id of the vehicle.</param>
        /// <param name="departTime">Specify at which timestep of the simulation the vehicle should depart.</param>
        /// <param name="departPosition">Specify the position from which the vehicle should depart.</param>
        /// <param name="departSpeed">Specify the initial speed of the vehicle.</param>
        /// <param name="departLane">Specify the lane where the vehicle should start.</param>
        internal void AddNewVehicle(string vehId, string type, string routeId, int departTime, double departPosition, double departSpeed, byte departLane)
        {
            if (simulationStarted)
            {
                //Console.WriteLine(" Adding vehicle...\n");
                byte[] resp = AddVehicle(vehId, type, routeId, departTime, departPosition, departSpeed, departLane);
            }
        }

        /// <summary>
        /// Adds a new stop in the vehicle. 
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <param name="edgeId">String containing the edge where the vehicle should stop.</param>
        /// <param name="position">Position in the edge where the vehicle should stop.</param>
        /// <param name="laneIndex">Index of the lane.</param>
        /// <param name="durationInMs">Number of milliseconds the vehicle will stop before continuing its trip.</param>
        internal void AddStopInVehicle(string vehId, string edgeId, double position, byte laneIndex, int durationInMs)
        {
            if (simulationStarted)
            {
                //Console.WriteLine(" Adding stop in vehicle...\n");
                byte[] resp = StopVehicle(vehId, edgeId, position, laneIndex, durationInMs);
            }
        }

        /// <summary>
        /// Converts a Lon-Lat coordinate to a 2D Position (x,y)
        /// </summary>
        /// <param name="lon">Longitude to convert</param>
        /// <param name="lat">Latitude to convert</param>
        /// <returns>Array of doubles containing the conversion (x,y) in positions [0] and [1], respectively.</returns>
        internal double[] LonLatTo2DPosition(double lon, double lat)
        {
            double[] dummy = { -1.0, 1.0 };

            if (simulationStarted)
            {
                byte[] resp = ConvertLonLatTo2DPosition(lon, lat);
                return ReadPositionFromResponse(resp);

                //TODO parse the result and return the conversion
            }

            return dummy;
        }

        /// <summary>
        /// Auxiliar private method. Sends the command <i>end_simulation</i> to SUMO simulator
        /// through the TCP connection. 
        /// </summary>
        /// <returns>Array of bytes with the response from SUMO.</returns>
        /// <seealso cref="SendTCPPacketToSumo"/>
        private byte[] CloseSimulation()
        {
            byte cmd_id = 0x7F;

            byte[] cmd_content = { };

            SendTCPPacketToSumo(WrapSumoTCPPacket(cmd_id, cmd_content));

            return GetResponse();
        }

        /// <summary>
        /// Auxiliar private method. Sends the command <i>simulation_step</i> to SUMO simulator
        /// through the TCP connection. 
        /// </summary>
        /// <param name="targetTime">
        /// Number of milliseconds to be simulated (1 timestep = 1000 ms). 
        /// If 0, request a single timestep.
        /// </param>
        /// <returns>Array of bytes with the response from SUMO.</returns>
        /// <seealso cref="SendTCPPacketToSumo"/>
        private byte[] StepSimulation(int targetTime)
        {
            byte cmd_id = 0x02;

            byte[] bTargetTime = BitConverter.GetBytes(targetTime);
            byte[] cmd_content = { bTargetTime[3], bTargetTime[2], bTargetTime[1], bTargetTime[0] };

            SendTCPPacketToSumo(WrapSumoTCPPacket(cmd_id, cmd_content));
            return GetResponse();
        }

        /// <summary>
        /// Auxiliar private method. Sends the command <i>change_vehicle_state(slow_down)</i> 
        /// to SUMO simulator through the TCP connection. 
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <param name="speed">New speed for the vehicle.</param>
        /// <param name="ms">Milliseconds to reach the new speed.</param>
        /// <returns>Array of bytes with the response from SUMO.</returns>
        /// <seealso cref="SendTCPPacketToSumo"/>
        /// <remarks>This method can be used to smoothly accelerate and decelerate vehicles.</remarks>
        private byte[] AccelerateVehicle(string vehId, double speed, int ms)
        {
            byte cmd_id = 0xc4;
            byte[] cmd_content = { };

            cmd_content = new byte[5 + vehId.Length + 19];

            //Put subcommand 'slow_down' in content
            cmd_content[0] = 0x14;

            //Put vehicle id in content
            byte[] bvehIdLength = BitConverter.GetBytes(vehId.Length);
            byte[] bVehId = encoder.GetBytes(vehId);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bvehIdLength, 0, bvehIdLength.Length);

            Buffer.BlockCopy(bvehIdLength, 0, cmd_content, 1, 4);
            Buffer.BlockCopy(bVehId, 0, cmd_content, 5, vehId.Length);

            //Put parameters of the command in content
            byte valueTypeCompound = 0x0F;
            byte[] itemNumber = BitConverter.GetBytes(2);
            byte valueTypeDouble = 0x0B;
            byte[] bSpeed = BitConverter.GetBytes(speed);
            byte valueTypeInt = 0x09;
            byte[] bMs = BitConverter.GetBytes(ms);

            byte[] parameters = {valueTypeCompound, itemNumber[3], itemNumber[2], 
                        itemNumber[1], itemNumber[0], valueTypeDouble, bSpeed[7],
                        bSpeed[6], bSpeed[5], bSpeed[4], bSpeed[3], bSpeed[2], bSpeed[1], 
                        bSpeed[0], valueTypeInt, bMs[3], bMs[2], bMs[1], bMs[0]};

            Buffer.BlockCopy(parameters, 0, cmd_content, 5 + vehId.Length, parameters.Length);

            SendTCPPacketToSumo(WrapSumoTCPPacket(cmd_id, cmd_content));
            return GetResponse();
        }

        /// <summary>
        /// Auxiliar private method. Sends the command <i>change_vehicle_state(speed)</i> 
        /// to SUMO simulator through the TCP connection.  
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <param name="speed">New speed for the vehicle.</param>
        /// <returns>Array of bytes with the response from SUMO.</returns>
        /// <seealso cref="SendTCPPacketToSumo"/>
        /// <remarks>This method will change inmmediately the speed of the vehicle, and will
        /// keep it constant until a new value is given. The vehicle will resume its normal 
        /// behaviour if -1 is given in the field <see cref="speed"/>.</remarks>
        private byte[] ChangeVehicleSpeed(string vehId, double speed)
        {
            byte cmd_id = 0xc4;
            byte[] cmd_content = { };

            cmd_content = new byte[5 + vehId.Length + 9];

            //Put subcommand in content
            cmd_content[0] = 0x40;

            //Put vehicle id in content
            byte[] bvehIdLength = BitConverter.GetBytes(vehId.Length);
            byte[] bVehId = encoder.GetBytes(vehId);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bvehIdLength, 0, bvehIdLength.Length);

            Buffer.BlockCopy(bvehIdLength, 0, cmd_content, 1, 4);
            Buffer.BlockCopy(bVehId, 0, cmd_content, 5, vehId.Length);

            //Put parameters of the command in content
            byte valueTypeDouble = 0x0B;
            byte[] bSpeed = BitConverter.GetBytes(speed);

            byte[] parameters = {valueTypeDouble, bSpeed[7],
                        bSpeed[6], bSpeed[5], bSpeed[4], bSpeed[3], bSpeed[2], bSpeed[1], 
                        bSpeed[0]};

            Buffer.BlockCopy(parameters, 0, cmd_content, 5 + vehId.Length, parameters.Length);

            SendTCPPacketToSumo(WrapSumoTCPPacket(cmd_id, cmd_content));
            return GetResponse();
        }

        /// <summary>
        /// Auxiliar private method. Sends the command <i>change_vehicle_state(stop)</i> 
        /// to SUMO simulator through the TCP connection.  
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <param name="edgeId">String containing the edge where the vehicle should stop.</param>
        /// <param name="position">Position in the edge where the vehicle should stop.</param>
        /// <param name="laneIndex">Index of the lane.</param>
        /// <param name="durationInMs">Number of milliseconds the vehicle will stop before continuing its trip.</param>
        /// <returns>Array of bytes with the response from SUMO.</returns>
        /// <seealso cref="SendTCPPacketToSumo"/>
        private byte[] StopVehicle(string vehId, string edgeId, double position, byte laneIndex, int durationInMs)
        {
            byte cmd_id = 0xc4;
            byte[] cmd_content = { };

            cmd_content = new byte[5 + vehId.Length + 5 + edgeId.Length + 5 + 9 + 2 + 5];

            //Put subcommand in content
            cmd_content[0] = 0x12;

            //Put vehicle id in content
            byte[] bvehIdLength = BitConverter.GetBytes(vehId.Length);
            byte[] bVehId = encoder.GetBytes(vehId);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bvehIdLength, 0, bvehIdLength.Length);

            Buffer.BlockCopy(bvehIdLength, 0, cmd_content, 1, 4);
            Buffer.BlockCopy(bVehId, 0, cmd_content, 5, vehId.Length);

            //Put parameters of the command in content
            cmd_content[5 + vehId.Length] = 0x0F;

            //Number of elements
            byte[] noElements = BitConverter.GetBytes(4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(noElements, 0, noElements.Length);
            Buffer.BlockCopy(noElements, 0, cmd_content, 6 + vehId.Length, 4);

            //Edge id
            cmd_content[10 + vehId.Length] = 0x0C;
            byte[] bEdgeIdLength = BitConverter.GetBytes(edgeId.Length);
            byte[] bEdgeId = encoder.GetBytes(edgeId);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bEdgeIdLength, 0, bEdgeIdLength.Length);

            Buffer.BlockCopy(bEdgeIdLength, 0, cmd_content, 11 + vehId.Length, 4);
            Buffer.BlockCopy(bEdgeId, 0, cmd_content, 15 + vehId.Length, edgeId.Length);

            //Position
            byte valueTypeDouble = 0x0B;
            byte[] bPosition = BitConverter.GetBytes(position);

            //Lane
            byte valueTypeByte = 0x08;

            //Duration
            byte valueTypeInt = 0x09;
            byte[] bDuration = BitConverter.GetBytes(durationInMs);

            byte[] parameters = { valueTypeDouble, bPosition[7], bPosition[6], bPosition[5], bPosition[4], bPosition[3], bPosition[2], bPosition[1], bPosition[0],
                              valueTypeByte, laneIndex, valueTypeInt, bDuration[3], bDuration[2], bDuration[1], bDuration[0]};

            Buffer.BlockCopy(parameters, 0, cmd_content, 15 + vehId.Length + edgeId.Length, parameters.Length);

            SendTCPPacketToSumo(WrapSumoTCPPacket(cmd_id, cmd_content));
            return GetResponse();
        }

        /// <summary>
        /// Auxiliar private method. Sends the command <i>change_lane_state(allowed vehicle 
        /// classes)</i> to SUMO simulator through the TCP connection. 
        /// </summary>
        /// <param name="laneId">Id of the lane.</param>
        /// <param name="allowedClasses">String list with the allowed vehicle classes.</param>
        /// <returns>Array of bytes with the response from SUMO.</returns>
        /// <seealso cref="SendTCPPacketToSumo"/>
        private byte[] ChangeAllowanceInLane(string laneId, string[] allowedClasses, bool allow)
        {
            byte cmd_id = 0xc3;
            byte[] cmd_content = { };

            cmd_content = new byte[5 + laneId.Length + 5 + 4 * allowedClasses.Length + numberOfBytesInStringArray(allowedClasses)];

            //Put subcommand in content
            if (allow)
                cmd_content[0] = 0x34;
            else
                cmd_content[0] = 0x35;

            //Put vehicle id in content
            byte[] blaneIdLength = BitConverter.GetBytes(laneId.Length);
            byte[] blaneId = encoder.GetBytes(laneId);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(blaneIdLength, 0, blaneIdLength.Length);

            Buffer.BlockCopy(blaneIdLength, 0, cmd_content, 1, 4);
            Buffer.BlockCopy(blaneId, 0, cmd_content, 5, laneId.Length);

            //Put value type (stringlist) in content
            cmd_content[5 + laneId.Length] = 0x0E;

            //Put length of the stringlist in content
            byte[] ballowedClassesLength = BitConverter.GetBytes(allowedClasses.Length);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(ballowedClassesLength, 0, ballowedClassesLength.Length);

            Buffer.BlockCopy(ballowedClassesLength, 0, cmd_content, 5 + laneId.Length + 1, 4);

            int currentIndexOf_cmd_content = 5 + laneId.Length + 5;

            //Put the information of each string in the list
            for (int i = 0; i < allowedClasses.Length; i++)
            {
                byte[] bclassLength = BitConverter.GetBytes(allowedClasses[i].Length);
                byte[] bclass = encoder.GetBytes(allowedClasses[i]);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bclassLength, 0, blaneIdLength.Length);

                Console.WriteLine(currentIndexOf_cmd_content);

                Buffer.BlockCopy(bclassLength, 0, cmd_content, currentIndexOf_cmd_content, 4);
                Buffer.BlockCopy(bclass, 0, cmd_content, currentIndexOf_cmd_content + 4, bclass.Length);

                currentIndexOf_cmd_content += 4 + allowedClasses[i].Length;
            }

            SendTCPPacketToSumo(WrapSumoTCPPacket(cmd_id, cmd_content));
            return GetResponse();
        }

        /// <summary>
        /// Auxiliar private method. Sends the command <i>change_vehicle_state(add)</i> 
        /// to SUMO simulator through the TCP connection.  
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <param name="type">String containing the type of the vehicle.</param>
        /// <param name="routeId">String containing the route id of the vehicle.</param>
        /// <param name="departTime">Specify at which timestep of the simulation the vehicle should depart.</param>
        /// <param name="departPosition">Specify the position from which the vehicle should depart.</param>
        /// <param name="departSpeed">Specify the initial speed of the vehicle.</param>
        /// <param name="departLane">Specify the lane where the vehicle should start.</param>
        /// <returns>Array of bytes with the response from SUMO.</returns>
        /// <seealso cref="SendTCPPacketToSumo"/>
        private byte[] AddVehicle(string vehId, string type, string routeId, int departTime, double departPosition, double departSpeed, byte departLane)
        {
            byte cmd_id = 0xc4;
            byte[] cmd_content = { };

            cmd_content = new byte[5 + vehId.Length + 5 + type.Length + 5 + routeId.Length + 5 + 5 + 9 + 9 + 2];

            //Put subcommand in content
            cmd_content[0] = 0x80;

            //Put vehicle id in content
            byte[] bvehIdLength = BitConverter.GetBytes(vehId.Length);
            byte[] bVehId = encoder.GetBytes(vehId);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bvehIdLength, 0, bvehIdLength.Length);

            Buffer.BlockCopy(bvehIdLength, 0, cmd_content, 1, 4);
            Buffer.BlockCopy(bVehId, 0, cmd_content, 5, vehId.Length);

            //Value type compound
            cmd_content[5 + vehId.Length] = 0x0F;

            //Number of elements
            byte[] noElements = BitConverter.GetBytes(6);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(noElements, 0, noElements.Length);
            Buffer.BlockCopy(noElements, 0, cmd_content, 6 + vehId.Length, 4);

            //Vehicle type
            cmd_content[10 + vehId.Length] = 0x0C;
            byte[] bTypeLength = BitConverter.GetBytes(type.Length);
            byte[] bType = encoder.GetBytes(type);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bTypeLength, 0, bTypeLength.Length);

            Buffer.BlockCopy(bTypeLength, 0, cmd_content, 11 + vehId.Length, 4);
            Buffer.BlockCopy(bType, 0, cmd_content, 15 + vehId.Length, type.Length);

            //Route ID
            cmd_content[15 + vehId.Length + type.Length] = 0x0C;
            byte[] bRouteIdLength = BitConverter.GetBytes(routeId.Length);
            byte[] bRouteId = encoder.GetBytes(routeId);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bRouteIdLength, 0, bRouteIdLength.Length);

            Buffer.BlockCopy(bRouteIdLength, 0, cmd_content, 16 + vehId.Length + type.Length, 4);
            Buffer.BlockCopy(bRouteId, 0, cmd_content, 20 + vehId.Length + type.Length, routeId.Length);

            //Depart time
            byte valueTypeInt = 0x09;
            byte[] bTime = BitConverter.GetBytes(departTime);

            //Depart position and speed
            byte valueTypeDouble = 0x0B;
            byte[] bPosition = BitConverter.GetBytes(departPosition);
            byte[] bSpeed = BitConverter.GetBytes(departSpeed);

            //Depart lane
            byte valueTypeByte = 0x08;

            byte[] parameters = { valueTypeInt, bTime[3], bTime[2], bTime[1], bTime[0], 
                                valueTypeDouble, bPosition[7], bPosition[6], bPosition[5], bPosition[4], bPosition[3], bPosition[2], bPosition[1], bPosition[0], 
                                valueTypeDouble, bSpeed[7], bSpeed[6], bSpeed[5], bSpeed[4], bSpeed[3], bSpeed[2], bSpeed[1], bSpeed[0],
                                valueTypeByte, departLane};

            Buffer.BlockCopy(parameters, 0, cmd_content, 20 + vehId.Length + type.Length + routeId.Length, parameters.Length);

            SendTCPPacketToSumo(WrapSumoTCPPacket(cmd_id, cmd_content));
            return GetResponse();
        }

        /// <summary>
        /// Auxiliar private method. Sends the command <i>change_egde_state(change travel/effort)</i> 
        /// to SUMO simulator through the TCP connection. It always send a high value in order to
        /// block the edge.  
        /// </summary>
        /// <param name="edgeId">Id of the edge to block.</param>
        /// <param name="usingTravelTime">If true, it will block the edge increasing its travel time, 
        /// otherwise it will block it increasing the global effort.</param>
        /// <returns>Array of bytes with the response from SUMO.</returns>
        /// <seealso cref="SendTCPPacketToSumo"/>
        private byte[] BlockEdge(string edgeId, bool usingTravelTime)
        {
            byte cmd_id = 0xca;
            byte[] cmd_content = { };

            cmd_content = new byte[5 + edgeId.Length + 24];

            //Put subcommand in content
            if (usingTravelTime)
                cmd_content[0] = 0x58;
            else
                cmd_content[0] = 0x59;

            //Put vehicle id in content
            byte[] bedgeIdLength = BitConverter.GetBytes(edgeId.Length);
            byte[] bedgeId = encoder.GetBytes(edgeId);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bedgeIdLength, 0, bedgeIdLength.Length);

            Buffer.BlockCopy(bedgeIdLength, 0, cmd_content, 1, 4);
            Buffer.BlockCopy(bedgeId, 0, cmd_content, 5, bedgeId.Length);

            //Put parameters of the command in content
            byte valueTypeCompound = 0x0F;

            double value = Double.MaxValue;
            byte[] bvalue = BitConverter.GetBytes(value);

            int numberOfElem = 3;
            byte[] bnumberOfElem = BitConverter.GetBytes(numberOfElem);

            int begin = 0;
            byte[] bbegin = BitConverter.GetBytes(begin);

            int end = 10000;
            byte[] bend = BitConverter.GetBytes(end);

            byte[] parameters = { valueTypeCompound, bnumberOfElem[3], bnumberOfElem[2], bnumberOfElem[1], bnumberOfElem[0], 
                                0x09, bbegin[3], bbegin[2], bbegin[1], bbegin[0], 0x09, bend[3], bend[2], bend[1], bend[0], 0x0b, 
                                bvalue[7], bvalue[6], bvalue[5], bvalue[4], bvalue[3], bvalue[2], bvalue[1], bvalue[0] };

            Buffer.BlockCopy(parameters, 0, cmd_content, 5 + edgeId.Length, parameters.Length);

            SendTCPPacketToSumo(WrapSumoTCPPacket(cmd_id, cmd_content));
            return GetResponse();
        }

        /// <summary>
        /// Auxiliar private method. Sends the command <i>change_vehicle_type_state(subcommand)</i> 
        /// to SUMO simulator through the TCP connection.
        /// </summary>
        /// <param name="vehId">String containing the vehicle id.</param>
        /// <param name="subcommand">Subcommand to be requested in SUMO.</param>
        /// <param name="stringValue">String value (several meanings depending on the subcommand).</param>
        /// <param name="doubleValue">Double value (several meanings depending on the subcommand).</param>
        /// <returns>Array of bytes with the response from SUMO.</returns>
        /// <seealso cref="SendTCPPacketToSumo"/>
        private byte[] ChangeVehTypeState(string vehId, string subcommand, string stringValue, double doubleValue)
        {
            byte cmd_id = 0xc5;
            byte[] cmd_content = { };
            byte valueType;
            bool isString;

            //Put subcommand in content
            switch (subcommand)
            {
                case "vehicle class":
                    cmd_content = new byte[5 + vehId.Length + 5 + stringValue.Length];
                    valueType = 0x0c;
                    isString = true;
                    cmd_content[0] = 0x49;

                    break;
                case "imperfection":
                    cmd_content = new byte[5 + vehId.Length + 9];
                    valueType = 0x0b;
                    isString = false;
                    cmd_content[0] = 0x5d;
                    break;
                default:
                    Console.WriteLine(" Command not implemented yet in TCPCommunication\n");
                    return null;
            }

            //Put vehicle id in content
            byte[] bvehIdLength = BitConverter.GetBytes(vehId.Length);
            byte[] bvehId = encoder.GetBytes(vehId);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bvehIdLength, 0, bvehIdLength.Length);

            Buffer.BlockCopy(bvehIdLength, 0, cmd_content, 1, 4);
            Buffer.BlockCopy(bvehId, 0, cmd_content, 5, vehId.Length);

            cmd_content[5 + vehId.Length] = valueType;

            if (isString)
            {
                byte[] bstringLength = BitConverter.GetBytes(stringValue.Length);
                byte[] bstring = encoder.GetBytes(stringValue);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bstringLength, 0, bstringLength.Length);

                Buffer.BlockCopy(bstringLength, 0, cmd_content, vehId.Length + 6, 4);
                Buffer.BlockCopy(bstring, 0, cmd_content, vehId.Length + 10, bstring.Length);
            }
            else
            {
                byte[] bvalue = BitConverter.GetBytes(doubleValue);
                byte[] parameters = { bvalue[7], bvalue[6], bvalue[5], bvalue[4], bvalue[3], bvalue[2], bvalue[1], bvalue[0] };

                Buffer.BlockCopy(parameters, 0, cmd_content, vehId.Length + 6, parameters.Length);
            }

            SendTCPPacketToSumo(WrapSumoTCPPacket(cmd_id, cmd_content));
            return GetResponse();
        }


        /// <summary>
        /// Auxiliar private method. Sends the command <i>position_conversion</i> 
        /// to SUMO simulator through the TCP connection.
        /// </summary>
        /// <param name="lon">Longitude to convert.</param>
        /// <param name="lat">Latitude to convert.</param>
        /// <returns>Array of bytes with the response from SUMO.</returns>
        /// <seealso cref="SendTCPPacketToSumo"/>
        private byte[] ConvertLonLatTo2DPosition(double lon, double lat)
        {
            byte cmd_id = 0xab;
            byte variable = 0x82;
            byte[] cmd_content = { };

            cmd_content = new byte[5 + 5 + 17 + 1 + 1];

            //Put parameters of the command in content
            byte valueTypeCompound = 0x0F;

            int numberOfElem = 2;
            byte[] bnumberOfElem = BitConverter.GetBytes(numberOfElem);

            byte[] bLon = BitConverter.GetBytes(lon);
            byte[] bLat = BitConverter.GetBytes(lat);

            byte[] parameters = { variable, 0x00, 0x00, 0x00, 0x00, valueTypeCompound, bnumberOfElem[3], bnumberOfElem[2], bnumberOfElem[1], bnumberOfElem[0], 
                                0x00, bLon[7], bLon[6], bLon[5], bLon[4], bLon[3], bLon[2], bLon[1], bLon[0], bLat[7], 
                                bLat[6], bLat[5], bLat[4], bLat[3], bLat[2], bLat[1], bLat[0], 0x07, 0x01};

            Buffer.BlockCopy(parameters, 0, cmd_content, 0, parameters.Length);

            SendTCPPacketToSumo(WrapSumoTCPPacket(cmd_id, cmd_content));
            return GetResponse();
        }

        /// <summary>
        /// Auxiliar private method. Sends a TCP packet to SUMO.
        /// </summary>
        /// <param name="tcpPacket">Array of bytes with the TCP packet to be sent.</param>
        /// <seealso cref="WrapSumoTCPPacket"/>
        private void SendTCPPacketToSumo(byte[] tcpPacket)
        {
            try
            {
                sumoClientStream.Write(tcpPacket, 0, tcpPacket.Length);
            }
            catch
            {
                Console.WriteLine(" Error sending the TCP packet to SUMO\n");
                return;
            }
        }

        /// <summary>
        /// Auxiliar private method. Creates a TCP packet ready to be sent to SUMO simulator.
        /// </summary>
        /// <param name="cmd_id">Id of the command to be sent.</param>
        /// <param name="cmd_content">Information to be sent.</param>
        /// <returns>Array of bytes with the TCP packet ready to be sent to SUMO.</returns>
        private byte[] WrapSumoTCPPacket(byte cmd_id, byte[] cmd_content)
        {
            try
            {
                //Calculate the final length of the command
                int cmdLength = cmd_content.Length + 2;

                //Create the command header
                byte[] cmd_header;

                if (cmdLength < 255)
                {
                    cmd_header = new byte[2];
                    cmd_header[0] = (byte)cmdLength;
                    cmd_header[1] = cmd_id;
                }
                else
                {
                    cmd_header = new byte[6];
                    byte[] bCmdLength = BitConverter.GetBytes(cmdLength);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bCmdLength, 0, bCmdLength.Length);

                    cmd_header[0] = 0x00;
                    cmd_header[1] = bCmdLength[0];
                    cmd_header[2] = bCmdLength[1];
                    cmd_header[3] = bCmdLength[2];
                    cmd_header[4] = bCmdLength[3];
                    cmd_header[5] = cmd_id;
                }

                //Create the final command message
                byte[] finalCommandMessage = new byte[cmdLength];
                Buffer.BlockCopy(cmd_header, 0, finalCommandMessage, 0, cmd_header.Length);
                Buffer.BlockCopy(cmd_content, 0, finalCommandMessage, cmd_header.Length, cmd_content.Length);

                //Calculate the final length of the TCP packet
                int packetLength = finalCommandMessage.Length + 4;
                byte[] bytesPacketLength = BitConverter.GetBytes(packetLength);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytesPacketLength, 0, bytesPacketLength.Length);

                //Create the final TCP packet
                byte[] tcpPacket = new byte[packetLength];
                Buffer.BlockCopy(bytesPacketLength, 0, tcpPacket, 0, 4);
                Buffer.BlockCopy(finalCommandMessage, 0, tcpPacket, bytesPacketLength.Length, finalCommandMessage.Length);

                return tcpPacket;
            }
            catch
            {
                Console.WriteLine("Error during WrapSumoTCPPacket operation\n");
                return null;
            }
        }

        /// <summary>
        /// Private auxiliar method. Gets the response from SUMO after sending a packet.
        /// </summary>
        /// <returns>Array of bytes with the response from SUMO.</returns>
        private byte[] GetResponse()
        {
            try
            {
                //Read the length of the description
                byte[] stringHeader = new byte[4];
                sumoClientStream.Read(stringHeader, 0, 4);
                int descrLength = readIntFromBuffer(stringHeader, 0);

                //Read the description
                byte[] responseDescription = new byte[descrLength];
                int bytesRead = sumoClientStream.Read(responseDescription, 0, descrLength);

                //Console.WriteLine(bytesRead);

                //Create the final message
                byte[] response = new byte[bytesRead + 4];
                Buffer.BlockCopy(stringHeader, 0, response, 0, stringHeader.Length);
                Buffer.BlockCopy(responseDescription, 0, response, stringHeader.Length, bytesRead);

                //Console.WriteLine(BitConverter.ToString(response, 0, response.Length));

                return response;
            }
            catch
            {
                Console.WriteLine(" Error getting response from SUMO\n");
                return null;
            }
        }

        /// <summary>
        /// Private auxiliar method. Read a position inside a response given by SUMO for the command 0xab.
        /// </summary>
        /// <param name="resp">Array of bytes with the response to be read.</param>
        /// <returns></returns>
        private double[] ReadPositionFromResponse(byte[] resp)
        {
            byte command, result, responseCommand, variable, positionType;
            int stringLength;
            double[] position = {};

            try
            {
                int respIndex = 0;

                //Read the length of the TCP message
                readIntFromBuffer(resp, respIndex);
                respIndex += 4;

                //Skip the length of the basic respond
                respIndex++;

                //Read the command
                command = resp[respIndex++];

                //Read the result
                result = resp[respIndex++];

                //Continue if result is success
                if (result == 0x00)
                {
                    //Skip description
                    stringLength = readIntFromBuffer(resp, respIndex);
                    respIndex += 4 + stringLength;

                    //Skip the length of the position response
                    respIndex++;

                    //Read the response command
                    responseCommand = resp[respIndex++];

                    //Read the variable
                    variable = resp[respIndex++];

                    //Skip the id string
                    stringLength = readIntFromBuffer(resp, respIndex);
                    respIndex += 4 + stringLength;

                    //Read the type of the position
                    positionType = resp[respIndex++];

                    //Read the position according to the type
                    if (positionType == 0x01)
                    {
                        position = new double[2];
                        position[0] = readDoubleFromBuffer(resp, respIndex);
                        respIndex += 8;
                        position[1] = readDoubleFromBuffer(resp, respIndex);
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error reading position from response\n");
                return position;
            }

            return position;
        }

        private double readDoubleFromBuffer(byte[] buffer, int index)
        {
            try
            {
                byte[] doubleInBytes = new byte[8];
                Buffer.BlockCopy(buffer, index, doubleInBytes, 0, 8);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(doubleInBytes, 0, doubleInBytes.Length);
                return BitConverter.ToDouble(doubleInBytes, 0);
            }
            catch
            {
                Console.WriteLine(" Error reading double from buffer\n");
                return -1;
            }
        }

        /// <summary>
        /// Auxiliar private method. Reads an integer contained in an array of bytes. 
        /// </summary>
        /// <param name="buffer">Array of bytes.</param>
        /// <param name="index">Position in the array where the integer starts.</param>
        /// <returns>Integer read from the buffer.</returns>
        private int readIntFromBuffer(byte[] buffer, int index)
        {
            try
            {
                byte[] intInBytes = new byte[4];
                Buffer.BlockCopy(buffer, index, intInBytes, 0, 4);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(intInBytes, 0, intInBytes.Length);
                return BitConverter.ToInt32(intInBytes, 0);
            }
            catch
            {
                Console.WriteLine(" Error reading integer from buffer\n");
                return -1;
            }
        }

        /// <summary>
        /// Auxiliar private method. Performs additional operations in the script after running 
        /// a timestep in the simulation.
        /// </summary>
        /// <remarks>This method is mainly used to control the speed of the vehicles that are
        /// breaking or accelerating.</remarks>
        private void OnTimeStepEvent()
        {
            //Increment the counter
            stepNumber++;

            List<String> keys = new List<String>();

            //Collect the vehicles that have finished accelerating/decelerating
            foreach (String s in velocities.Keys)
            {
                if (millisecondsInSumo < velocities[s].Item1)
                {
                    keys.Add(s);
                }
            }

            //Change the constant speed of the vehicles that have been collected
            foreach (String s in keys)
            {
                byte[] resp1 = ChangeVehicleSpeed(s, velocities[s].Item2);
                velocities.Remove(s);
            }
        }

        /// <summary>
        /// Auxiliar private method. Calculates the number of bytes in an array of strings.
        /// </summary>
        /// <param name="allowedClasses">Array of strings.</param>
        /// <returns>Total number of bytes in the array of strings.</returns>
        private int numberOfBytesInStringArray(string[] allowedClasses)
        {
            int totalLength = 0;

            for (int i = 0; i < allowedClasses.Length; i++)
            {
                totalLength += allowedClasses[i].Length;
            }

            return totalLength;
        }

        /// <summary>
        /// Destroy method for the client stream
        /// </summary>
        internal void OnDestroy()
        {
            if (sumoClientStream != null)
            {
                sumoClientStream.Close();
                sumoClientStream.Dispose();
            }
            if (sumoClient != null)
                sumoClient.Close();
        }

        /// <summary>
        /// Get the current version of SUMO
        /// </summary>
        /// <returns></returns>
        private byte[] GetVersion()
        {
            byte cmd_id = 0x00;

            byte[] cmd_content = { };

            SendTCPPacketToSumo(WrapSumoTCPPacket(cmd_id, cmd_content));
            return GetResponse();
        }

        /// <summary>
        /// Request SUMO a not implemented command (only for debugging purposes)
        /// </summary>
        /// <returns></returns>
        private byte[] NotImplementedCommand()
        {
            byte cmd_id = 0xff;
            byte[] cmd_content = { };

            SendTCPPacketToSumo(WrapSumoTCPPacket(cmd_id, cmd_content));
            return GetResponse();
        }

        /// <summary>
        /// Print a response sent by SUMO
        /// </summary>
        /// <param name="resp"></param>
        private void PrintResponse(byte[] resp)
        {
            try
            {
                int respIndex = 0;

                //Read the length of the TCP message
                readIntFromBuffer(resp, respIndex);
                respIndex += 4;

                //Print status response
                respIndex += PrintStatusResponse(resp, respIndex);

                //Print version info (case cmd 0x00)
                if (resp[5] == 0x00)
                {
                    respIndex += PrintVersion(resp, respIndex);
                }
            }
            catch
            {
                Console.WriteLine("Error printing response\n");
                return;
            }
        }

        /// <summary>
        /// Print a status response sent by SUMO (Aux. function of PrintResponse)
        /// </summary>
        /// <param name="resp"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private int PrintStatusResponse(byte[] resp, int index)
        {
            try
            {
                //Read length of the message
                int messageLength = resp[index++];
                if (messageLength == 0)
                {
                    messageLength = readIntFromBuffer(resp, index);
                    index += 4;
                }

                //Skip command
                index++;

                //Print the result
                byte result = resp[index++];
                //UnityEngine.Debug.Log("\nResult: " + result.ToString() + "\n");
                Console.WriteLine("\nResult: " + result.ToString() + "\n");

                //Print the description
                int descrLength = readIntFromBuffer(resp, index);
                index += 4;
                //UnityEngine.Debug.Log("Description: " + encoder.GetString(resp, index, descrLength) + "\n");
                Console.WriteLine("Description: " + encoder.GetString(resp, index, descrLength) + "\n");

                return messageLength;
            }
            catch
            {
                Console.WriteLine("Error printing status response\n");
                return -1;
            }
        }

        /// <summary>
        /// Print the version message sent by SUMO (Aux. function of PrintResponse)
        /// </summary>
        /// <param name="resp"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private int PrintVersion(byte[] resp, int index)
        {
            try
            {
                //Read length of the message
                int messageLength = resp[index++];
                if (messageLength == 0)
                {
                    messageLength = readIntFromBuffer(resp, index);
                    index += 4;
                }

                //Skip command
                index++;

                //Print the API version
                int apiVersion = readIntFromBuffer(resp, index);
                index += 4;
                Console.WriteLine("API version: " + apiVersion.ToString() + "\n");

                //Print the software version
                int descrLength = readIntFromBuffer(resp, index);
                index += 4;
                Console.WriteLine("Software version: " + encoder.GetString(resp, index, descrLength) + "\n");

                return messageLength;
            }
            catch
            {
                Console.WriteLine("Error printing version\n");
                return -1;
            }
        }
    }

    //--------------------------------------------------------------------------------------------

    /// <summary>
    /// Class that defines a Tuple (needed for Unity).
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    internal class Tuple<T1, T2>
    {
        private T1 item1 { get; set; }
        private T2 item2 { get; set; }

        /// <summary>
        /// Constructor of the class.
        /// </summary>
        /// <param name="item1">Value of field item1.</param>
        /// <param name="item2">Value of field item2.</param>
        internal Tuple(T1 item1, T2 item2)
        {
            this.item1 = item1;
            this.item2 = item2;
        }

        /// <summary>
        /// Get field item1.
        /// </summary>
        internal T1 Item1
        {
            get { return item1; }
        }

        /// <summary>
        /// Get field item2.
        /// </summary>
        internal T2 Item2
        {
            get { return item2; }
        }
    }

    //--------------------------------------------------------------------------------------------

}
