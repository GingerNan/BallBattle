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
                lock (clients)
                {
                    clients.Add(client);
                }

                // 发送新连接客户端的标识
                SendClientTheId(client);
                
                Console.WriteLine($"有客户端连接上了服务器: {clientSocket.RemoteEndPoint}");
                
                // 向新链接上来的客户端同步食物
                SyncAllFoodsToClient(client);
                
                // 向新客户端同步现有所有玩家位置
                SyncAllPositionsToClient(client);
                
                // 继续等待其他客户端链接
                StartAccepting();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // 发送新连接客户端的标识
        private void SendClientTheId(Client client)
        {
            NetworkMessage networkMessage = new NetworkMessage
            {
                Type = MessageType.GiveThePlayerId,
                PlayerId = client.PlayerId,
            };
            
            string jsonMsg = JsonConvert.SerializeObject(networkMessage);
            client.Send(jsonMsg);
            
            Console.WriteLine($"向客户端 {client.PlayerId} 告知ID");
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
                    Console.WriteLine($"食物 {foodId} 不存在, 可能已被吃掉");
                    return;
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

        public void HandlePlayerPosition(Client client)
        {
            try
            {
                BroadcastPlayerPosition(client);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // 向某一个客户端同步所有玩家位置
        public void SyncAllPositionsToClient(Client client)
        {
            var playerPositions = GetAllPlayerPositions();
            var message = new NetworkMessage()
            {
                Type = MessageType.SyncPositions,
                PlayerPositions = playerPositions,
            };
            
            client.Send(JsonConvert.SerializeObject(message));
        }
        
        #region 广播方法

        // 广播某个玩家吐球数据
        public void BroadcastPlayerVomit(VomitData vomitData)
        {
            try
            {
                // 生成食物id
                vomitData.FoodId = Guid.NewGuid().ToString();

                var message = new NetworkMessage()
                {
                    Type = MessageType.PlayerVomit,
                    VomitData = vomitData,
                };
                
                Broadcast(JsonConvert.SerializeObject(message));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        // 广播单个玩家的位置给其他玩家
        private void BroadcastPlayerPosition(Client client)
        {
            try
            {
                var position = new PlayerPositionData()
                {
                    PlayerId = client.PlayerId,
                    Balls = client.Balls,
                    TotalMass = client.TotalMass,
                    Position = client.Position,
                };

                var message = new NetworkMessage()
                {
                    Type = MessageType.SyncPositions,
                    PlayerPosition = position,
                };
                
                BroadcastToOthers(JsonConvert.SerializeObject(message), client);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

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

        private void BroadcastToOthers(string msg, Client sourceClient)
        {
            lock (clients)
            {
                foreach (var client in clients)
                {
                    if (!client.IsConnected || sourceClient == client) continue;
                    
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
            lock (clients)
            {
                if (clients.Contains(client))
                {
                    clients.Remove(client);
                }
            }
        }

        // 获取所有玩家的位置信息
        private List<PlayerPositionData> GetAllPlayerPositions()
        {
            var positions = new List<PlayerPositionData>();

            lock (clients)
            {
                foreach (var client in clients)
                {
                    positions.Add(new PlayerPositionData()
                    {
                        PlayerId = client.PlayerId,
                        Position = client.Position,
                        Balls = client.Balls,
                        TotalMass = client.TotalMass,
                    });
                }
            }

            return positions;
        }
    }
}