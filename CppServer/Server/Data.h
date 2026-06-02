#pragma once
#include <string>
#include <vector>
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
	std::vector<BallData> Balls;
};

struct NetworkMessage
{
	MessageType Type = MSG_PLAYER_JOIN;

	// 玩家相关
	std::string PlayerId;
	Vector2 Position;
	std::string Data;

	// 同步位置相关
	std::vector<PlayerPostionData> PlayerPositions;
	PlayerPostionData PlayerPosition;

	// 食物相关
	std::vector<FoodData> Foods;
	FoodData Food;
	std::string FoodId;

	VomitData VomitData;
};

// Json序列化和反序列化
namespace JsonUtil
{
template <typename T>
void GetIfPresent(const nlohmann::json& j, const char* key, T& value)
{
	auto it = j.find(key);
	if (it != j.end() && !it->is_null())
	{
		it->get_to(value);
	}
}
}

inline void to_json(nlohmann::json& j, const Vector2& value)
{
	j = nlohmann::json{
		{"x", value.x},
		{"y", value.y}
	};
}

inline void from_json(const nlohmann::json& j, Vector2& value)
{
	value = Vector2{};
	JsonUtil::GetIfPresent(j, "x", value.x);
	JsonUtil::GetIfPresent(j, "y", value.y);
}

inline void to_json(nlohmann::json& j, const FoodData& value)
{
	j = nlohmann::json{
		{"FoodId", value.FoodId},
		{"Position", value.Position},
		{"Mass", value.Mass},
		{"FromPlayerId", value.FromPlayerId}
	};
}

inline void from_json(const nlohmann::json& j, FoodData& value)
{
	value = FoodData{};
	JsonUtil::GetIfPresent(j, "FoodId", value.FoodId);
	JsonUtil::GetIfPresent(j, "Position", value.Position);
	JsonUtil::GetIfPresent(j, "Mass", value.Mass);
	JsonUtil::GetIfPresent(j, "FromPlayerId", value.FromPlayerId);
}

inline void to_json(nlohmann::json& j, const BallData& value)
{
	j = nlohmann::json{
		{"BallId", value.BallId},
		{"Position", value.Position},
		{"Mass", value.Mass}
	};
}

inline void from_json(const nlohmann::json& j, BallData& value)
{
	value = BallData{};
	JsonUtil::GetIfPresent(j, "BallId", value.BallId);
	JsonUtil::GetIfPresent(j, "Position", value.Position);
	JsonUtil::GetIfPresent(j, "Mass", value.Mass);
}

inline void to_json(nlohmann::json& j, const VomitData& value)
{
	j = nlohmann::json{
		{"PlayerId", value.PlayerId},
		{"Position", value.Position},
		{"Direction", value.Direction},
		{"Mass", value.Mass},
		{"FoodId", value.FoodId}
	};
}

inline void from_json(const nlohmann::json& j, VomitData& value)
{
	value = VomitData{};
	JsonUtil::GetIfPresent(j, "PlayerId", value.PlayerId);
	JsonUtil::GetIfPresent(j, "Position", value.Position);
	JsonUtil::GetIfPresent(j, "Direction", value.Direction);
	JsonUtil::GetIfPresent(j, "Mass", value.Mass);
	JsonUtil::GetIfPresent(j, "FoodId", value.FoodId);
}

inline void to_json(nlohmann::json& j, const PlayerPostionData& value)
{
	j = nlohmann::json{
		{"PlayerId", value.PlayerId},
		{"Position", value.Position},
		{"TotalMass", value.TotalMass},
		{"Balls", value.Balls}
	};
}

inline void from_json(const nlohmann::json& j, PlayerPostionData& value)
{
	value = PlayerPostionData{};
	JsonUtil::GetIfPresent(j, "PlayerId", value.PlayerId);
	JsonUtil::GetIfPresent(j, "Position", value.Position);
	JsonUtil::GetIfPresent(j, "TotalMass", value.TotalMass);
	JsonUtil::GetIfPresent(j, "Balls", value.Balls);
}

inline void to_json(nlohmann::json& j, const NetworkMessage& value)
{
	j = nlohmann::json{
		{"Type", static_cast<int>(value.Type)},
		{"PlayerId", value.PlayerId},
		{"Position", value.Position},
		{"Data", value.Data},
		{"PlayerPositions", value.PlayerPositions},
		{"PlayerPosition", value.PlayerPosition},
		{"Foods", value.Foods},
		{"Food", value.Food},
		{"FoodId", value.FoodId},
		{"VomitData", value.VomitData}
	};
}

inline void from_json(const nlohmann::json& j, NetworkMessage& value)
{
	value = NetworkMessage{};

	int type = static_cast<int>(value.Type);
	JsonUtil::GetIfPresent(j, "Type", type);
	value.Type = static_cast<MessageType>(type);

	JsonUtil::GetIfPresent(j, "PlayerId", value.PlayerId);
	JsonUtil::GetIfPresent(j, "Position", value.Position);
	JsonUtil::GetIfPresent(j, "Data", value.Data);
	JsonUtil::GetIfPresent(j, "PlayerPositions", value.PlayerPositions);
	JsonUtil::GetIfPresent(j, "PlayerPosition", value.PlayerPosition);
	JsonUtil::GetIfPresent(j, "Foods", value.Foods);
	JsonUtil::GetIfPresent(j, "Food", value.Food);
	JsonUtil::GetIfPresent(j, "FoodId", value.FoodId);
	JsonUtil::GetIfPresent(j, "VomitData", value.VomitData);
}
