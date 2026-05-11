using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        public static Socket serverSocket;
        
        static void Main(string[] args)
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            
            try
            {
                serverSocket.Bind(endPoint);
                serverSocket.Listen(10);
                Console.WriteLine($"服务端开启，监听端口: {endPoint.Address}:{endPoint.Port}");

                serverSocket.BeginAccept(AcceptCallback, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            Console.ReadLine();
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = serverSocket.EndAccept(ar);
                Console.WriteLine($"有客户端连接上了服务器: {clientSocket.RemoteEndPoint}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
