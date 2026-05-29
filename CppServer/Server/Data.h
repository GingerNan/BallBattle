#pragma once
#include <string>
#include <list>
#include "Common.h"
#include "json.hpp"

// 二维向量
struct Vector2
{
	float x = 0.0f;
	float y = 0.0f;

	Vector2() = default;
	Vector2(float x, float y) : x(x), y(y) {}
};

// 食物数据
struct FoodData
{
	std::string FoodId;
	Vector2 Position;
	float Mass = 0.0f;
	std::string FromPlayerId;
};

struct BallData
{
	std::string BallId;
	Vector2 Position;
	float Mass = 0.0f;
};

// 吐球数据
struct VomitData
{
	std::string PlayerId;
	Vector2 Position;
	Vector2 Direction;		// 吐球的方向
	float Mass = 0.0f;
	std::string FoodId;		// 吐球对应的食物id
};


struct PlayerPostionData
{
	std::string PlayerId;
	Vector2 Position;
	float TotalMass = 0.0f;
	std::list<BallData> Balls;
};

struct NetworkMessage
{
	MessageType Type = PlayerJoin;

	// 玩家相关
	std::string PlayerId;
	Vector2 Position;
	std::string Data;

	// 同步位置相关
	std::list<PlayerPostionData> PlayerPositions;
	PlayerPostionData PlayerPosition;

	// 食物相关
	std::list<FoodData> Foods;
	FoodData Food;
	std::string FoodId;

	VomitData VomitData;
};

// Json序列化和反序列化
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(Vector2, x, y)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(FoodData, FoodId, Position, Mass, FromPlayerId)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(BallData, BallId, Position, Mass)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(VomitData, PlayerId, Position, Direction, Mass, FoodId)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(PlayerPostionData, PlayerId, Position, TotalMass, Balls)
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(NetworkMessage, Type, PlayerId, Position, Data, PlayerPositions, PlayerPosition, Foods, Food, FoodId, VomitData)
