#include "PlayerManager.h"
#include "Player.h"
#include <iostream>

PlayerManager::PlayerManager()
{
}

PlayerManager::~PlayerManager()
{
}

std::shared_ptr<Player> PlayerManager::CreatePlayer()
{
	auto player = std::make_shared<Player>();
	_players[player->GetId()] = player;
	return player;
}

void PlayerManager::AddPlayer(std::shared_ptr<Player> player)
{
	if (_players.find(player->GetId()) != _players.end())
	{
		std::cout << "Player " << player->GetId() << " already exists!" << std::endl;
		return;
	}
	_players[player->GetId()] = player;
}

void PlayerManager::RemovePlayer(std::string playerId)
{
	_players.erase(playerId);
}

void PlayerManager::OnDayChanged()
{
	std::cout << "PlayerManager day changed, no daily player state to reset." << std::endl;
}

std::shared_ptr<Player> PlayerManager::FindPlayerById(std::string playerId)
{
	auto it = _players.find(playerId);
	if (it != _players.end())
	{
		return it->second;
	}
	return std::shared_ptr<Player>();
}

std::vector<PlayerPostionData> PlayerManager::GetAllPlayerPositions()
{
	std::vector<PlayerPostionData> positions;
	for (auto& [playerId, player] : _players)
	{
		PlayerPostionData postion;
		postion.PlayerId = playerId;
		postion.Balls = player->GetBalls();
		postion.Position = player->GetPosition();
		postion.TotalMass = player->GetTotalMass();
		positions.push_back(postion);
	}
	return positions;
}
