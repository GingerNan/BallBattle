#pragma once
#include <map>
#include <memory>
#include <string>
#include "CClient.h"

class CServer
{
public:
	CServer(boost::asio::io_context& ioc, unsigned short port);

	void ClearSession(std::string uuid);
public:
	void HandlePlayerPosition(std::shared_ptr<CClient> client);

	void HandleRemoveFood(std::string foodId, std::shared_ptr<CClient> client);

	void HandlePlayerVomit(VomitData vomitData);
private:
	// 发送新连接客户端的标识
	void SendClientTheId(std::shared_ptr<CClient> client);

	// 向该客户端同步食物
	void SyncAllFoodsToClient(std::shared_ptr<CClient> client);

	void SyncAllPositionsToClient(std::shared_ptr<CClient> client);

	// 广播单个玩家的位置给其他玩家
	void BroadcastPlayerPosition(std::shared_ptr<CClient> client);

	// 广播移除该食物
	void BroadcastFoodRemove(std::string foodId);

	void BroadcastFoodGenerated(std::shared_ptr<FoodData> food);

	void BroadcastPlayerVomit(VomitData vomitData);

	void BroadcastToOthers(std::string msg, std::shared_ptr<CClient> exclude_client);

	void Broadcast(std::string msg);

	std::vector<PlayerPostionData> GetAllPlayerPositions();
private:
	void StartAccpet();

	void HandleAccept(std::shared_ptr<CClient> newSeesion, const boost::system::error_code& err);

private:
	boost::asio::io_context& _ioc;
	boost::asio::ip::tcp::acceptor _acceptor;
	unsigned short _port;
	std::map<std::string, std::shared_ptr<CClient>> _clients;
};

