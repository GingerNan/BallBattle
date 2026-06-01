#pragma once
#include <string>
#include <vector>
#include "Data.h"

class Player
{
public:
	explicit Player();

	const std::string& GetId() const;
	const Vector2& GetPosition() const;
	const std::vector<BallData>& GetBalls() const;
	float GetTotalMass() const;
private:
	std::string _playerId;
	Vector2 _position;
	std::vector<BallData> _balls;
	float _totalMass;
};

