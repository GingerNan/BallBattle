using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    public class Server
    {
        public static Socket serverSocket;
        List<Client> clients = new List<Client>();
        
        public Server()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            
            try
            {
                serverSocket.Bind(endPoint);
                serverSocket.Listen(10);
                Console.WriteLine($"服务端开启，监听端口: {endPoint.Address}:{endPoint.Port}");

                StartAccepting();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// 持续等待客户端链接
        /// </summary>
        private void StartAccepting()
        {
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        /// <summary>
        /// 客户端的链接回调
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                // 存储该客户端
                Socket clientSocket = serverSocket.EndAccept(ar);
                Client client = new Client(clientSocket);
                clients.Add(client);
                
                Console.WriteLine($"有客户端连接上了服务器: {clientSocket.RemoteEndPoint}");
                
                // 继续等待其他客户端链接
                StartAccepting();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        /// <summary>
        /// 移除指定客户端
        /// </summary>
        /// <param name="client"></param>
        public void RemoveClient(Client client)
        {
            if (clients.Contains(client))
            {
                clients.Remove(client);
            }
        }
    }
}