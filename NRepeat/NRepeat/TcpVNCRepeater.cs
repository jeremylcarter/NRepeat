using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NRepeat
{

    public class TcpVNCRepeater : IProxy
    {
        public IPEndPoint Server { get; set; }

        public List<VNCRepeaterDefinition> ClientList { get; set; }

        public int Buffer { get; set; }
        public bool Running { get; set; }

        private static TcpListener listener;

        private CancellationTokenSource cancellationTokenSource;

        public event EventHandler<ProxyDataEventArgs> ClientDataSentToServer;
        public event EventHandler<ProxyDataEventArgs> ServerDataSentToClient;
        public event EventHandler<ProxyByteDataEventArgs> BytesTransfered;

        /// <summary>
        /// Start the VNC Repeater
        /// </summary>
        public async void Start()
        {
            if (Running == false)
            {
                cancellationTokenSource = new CancellationTokenSource();
                // Check if the listener is null, this should be after the proxy has been stopped
                if (listener == null)
                {
                    await AcceptConnections();
                }
            }
        }

        /// <summary>
        /// Accept connections
        /// </summary>
        /// <returns></returns>
        private async Task AcceptConnections()
        {
            listener = new TcpListener(Server.Address, Server.Port);
            var bufferSize = Buffer; // Get the current buffer size on start
            listener.Start();
            Running = true;

            // If there is an exception we want to output the message to the console for debugging
            try
            {
                // While the Running bool is true, the listener is not null and there is no cancellation requested
                while (Running && listener != null && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync().WithWaitCancellation(cancellationTokenSource.Token);
                    if (client != null)
                    {
                        // Proxy the data from the client to the server until the end of stream filling the buffer.
                        if (ClientList.Count(definition => definition.ServerEndPoint == client.Client.LocalEndPoint) == 0)
                        {
                            var definition = new VNCRepeaterDefinition()
                            {
                                Authenticated = false,
                                FirstRequest = DateTime.UtcNow,
                                ClientEndPoint = (IPEndPoint)client.Client.RemoteEndPoint,
                                ServerEndPoint = (IPEndPoint)client.Client.LocalEndPoint
                            };

                            this.ClientList.Add(definition);

                            // We havent dealt with this one before.
                            await ProxyClientConnection(client, definition, bufferSize);
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            listener.Stop();
        }

        /// <summary>
        /// Send and receive data between the Client and Server
        /// </summary>
        /// <param name="client"></param>
        /// <param name="definition"></param>
        /// <param name="serverStream"></param>
        /// <param name="clientStream"></param>
        /// <param name="bufferSize"></param>
        /// <param name="cancellationToken"></param>
        private void ProxyClientDataToServer(TcpClient client, VNCRepeaterDefinition definition, NetworkStream serverStream, NetworkStream clientStream, int bufferSize, CancellationToken cancellationToken)
        {
            byte[] message = new byte[bufferSize];
            int clientBytes;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    clientBytes = clientStream.Read(message, 0, bufferSize);
                    if (BytesTransfered != null)
                    {
                        var messageTrimed = message.Reverse().SkipWhile(x => x == 0).Reverse().ToArray();
                        BytesTransfered(this, new ProxyByteDataEventArgs(messageTrimed, "Client"));
                    }
                }
                catch
                {
                    // Socket error - exit loop.  Client will have to reconnect.
                    break;
                }
                if (clientBytes == 0)
                {
                    // Client disconnected. 
                    if (definition != null)
                    {
                        this.ClientList.Remove(definition);
                    }
                    break;
                }
                serverStream.Write(message, 0, clientBytes);

                if (ClientDataSentToServer != null)
                {
                    ClientDataSentToServer(this, new ProxyDataEventArgs(clientBytes));
                }
            }

            message = null;
            client.Close();
        }

        /// <summary>
        /// Send and receive data between the Server and Client
        /// </summary>
        /// <param name="serverStream"></param>
        /// <param name="definition"></param>
        /// <param name="clientStream"></param>
        /// <param name="bufferSize"></param>
        /// <param name="cancellationToken"></param>
        private void ProxyServerDataToClient(NetworkStream serverStream, VNCRepeaterDefinition definition, NetworkStream clientStream, int bufferSize, CancellationToken cancellationToken)
        {
            byte[] message = new byte[bufferSize];
            int serverBytes;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    serverBytes = serverStream.Read(message, 0, bufferSize);
                    if (BytesTransfered != null)
                    {
                        var messageTrimed = message.Reverse().SkipWhile(x => x == 0).Reverse().ToArray();
                        BytesTransfered(this, new ProxyByteDataEventArgs(messageTrimed, "Server"));
                    }
                    clientStream.Write(message, 0, serverBytes);
                }
                catch
                {
                    // Server socket error - exit loop.  Client will have to reconnect.
                    break;
                }
                if (serverBytes == 0)
                {
                    // server disconnected.
                    if (definition != null)
                    {
                        this.ClientList.Remove(definition);
                    }

                    break;

                }
                if (ServerDataSentToClient != null)
                {
                    ServerDataSentToClient(this, new ProxyDataEventArgs(serverBytes));
                }
            }
            message = null;
        }

        /// <summary>
        /// Process the client with a predetermined buffer size
        /// </summary>
        /// <param name="client"></param>
        /// <param name="definition"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        private async Task ProxyClientConnection(TcpClient client, VNCRepeaterDefinition definition, int bufferSize)
        {
            // Get the client stream
            var clientStream = client.GetStream();

            // First we check if the definitions IP addresses are set to the same thing
            if (Equals(definition.ClientEndPoint.Address, definition.ServerEndPoint.Address))
            {
                // Process as a Repeater stream
                byte[] sendBytes = Encoding.ASCII.GetBytes("RFB 000.000\n");
                clientStream.Write(sendBytes, 0, sendBytes.Length);

                var attempts = 0;

                // Send RFB back and forth until an IP Address is given
                // Attempt a few times to send the RFB proto back and forth until we get something
                byte[] message = new byte[bufferSize];
                int clientBytes;
                while (!definition.Authenticated && attempts < 10)
                {
                    attempts += 1;
                    try
                    {
                        clientBytes = clientStream.Read(message, 0, bufferSize);
                        if (BytesTransfered != null)
                        {
                            var messageTrimed = message.Reverse().SkipWhile(x => x == 0).Reverse().ToArray();
                            var messageString = Encoding.ASCII.GetString(messageTrimed);

                            if (messageString.Length > 5 && messageString.Contains(":"))
                            {
                                var split = messageString.Split(":".ToCharArray());
                                if (split.Length >= 2)
                                {
                                    var ip = split[0];
                                    var port = split[1];
                                    var endpoint = new IPEndPoint(IPAddress.Parse(ip), Convert.ToInt32(port));
                                    definition.ClientEndPoint = endpoint;
                                    definition.Authenticated = true;
                                    break;
                                }

                                sendBytes = Encoding.ASCII.GetBytes("RFB 000.000\n");
                                clientStream.Write(sendBytes, 0, sendBytes.Length);
                            }
                        }
                    }
                    catch
                    {
                        // Socket error - exit loop.  Client will have to reconnect.
                        break;
                    }
                    if (clientBytes == 0)
                    {
                        // Client disconnected.
                        break;
                    }

                }
            }
            else
            {
                definition.Authenticated = true;
            }

            if (definition.Authenticated)
            {
                // Handle this client
                // Send the server data to client and client data to server - swap essentially.
                TcpClient server = new TcpClient(definition.ClientEndPoint.Address.ToString(), definition.ClientEndPoint.Port);
                var serverStream = server.GetStream();

                var cancellationToken = cancellationTokenSource.Token;

                try
                {
                    // Continually do the proxying
                    new Task(() => ProxyClientDataToServer(client,definition, serverStream, clientStream, bufferSize, cancellationToken), cancellationToken).Start();
                    new Task(() => ProxyServerDataToClient(serverStream, definition,clientStream, bufferSize, cancellationToken), cancellationToken).Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

        }

        /// <summary>
        /// Stop the VNC Repeater
        /// </summary>
        public void Stop()
        {
            if (listener != null && cancellationTokenSource != null)
            {
                try
                {
                    Running = false;
                    listener.Stop();
                    cancellationTokenSource.Cancel();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                cancellationTokenSource = null;

            }
        }

        public TcpVNCRepeater(short port)
        {
            Server = new IPEndPoint(IPAddress.Any, port);
            Buffer = 4096;
            ClientList = new List<VNCRepeaterDefinition>();
        }
        public TcpVNCRepeater(short port, IPAddress ipAddress)
        {
            Server = new IPEndPoint(ipAddress, port);
            Buffer = 4096;
            ClientList = new List<VNCRepeaterDefinition>();
        }
        public TcpVNCRepeater(short port, IPAddress ipAddress, int buffer)
        {
            Server = new IPEndPoint(ipAddress, port);
            Buffer = buffer;
            ClientList = new List<VNCRepeaterDefinition>();
        }

    }

}
