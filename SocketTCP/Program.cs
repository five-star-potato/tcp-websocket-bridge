using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Fleck;

namespace SocketTCP
{
    class Program
    {
        static IWebSocketConnection websockConn = null;
        static NetworkStream tcpStream = null;

        static void Main()
        {
            new Thread(StartWebsocket).Start();
            new Thread(StartTCP).Start();

            Console.ReadLine();
        }

        static void StartTCP()
        {
            IPAddress IP = IPAddress.Loopback;
            using (TcpClient client = new TcpClient())
            {
                client.Connect(IP, 4242);

                if (client.Connected)
                {
                    using (NetworkStream networkStream = client.GetStream())
                    using (StreamReader reader = new StreamReader(networkStream, Encoding.UTF8))
                    using (StreamWriter writer = new StreamWriter(networkStream))
                    {
                        var message = "<GET ID=\"SERIAL_ID\" />\r\n";
                        Console.WriteLine(message);
                        byte[] bytes = Encoding.UTF8.GetBytes(message);
                        networkStream.Write(bytes, 0, bytes.Length);
                        tcpStream = networkStream;

                        while (client.Connected)
                        {
                            char[] buffer = new char[client.ReceiveBufferSize];
                            int read = reader.Read(buffer, 0, buffer.Length);
                            if (read > 0)
                            {
                                Console.WriteLine(buffer);
                                if (websockConn != null)
                                {
                                    websockConn.Send(new string(buffer));
                                }
                            }
                        }
                    }
                }
            }
        }


        static void StartWebsocket()
        {
            var server = new WebSocketServer("ws://0.0.0.0:8181");
            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine("Open!");
                    websockConn = socket;
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine("Close!");
                    websockConn = null;
                };
                socket.OnMessage = message =>
                {
                    message = message + "\r\n";
                    if (tcpStream != null)
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(message);
                        tcpStream.Write(bytes, 0, bytes.Length);
                    }
                };
            });
        }
    }
}
