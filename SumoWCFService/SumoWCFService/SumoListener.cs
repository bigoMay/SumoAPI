/*
 * 
 * SUMO COMMUNICATION API
 * SumoListener.cs
 * 
 * Miguel Ramos Carretero (miguelrc@kth.se) 
 */

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;

namespace SumoWCFService
{
    /// <summary>
    /// Listens and processes data from the FCD output of SUMO when the simulation is running.
    /// </summary>
    public class SumoListener
    {
        private SumoTrafficDB trafficDB;
        private Thread thread;
        private Stream streamSocket;
        private Socket sumoSocket;
        private TcpListener sumoListener;
        private int port = 3654;
        private float time;

        /// <summary>
        /// Constructor of the class. 
        /// </summary>
        /// <param name="remoteFCDPort">Remote port where SUMO is sending the FCD data.</param>
        /// <param name="trafficDB">A SumoTrafficDB instance that is going to be populated with the FCD data.</param>
        public SumoListener(int remoteFCDPort, SumoTrafficDB trafficDB)
        {
            port = remoteFCDPort;
            this.trafficDB = trafficDB;
        }

        /// <summary>
        /// Start listening from the FCD output of SUMO.
        /// </summary>
        public int StartListening()
        {
            //Create a thread for the listener
            ThreadStart ts = new ThreadStart(Listen);
            thread = new Thread(ts);
            thread.Start();
            return 0;
            //System.Diagnostics.Debug.Write("Thread for listening created\n");
        }

        /// <summary>
        /// Auxiliar method for threading. 
        /// Creates a TCP listener to receive data from the FCD output from SUMO. Reads and parses
        /// the XML data containing all the information of the simulation (timesteps and vehicles), 
        /// adding it to the <see cref="trafficDB"/>.
        /// </summary>
        /// <seealso cref="SumoTrafficDB"/>
        internal void Listen()
        {
            XmlReader reader;

            try
            {
                //Start the listener
                //System.Diagnostics.Debug.Write("Starting FCD Listener...\n");
                sumoListener = new TcpListener(IPAddress.Any, port);

                //The following line allows reusing the same port for cases in which listener is stopped and 
                //started inmediately after (case of RestartSimulation in the SumoWCFService):
                sumoListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

                sumoListener.Start();

                //System.Diagnostics.Debug.Write("Listening in port " + port + "...\n");

                //Open a socket to receive data from SUMO 
                sumoSocket = sumoListener.AcceptSocket();

                //Create a xml reader to read from the stream of the socket
                streamSocket = new NetworkStream(sumoSocket);
                reader = XmlReader.Create(streamSocket);
            }
            catch
            {
                System.Diagnostics.Debug.Write(" Error trying to listen from SUMO, is the server running?\n");
                return;
            }

            try
            {
                //Listening
                while (true)
                {
                    //If readable XML chunk, process data
                    if (reader.CanReadValueChunk)
                    {
                        reader.Read();

                        switch (reader.NodeType)
                        {
                            //Reading element    
                            case XmlNodeType.Element:

                                //Reading timeStep
                                if (reader.Name.Equals("timestep"))
                                {
                                    time = float.Parse(reader.GetAttribute("time"));
                                    trafficDB.InsertNewTimeStep(time);
                                }

                                //Reading vehicle
                                if (reader.Name.Equals("vehicle"))
                                {
                                    trafficDB.InsertVehicle(reader.GetAttribute("id"),
                                            reader.GetAttribute("x"),
                                            reader.GetAttribute("y"),
                                            reader.GetAttribute("type"),
                                            reader.GetAttribute("angle")
                                            );
                                }
                                break;

                            //Reading end element
                            case XmlNodeType.EndElement:

                                if (reader.Name.Equals("fcd-export"))
                                {
                                    System.Diagnostics.Debug.Write(" End of the FCD output stream reached\n");
                                    return;
                                }
                                break;
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                System.Diagnostics.Debug.Write(" Thread for listening aborted\n");
                return;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.Write(" Error while listening!\n" + e.ToString());
                return;
            }
            finally
            {
                StopListening();
            }
        }

        /// <summary>
        /// Abort the listening from the FCD output of SUMO.
        /// </summary>
        public void StopListening()
        {
            System.Diagnostics.Debug.WriteLine("Stopping the listener! Abort FCD Listener\n");

            if (sumoListener != null)
            {
                sumoListener.Server.Close(0);
                sumoListener.Stop();
                sumoListener = null;
            }
            if (streamSocket != null)
            {
                streamSocket.Dispose();
                streamSocket.Close();
                streamSocket = null;
            }
            if (sumoSocket != null)
            {
                sumoSocket.Dispose();
                sumoSocket.Close();
                sumoSocket = null;
            }
            if (thread != null)
            {
                thread.Abort();
            }

            thread.Abort();
            return;
        }
    }
}