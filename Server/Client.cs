using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace Server
{
    public class Client
    {
        private Socket Socket {get; set;}
        public string PlayerId { get; private set; }
        public bool IsConnected => Socket != null && Socket.Connected;
        
        private List<byte> _receiveBuffer = new List<byte>();   // 累计接收消息的缓冲区
        private int _expectedBodyLength = -1;   // 期望消息长度
        private int _expectedMessageId = -1;
        private const int MessageHeaderLength = 4;
        private const int MaxMessageBodyLength = short.MaxValue;
        
        // 玩家状态信息
        public Vector2 Position { get; set; } = new Vector2(0, 0);
        public List<BallData> Balls { get; set; } = new List<BallData>();
        public float TotalMass { get; set; } = 0;
        
        public Client(Socket socket)
        {
            Socket = socket;
            // 唯一标识
            PlayerId = Guid.NewGuid().ToString();
            StartReceiving();
        }
        
        // 持续接收消息
        private void StartReceiving()
        {
            try
            {
                byte[] buffer = new byte[1024];
                Socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, buffer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int bytesRead = Socket.EndReceive(ar);
                if (bytesRead <= 0)
                {
                    // 客户端断开连接
                    Disconnect();
                    return;
                }
                
                // 这一次收到的数据
                byte[] buffer = ar.AsyncState as byte[];
                if (buffer == null)
                {
                    Console.WriteLine("接受缓冲区为空");
                    return;
                }
                
                byte[] newData = new byte[bytesRead];
                Array.Copy(buffer, newData, bytesRead);
                _receiveBuffer.AddRange(newData);

                // 处理累计缓冲区数据
                ProcessReceiveData();
                    
                // 接续接受
                StartReceiving();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // throw;   //不建议这里 throw，否则异步回调里的异常会直接导致服务端崩溃
            }
        }

        /// <summary>
        /// 向客户端发送消息
        /// </summary>
        /// <param name="msg"></param>
        public void Send(string msg)
        {
            try
            {
                byte[] bodyData = Encoding.UTF8.GetBytes(msg);
                int bodyLength = bodyData.Length;
                if (bodyLength > MaxMessageBodyLength)
                {
                    Console.WriteLine($"消息体过长: {bodyLength}");
                    return;
                }

                NetworkMessage message = JsonConvert.DeserializeObject<NetworkMessage>(msg);
                short messageId = IPAddress.HostToNetworkOrder((short)message.Type);
                short messageLength = IPAddress.HostToNetworkOrder((short)bodyLength);

                // 合并消息头和消息体
                byte[] totalData = new  byte[MessageHeaderLength + bodyData.Length];
                Buffer.BlockCopy(BitConverter.GetBytes(messageId), 0, totalData, 0, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(messageLength), 0, totalData, 2, 2);
                Buffer.BlockCopy(bodyData, 0, totalData, MessageHeaderLength, bodyData.Length);
                
                Socket.BeginSend(totalData, 0, totalData.Length, SocketFlags.None, SendCallback, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // 发送消息回调
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // 断开连接
        private void Disconnect()
        {
            try
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();
                Program.server.RemoveClient(this);
                
                Console.WriteLine($"玩家 {PlayerId} 断开连接");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        /// <summary>
        /// 解析消息
        /// </summary>
        private void ProcessReceiveData()
        {
            try
            {
                while (_receiveBuffer.Count >= MessageHeaderLength)
                {
                    if (_expectedBodyLength == -1)
                    {
                        byte[] headBytes = _receiveBuffer.GetRange(0, MessageHeaderLength).ToArray();
                        _expectedMessageId = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(headBytes, 0));
                        _expectedBodyLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(headBytes, 2));

                        if (_expectedBodyLength < 0 || _expectedBodyLength > MaxMessageBodyLength)
                        {
                            Console.WriteLine($"无效的消息长度: {_expectedBodyLength}");
                            Disconnect();
                            return;
                        }
                    }

                    // 判断是否是一条完整的消息
                    if (_receiveBuffer.Count >=  MessageHeaderLength + _expectedBodyLength)
                    {
                        // 提取消息体
                        byte[] bodyBytes = _receiveBuffer.GetRange(MessageHeaderLength, _expectedBodyLength).ToArray();
                        string msg = Encoding.UTF8.GetString(bodyBytes);
                        
                        // 实际处理消息
                        ProcessMessage(_expectedMessageId, msg);
                        
                        // 从缓冲区移除已处理数据
                        _receiveBuffer.RemoveRange(0, MessageHeaderLength + _expectedBodyLength);
                        _expectedBodyLength = -1;
                        _expectedMessageId = -1;
                    }
                    else
                    {
                        // 数据不完整，等待下次接受
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// 实际处理消息方法
        /// </summary>
        /// <param name="msg"></param>
        private void ProcessMessage(int messageId, string msg)
        {
            NetworkMessage networkMessage = JsonConvert.DeserializeObject<NetworkMessage>(msg);
            networkMessage.Type = (MessageType)messageId;

            switch (networkMessage.Type)
            {
                case MessageType.PlayerJoin:
                    HandlePlayerJoin();
                    break;
                case MessageType.SendPosition:
                    HandleSendPosition(networkMessage);
                    break;
                case MessageType.RemoveFood:
                    HandleRemoveFood(networkMessage.FoodId);
                    break;
                case MessageType.PlayerVomit:
                    HandlePlayerVomit(networkMessage.VomitData);
                    break;
                case MessageType.PlayerLeave:
                    break;
            }
        }

        #region 处理对应消息的方法

        // 玩家初次连接
        private void HandlePlayerJoin()
        {
            var response = new NetworkMessage
            {
                Type = MessageType.PlayerJoin,
                PlayerId = PlayerId,
            };
            
            Send(JsonConvert.SerializeObject(response));
            Console.WriteLine($"玩家 {PlayerId} 加入游戏");
        }

        // 处理玩家位置和状态
        private void HandleSendPosition(NetworkMessage message)
        {
            this.Position = message.Position;
            if (message.Position != null)
            {
                this.Balls = message.PlayerPosition.Balls ?? new List<BallData>();
                this.TotalMass = message.PlayerPosition.TotalMass;
            }
            
            Program.server.HandlePlayerPosition(this);
        }
        
        // 处理移除食物
        private void HandleRemoveFood(string foodId)
        {
            Console.WriteLine($"移除食物: {foodId}");
            Program.server.HandleFoodRemove(foodId, this);
        }

        // 处理玩家吐球
        private void HandlePlayerVomit(VomitData vomitData)
        {
            Console.WriteLine($"玩家 {PlayerId} 吐球, 质量: {vomitData.Mass}");
            Program.server.BroadcastPlayerVomit(vomitData);
        }
        
        #endregion
    }
}
