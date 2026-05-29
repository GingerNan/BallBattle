#pragma once
#include <string>
#include <list>
#include "Common.h"
#include "json.hpp"

// 二维向量
struct Vector2
{
	float x;
	float y;

	Vector2(float x, float y) : x(x), y(y) {}
};

// 食物数据
struct FoodData
{
	std::string FoodId;
	Vector2 Position;
	float Mass;
	std::string FromPlayerId;
};

struct BallData
{
	std::string BallId;
	Vector2 Position;
	float Mass;
};

// 吐球数据
struct VomitData
{
	std::string PlayerId;
	Vector2 Position;
	Vector2 Direction;		// 吐球的方向
	float Mass;
	std::string FoodId;		// 吐球对应的食物id
};


struct PlayerPostionData
{
	std::string PlayerId;
	Vector2 Position;
	float TotalMass;
	std::list<BallData> Balls;
};

NLOHMANN_DEFINE_TYPE_INTRUSIVE(NetworkMessage, Type, PlayerId, Position, Data, PlayerPositions, PlayerPosition, Foods, food, FoodId, VomitData)

struct NetworkMessage
{
	MessageType Type;

	// 玩家相关
	std::string PlayerId;
	Vector2 Position;
	std::string Data;

	// 同步位置相关
	std::list<PlayerPostionData> PlayerPositions;
	PlayerPostionData PlayerPosition;

	// 食物相关
	std::list<FoodData> Foods;
	FoodData food;
	std::string FoodId;

	VomitData VomitData;
};