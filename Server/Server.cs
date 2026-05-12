using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace Server
{
    public class Server
    {
        public static Socket serverSocket;
        public List<Client> clients = new List<Client>();
        private FoodManager foodManager;
        
        public Server()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
            
            try
            {
                serverSocket.Bind(endPoint);
                serverSocket.Listen(10);
                Console.WriteLine($"服务端开启，监听端口: {endPoint.Address}:{endPoint.Port}");

                // 初始化食物位置
                foodManager = FoodManager.Instance;
                foodManager.InitializeFoods();
                
                // 监听客户端连接
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
                
                // 向新链接上来的客户端同步食物
                SyncAllFoodsToClient(client);
                
                // 继续等待其他客户端链接
                StartAccepting();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // 向该客户端同步食物
        private void SyncAllFoodsToClient(Client client)
        {
            List<FoodData> foods = foodManager.GetAllFoods();
            NetworkMessage message = new NetworkMessage
            {
                Type = MessageType.SyncFoods,
                Foods = foods,
            };

            string jsonMessage = JsonConvert.SerializeObject(message);
            client.Send(jsonMessage);
            Console.WriteLine($"向客户端 {client.PlayerId} 同步了 {foods.Count} 个食物");
        }

        /// <summary>
        /// 移除食物
        /// </summary>
        /// <param name="foodId"></param>
        /// <param name="client"></param>
        public void HandleFoodRemove(string foodId, Client client)
        {
            try
            {
                if (!foodManager.FoodExists(foodId))
                {
                    throw new Exception($"食物{foodId}不存在!");
                }

                // 移除旧食物 生成新的食物
                FoodData newFood = foodManager.HandleFoodRemove(foodId);
                
                // 广播告诉所有客户端移除该食物
                BroadcastFoodRemove(foodId);

                // 广播告诉所有客户端生成新食物
                if (newFood != null)
                {
                    BroadcastFoodGenerated(newFood);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #region 广播方法

        private void BroadcastFoodGenerated(FoodData food)
        {
            try
            {
                var message = new NetworkMessage
                {
                    Type = MessageType.GenerateFood,
                    Food =  food,
                };
                
                string jsonMessage = JsonConvert.SerializeObject(message);
                Broadcast(jsonMessage);
                Console.WriteLine($"广播生成食物: {food.FoodId}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        // 广播移除该食物
        private void BroadcastFoodRemove(string foodId)
        {
            try
            {
                var message = new NetworkMessage
                {
                    Type = MessageType.RemoveFood,
                    FoodId = foodId,
                };
                
                string jsonMessage = JsonConvert.SerializeObject(message);
                Broadcast(jsonMessage);
                Console.WriteLine($"广播移除食物: {foodId}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // 广播消息给所有客户端
        private void Broadcast(string msg)
        {
            lock (clients)
            {
                foreach (var client in clients)
                {
                    if (client.IsConnected)
                    {
                        try
                        {
                            client.Send(msg);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                }
            }
        }

        #endregion
        
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