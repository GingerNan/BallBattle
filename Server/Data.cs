using System;

namespace Server
{
    // 消息类型
    public enum MessageType
    {
        PlayerJoin,
        GiveThePlayerId,
        GenerateMap,
        SendPosition,   // 客户端发送位置和状态
        SyncPosition,   // 服务器同步所有玩家位置
        PlayerLeave,
        GenerateFood,
        RemoveFood,
        SyncFoods,
        PlayerVomit,
        PlayerEat,
    }
    
    [Serializable]
    public class NetworkMessage
    {
        public MessageType Type { get; set; }
    }
}