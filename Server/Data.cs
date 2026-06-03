using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Server
{
    // 自定义Vector2
    [Serializable]
    public class Vector2
    {
        [JsonProperty("x")]
        public float X { get; private set; }
        [JsonProperty("y")]
        public float Y { get; private set; }
        
        [JsonConstructor]
        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
    
    // 消息类型
    public enum MessageType
    {
        PlayerJoin = 1001,
        GiveThePlayerId,
        SendPosition,       // 客户端发送位置和状态
        SyncPositions,      // 服务器同步所有玩家位置
        PlayerLeave,
        GenerateFood,
        RemoveFood,
        SyncFoods,          // 同步来自客户端生成的食物
        PlayerVomit,
        PlayerEat,
    }

    // 食物数据
    [Serializable]
    public class FoodData
    {
        public string FoodId { get; set; }
        public Vector2 Position { get; set; }
        public float Mass { get; set; }
        public string FromPlayerId { get; set; }    // 来源为玩家时使用
    }

    [Serializable]
    public class BallData
    {
        public string BallId { get; set; }
        public Vector2 Position { get; set; }
        public float Mass { get; set; }
    }

    [Serializable]
    public class VomitData
    {
        public string PlayerId { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Direction { get; set; }
        public float Mass { get; set; }
        public string FoodId { get; set; }
    }
    
    [Serializable]
    public class PlayerPositionData
    {
        public string PlayerId { get; set; }
        public Vector2 Position { get; set; }
        public List<BallData> Balls {get; set;} = new  List<BallData>();
        public float TotalMass {get; set;}
    }
    
    [Serializable]
    public class NetworkMessage
    {
        public MessageType Type { get; set; }
        
        // 玩家相关
        public string PlayerId { get; set; }
        public Vector2 Position { get; set; }
        public string Data { get; set; }
        
        // 同步位置相关
        public List<PlayerPositionData> PlayerPositions { get; set; }
        public PlayerPositionData PlayerPosition { get; set; }
        
        // 食物信息
        public List<FoodData> Foods { get; set; }
        public FoodData Food { get; set; }
        public string FoodId { get; set; }

        public VomitData VomitData;
    }
}
