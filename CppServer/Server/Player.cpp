#include "Player.h"
#include <boost/uuid.hpp>

Player::Player()
{
	boost::uuids::uuid uuid = boost::uuids::random_generator()();
	_playerId = boost::uuids::to_string(uuid);
}

const std::string& Player::GetId() const
{
	return _playerId;
}

void Player::SetPosition(const Vector2& position)
{
	_position = position;
}

const Vector2& Player::GetPosition() const
{
	return _position;
}

const std::vector<BallData>& Player::GetBalls() const
{
	return _balls;
}

void Player::SetBalls(const std::vector<BallData>& balls)
{
	_balls = balls;
}

float Player::GetTotalMass() const
{
	return _totalMass;
}

void Player::SetTotalMass(float totalMass)
{
	_totalMass = totalMass;
}

