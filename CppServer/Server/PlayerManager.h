#pragma once
#include <string>
#include <memory>
#include <map>
#include "Data.h"

class Player;
class PlayerManager
{
public:
	static PlayerManager& GetInstance()
	{
		static PlayerManager instance;
		return instance;
	}

	PlayerManager(const PlayerManager&) = delete;
	PlayerManager& operator=(const PlayerManager&) = delete;
	~PlayerManager();
private:
	PlayerManager();

public:
	std::shared_ptr<Player> CreatePlayer();

	void AddPlayer(std::shared_ptr<Player> player);
	
	void RemovePlayer(std::string playerId);

	std::shared_ptr<Player> FindPlayerById(std::string playerId);

	// 获取所有玩家的位置和状态
	std::vector<PlayerPostionData> GetAllPlayerPositions();
private:
	std::map<std::string, std::shared_ptr<Player>> _players;
};

