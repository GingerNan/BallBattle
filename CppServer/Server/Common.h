#pragma once

#define MAX_LEN 1024*2

//头部总长度
#define HEAD_TOTAL_LEN 4
//头部id长度
#define HEAD_ID_LEN 2
//头部数据长度
#define HEAD_DATA_LEN 2

#define MAX_RECV_QUE 10000
#define MAX_SEND_QUE 1000

enum MessageType
{
	MSG_PLAYER_JOIN = 1001,				// 玩家加入游戏
	MSG_GIVE_PLAYER_ID = 1002,			// 发送玩家给客户端
	MSG_SEND_POSITION = 1003,			// 客户端发送位置和状态
	MSG_SYNC_POSITIONS = 1004,			// 服务器同步所有玩家的位置
	MSG_PLAYER_LEAVE = 1005,			// 玩家离开游戏
	MSG_GENERATE_FOOD = 1006,			// 生成食物
	MSG_REMOVE_FOOD = 1007,				// 移除食物
	MSG_SYNC_FOODS = 1008,				// 同步来自客户端生成的食物
	MSG_PLAYER_VOMIT = 1009,			// 玩家吐球
	MSG_PLAYER_EAT = 1010,				// 玩家吃球
};