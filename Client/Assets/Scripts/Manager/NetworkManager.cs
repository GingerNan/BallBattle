using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Server;
using UnityEngine;
using Object = System.Object;
using Vector2 = Server.Vector2;

public class NetworkManager : MonoSingleton<NetworkManager>
{
    public Socket clientSocket;
    
    private bool isConnected = false;
    private Thread receiveThread;
    
    private Queue<NetworkMessage> messageQueue = new Queue<NetworkMessage>();
    private Object queueLock = new object();

    private string _playerId = "Null";
    
    private List<byte> _receiveBuffer = new List<byte>();
    private int _expectedBodyLength = -1;

    #region 生命周期

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Connect();
    }

    private void Update()
    {
        ProcessMessageQueue();
    }

    protected override void OnDestroy()
    {
        Disconnect();
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        Disconnect();
    }
    
    #endregion
    
    
    /// <summary>
    /// 初始化连接
    /// </summary>
    private void Connect()
    {
        try
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(IPInfo.IP, IPInfo.Port);
            isConnected = true;
            
            Debug.Log("已连接至:" + clientSocket.RemoteEndPoint.ToString());

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void ReceiveData()
    {
        byte[] buffer = new byte[1024];
        while (isConnected && clientSocket.Connected && clientSocket!= null)
        {
            try
            {
                int bytesRead = clientSocket.Receive(buffer);
                if (bytesRead > 0)
                {
                    // 添加新数据到待处理缓冲区
                    byte[] newData = new byte[bytesRead];
                    Array.Copy(buffer, newData, bytesRead);
                    
                    _receiveBuffer.AddRange(newData);
                    
                    // 处理累计缓冲区中的数据
                    ProcessReceiveData();
                }
                else
                {
                    // 连接关闭
                    Debug.Log("服务器连接已关闭");
                    isConnected = false;
                    break;
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"接受数据异常: {e.Message}");
                isConnected = false;
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine($"接受数据异常: {e.Message}");
                isConnected = false;
                break;
            }
        }
        
        Debug.Log("接受线程结束");
        Disconnect();
    }

    /// <summary>
    /// 发送消息到服务器
    /// </summary>
    /// <param name="msg"></param>
    private void Send(string msg)
    {
        try
        {
            if (!isConnected || clientSocket == null || !clientSocket.Connected)
            {
                return;
            }
            
            byte[] bodyData = Encoding.UTF8.GetBytes(msg);
            int bodyLength = bodyData.Length;
            
            // 小端序
            byte[] headData = BitConverter.GetBytes(bodyLength);
            
            // 合并消息头和消息体
            byte[] totalData = new byte[headData.Length + bodyData.Length];
            Buffer.BlockCopy(headData, 0, totalData, 0, headData.Length);
            Buffer.BlockCopy(bodyData, 0, totalData, headData.Length, bodyData.Length);
            
            clientSocket.Send(totalData);
        }
        catch (Exception e)
        {
            Debug.Log($"发送消息错误: {e.Message}");
            Disconnect();
        }
    }

    // 断开连接
    private void Disconnect()
    {
        try
        {
            isConnected = false;
            if (clientSocket != null)
            {
                if (clientSocket.Connected)
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                }
                
                clientSocket.Close();
                clientSocket.Dispose();
                clientSocket = null;
            }

            receiveThread.Join(1000);   // 等待接受线程结束
            
            Debug.Log("与服务器断开连接");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    /// <summary>
    /// 解析消息并传递
    /// </summary>
    private void ProcessReceiveData()
    {
        try
        {
            while (_receiveBuffer.Count >= 4)
            {
                // 如果还没有解析出消息体长度，先解析消息头
                if (_expectedBodyLength == -1)
                {
                    byte[] headBytes = _receiveBuffer.GetRange(0, 4).ToArray();
                    _expectedBodyLength = BitConverter.ToInt32(headBytes, 0);
                    
                    // 添加长度验证
                    if (_expectedBodyLength < 0 || _expectedBodyLength > 10 * 1024 * 1024)  // 限制10MB
                    {
                        Debug.Log($"无效的消息长度: {_expectedBodyLength}");
                        Disconnect();
                        return;
                    }
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
            Debug.Log($"处理接受数据错误: {e.Message}");
            Disconnect();
        }
    }

    /// <summary>
    /// 添加到消息队列
    /// </summary>
    /// <param name="msg"></param>
    private void ProcessMessage(string msg)
    {
        try
        {
            NetworkMessage networkMessage = JsonConvert.DeserializeObject<NetworkMessage>(msg);
            lock (queueLock)
            {
                messageQueue.Enqueue(networkMessage);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// 处理消息队列
    /// </summary>
    private void ProcessMessageQueue()
    {
        lock (queueLock)
        {
            while (messageQueue.Count > 0)
            {
                NetworkMessage message = messageQueue.Dequeue();
                HandleMessage(message);
            }
        }
    }

    /// <summary>
    /// 实际处理消息
    /// </summary>
    /// <param name="message"></param>
    private void HandleMessage(NetworkMessage message)
    {
        try
        {
            switch (message.Type)
            {
                case MessageType.PlayerJoin:
                    GameManager.Instance.CreateNewPlayer(message.PlayerId);
                    break;
                case MessageType.GiveThePlayerId:
                    _playerId = message.PlayerId;
                    GameManager.Instance.CreateNewPlayer(_playerId, true);
                    Debug.Log($"自己ID是 {_playerId}");
                    break;
                case MessageType.SyncPositions:
                    if (message.PlayerPositions != null)
                    {
                        // 同步所有玩家位置
                        
                    }
                    else if (message.PlayerPosition != null)
                    {
                        // 同步单玩家的位置
                        Debug.Log($"同步单个玩家位置: {message.PlayerPosition.PlayerId}");
                        EventCenter.Instance.EventTrigger<PlayerPositionData>(GameEvent.玩家位置更新, message.PlayerPosition);
                    }
                    break;
                case MessageType.PlayerLeave:
                    break;
                case MessageType.SyncFoods:
                    Debug.Log($"收到食物同步消息，食物数量:{message.Foods?.Count ?? 0}");
                    EventCenter.Instance.EventTrigger<List<FoodData>>(GameEvent.同步食物, message.Foods);
                    break;
                case MessageType.GenerateFood:
                    Debug.Log($"收到食物生成消息，食物Id:{message.Food.FoodId}");
                    EventCenter.Instance.EventTrigger<FoodData>(GameEvent.食物生成, message.Food);
                    break;
                case MessageType.RemoveFood:
                    Debug.Log($"收到食物移除消息，食物Id:{message.Food.FoodId}");
                    EventCenter.Instance.EventTrigger<string>(GameEvent.食物移除, message.FoodId);
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    #region 外部调用通知

    public void SendFoodEatenMessage(string foodId)
    {
        try
        {
            NetworkMessage networkMessage = new NetworkMessage
            {
                Type = MessageType.RemoveFood,
                FoodId = foodId,
                //PlayerId = 
            };
            
            string jsonMsg = JsonConvert.SerializeObject(networkMessage);
            Send(jsonMsg);
            Debug.Log($"发送食物被吃掉消息:{foodId}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    // 发送自己的位置信息
    public void SendPlayerPosition(UnityEngine.Vector2 position, List<BallController> balls)
    {
        try
        {
            List<BallData> ballDataList = new List<BallData>();
            float totalMass = 0;

            foreach (var ball in balls)
            {
                if (ball == null)
                {
                    continue;
                }

                string ballId = ball.GetBallId();
                ballDataList.Add(new BallData()
                {
                    BallId = ballId,
                    Position = new Vector2(ball.transform.position.x, ball.transform.position.y),
                    Mass = ball.Mass
                });
                totalMass += ball.Mass;
            }

            var message = new NetworkMessage
            {
                Type = MessageType.SendPosition,
                PlayerId = _playerId,
                Position = new Vector2(position.x, position.y),
                PlayerPosition = new PlayerPositionData()
                {
                    PlayerId = _playerId,
                    Balls = ballDataList,
                    TotalMass = totalMass,
                    Position = new  Vector2(position.x, position.y),
                }
            };
            
            Send(JsonConvert.SerializeObject(message));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    #endregion

    public string GetPlayerId()
    {
        return _playerId;
    }
}
