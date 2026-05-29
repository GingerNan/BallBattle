#pragma once

#define MAX_LEN 1024*2
#define HEAD_LEN 2

#define MAX_RECV_QUE 10000
#define MAX_SEND_QUE 1000

enum MessageType
{
	PlayerJoin,
	GiveThePlayerId,
	SendPosition,			// 客户端发送位置和状态
	SyncPositions,			// 服务器同步所有玩家的位置
	PlayerLeave,
	GenerateFood,
	RemoveFood,
	SyncFoods,				// 同步来自客户端生成的食物
	PlayerVomit,
	PlayerEat,
};