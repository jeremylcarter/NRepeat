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

    public class TcpProxy : IProxy
    {
        public IPEndPoint Server { get; set; }
        public IPEndPoint Client { get; set; }
        public int Buffer { get; set; }
        public bool Running { get; set; }

        private static TcpListener listener;

        private CancellationTokenSource cancellationTokenSource;

        public event EventHandler<ProxyDataEventArgs> ClientDataSentToServer;
        public event EventHandler<ProxyDataEventArgs> ServerDataSentToClient;
        public event EventHandler<ProxyByteDataEventArgs> BytesTransfered;

        /// <summary>
        /// Start the TCP Proxy
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
        /// Accept Connections
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
                        await ProxyClientConnection(client, bufferSize);
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
        /// <param name="serverStream"></param>
        /// <param name="clientStream"></param>
        /// <param name="bufferSize"></param>
        /// <param name="cancellationToken"></param>
        private void ProxyClientDataToServer(TcpClient client, NetworkStream serverStream, NetworkStream clientStream, int bufferSize, CancellationToken cancellationToken)
        {
            byte[] message = new byte[bufferSize];
            int clientBytes;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    clientBytes = clientStream.Read(message, 0, bufferSize);
                    if (BytesTransfered !=null)
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
        /// <param name="clientStream"></param>
        /// <param name="bufferSize"></param>
        /// <param name="cancellationToken"></param>
        private void ProxyServerDataToClient(NetworkStream serverStream, NetworkStream clientStream, int bufferSize, CancellationToken cancellationToken)
        {
            byte[] message = new byte[bufferSize];
            int serverBytes;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    serverBytes = serverStream.Read(message, 0, bufferSize);
                    if (BytesTransfered !=null)
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
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        private async Task ProxyClientConnection(TcpClient client, int bufferSize)
        {

            // Handle this client
            // Send the server data to client and client data to server - swap essentially.
            var clientStream = client.GetStream();
            TcpClient server = new TcpClient(Client.Address.ToString(), Client.Port);
            var serverStream = server.GetStream();

            var cancellationToken = cancellationTokenSource.Token;

            try
            {
                // Continually do the proxying
                new Task(() => ProxyClientDataToServer(client, serverStream, clientStream, bufferSize, cancellationToken), cancellationToken).Start();
                new Task(() => ProxyServerDataToClient(serverStream, clientStream, bufferSize, cancellationToken), cancellationToken).Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        /// <summary>
        /// Stop the Proxy Server
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

        public TcpProxy(short port)
        {
            Server = new IPEndPoint(IPAddress.Any, port);
            Client = new IPEndPoint(IPAddress.Any, port + 1);
            Buffer = 4096;
        }
        public TcpProxy(short port, IPAddress ipAddress)
        {
            Server = new IPEndPoint(ipAddress, port);
            Client = new IPEndPoint(ipAddress, port + 1);
            Buffer = 4096;
        }
        public TcpProxy(short port, IPAddress ipAddress, int buffer)
        {
            Server = new IPEndPoint(ipAddress, port);
            Client = new IPEndPoint(ipAddress, port + 1);
            Buffer = buffer;
        }

        public TcpProxy(ProxyDefinition definition)
        {
            Server = new IPEndPoint(definition.ServerAddress, definition.ServerPort);
            Client = new IPEndPoint(definition.ClientAddress, definition.ClientPort);
            Buffer = 4096;
        }


    }

}
