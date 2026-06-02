#pragma once
#include <string>
#include <vector>
#include "Data.h"

class Player
{
public:
	explicit Player();

	const std::string& GetId() const;

	// 位置信息
	void SetPosition(const Vector2& position);
	const Vector2& GetPosition() const;

	// 玩家的球
	const std::vector<BallData>& GetBalls() const;
	void SetBalls(const std::vector<BallData>& balls);

	// 球的总质量
	float GetTotalMass() const;
	void SetTotalMass(float totalMass);

private:
	std::string _playerId;			// 玩家ID
	Vector2 _position;				// 玩家的位置
	std::vector<BallData> _balls;	// 玩家的球列表
	float _totalMass;				// 玩家球的总质量
};

