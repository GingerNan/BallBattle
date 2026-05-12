using System;
using System.Collections.Generic;
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
                byte[] headerData = BitConverter.GetBytes(bodyData.Length);
                // 合并消息头和消息体
                byte[] totalData = new  byte[headerData.Length + bodyData.Length];
                Buffer.BlockCopy(headerData, 0, totalData, 0, headerData.Length);
                Buffer.BlockCopy(bodyData, 0, totalData, headerData.Length, bodyData.Length);
                
                Socket.BeginSend(totalData, 0, totalData.Length, SocketFlags.None, SendCallback, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

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

        /// <summary>
        /// 解析消息
        /// </summary>
        private void ProcessReceiveData()
        {
            try
            {
                while (_receiveBuffer.Count >= 4)
                {
                    if (_expectedBodyLength == -1)
                    {
                        byte[] headBytes = _receiveBuffer.GetRange(0, 4).ToArray();
                        _expectedBodyLength = BitConverter.ToInt32(headBytes, 0);
                    }

                    // 判断是否是一条完整的消息
                    if (_receiveBuffer.Count >=  4 + _expectedBodyLength)
                    {
                        // 提取消息体
                        byte[] bodyBytes = _receiveBuffer.GetRange(4, _expectedBodyLength).ToArray();
                        string msg = Encoding.UTF8.GetString(bodyBytes);
                        
                        // 实际处理消息
                        ProcessMessage(msg);
                        
                        // 从缓冲区移除已处理数据
                        _receiveBuffer.RemoveRange(0, 4 + _expectedBodyLength);
                        _expectedBodyLength = -1;
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
        private void ProcessMessage(string msg)
        {
            NetworkMessage networkMessage = JsonConvert.DeserializeObject<NetworkMessage>(msg);

            switch (networkMessage.Type)
            {
                case MessageType.PlayerJoin:
                    break;
                case MessageType.RemoveFood:
                    HandleRemoveFood(networkMessage.FoodId);
                    break;
                case MessageType.PlayerLeave:
                    break;
            }
        }

        #region 处理对应消息的方法

        // 处理移除食物
        private void HandleRemoveFood(string foodId)
        {
            Console.WriteLine($"移除食物: {foodId}");
            Program.server.HandleFoodRemove(foodId, this);
        }

        #endregion
    }
}