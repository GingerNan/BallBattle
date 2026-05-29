#include "CServer.h"
#include "CClient.h"
#include "FoodManager.h"

#include <vector>
#include <iostream>
#include <boost/uuid.hpp>

CServer::CServer(boost::asio::io_context& ioc, unsigned short port)
	: _ioc(ioc), _port(port), 
	_acceptor(ioc, boost::asio::ip::tcp::endpoint(boost::asio::ip::tcp::v4(), port))
{
	std::cout << "Server Start! listen port: " << port << std::endl;
	StartAccpet();
}

void CServer::ClearSession(std::string uuid)
{
	_clients.erase(uuid);
}

void CServer::HandlePlayerPosition(std::shared_ptr<CClient> client)
{
	BroadcastPlayerPosition(client);
}

void CServer::HandleRemoveFood(std::string foodId, std::shared_ptr<CClient> client)
{
	if (!FoodManager::GetInstance().IsFoodExist(foodId))
	{
		std::cout << "食物 " << foodId << " 不存在，可能被吃掉了" << std::endl;
		return;
	}

	auto newFood = FoodManager::GetInstance().RemoveFood(foodId);

	BroadcastFoodRemove(foodId);

	if (newFood)
	{
		BroadcastFoodGenerated(newFood);
	}
}

void CServer::HandlePlayerVomit(VomitData vomitData)
{
	BroadcastPlayerVomit(vomitData);
}

void CServer::SendClientTheId(std::shared_ptr<CClient> client)
{
	NetworkMessage msg;
	msg.Type = GiveThePlayerId;
	msg.PlayerId = client->GetUuid();

	nlohmann::json json_msg = msg;
	client->Send(json_msg.dump());

	std::cout << "发送玩家ID给客户端， PlayerId: " << client->GetUuid() << std::endl;
}

void CServer::SyncAllFoodsToClient(std::shared_ptr<CClient> client)
{
	auto foods = FoodManager::GetInstance().GetAllFoods();
	NetworkMessage msg;
	msg.Type = SyncFoods;
	msg.Foods = foods;

	nlohmann::json json_msg = msg;
	client->Send(json_msg.dump());
	std::cout << "向客户端" << client->GetUuid() << " 同步食物，数量: " << foods.size() << std::endl;
}

void CServer::SyncAllPositionsToClient(std::shared_ptr<CClient> client)
{
	auto playerPositions = GetAllPlayerPositions();
	NetworkMessage msg;
	msg.Type = SyncPositions;
	msg.PlayerPositions = playerPositions;

	nlohmann::json json_msg = msg;
	client->Send(json_msg.dump());
}

void CServer::BroadcastPlayerPosition(std::shared_ptr<CClient> client)
{
	PlayerPostionData postion;
	postion.PlayerId = client->GetUuid();
	postion.Balls = client->GetBalls();
	postion.Position = client->GetPostion();
	postion.TotalMass = client->GetTotalMass();

	NetworkMessage message;
	message.Type = SyncPositions;
	message.PlayerPosition = postion;

	nlohmann::json json_msg = message;
	BroadcastToOthers(json_msg.dump(), client);
}

void CServer::BroadcastFoodRemove(std::string foodId)
{
	NetworkMessage msg;
	msg.Type = RemoveFood;
	msg.FoodId = foodId;

	nlohmann::json json_msg = msg;
	Broadcast(json_msg.dump());
}

void CServer::BroadcastFoodGenerated(std::shared_ptr<FoodData> food)
{
	NetworkMessage msg;
	msg.Type = GenerateFood;
	msg.Food = *food;

	nlohmann::json json_msg = msg;
	Broadcast(json_msg);
	std::cout << "广播生成食物：" << food->FoodId << std::endl;
}

void CServer::BroadcastPlayerVomit(VomitData vomitData)
{
	boost::uuids::uuid uuid = boost::uuids::random_generator()();
	vomitData.FoodId = boost::uuids::to_string(uuid);

	NetworkMessage msg;
	msg.Type = PlayerVomit;
	msg.VomitData = vomitData;

	nlohmann::json json_msg = msg;
	Broadcast(json_msg.dump());
}

void CServer::BroadcastToOthers(std::string msg, std::shared_ptr<CClient> exclude_client)
{
	for (auto& [uuid, client] : _clients)
	{
		if (client == exclude_client || client->IsClose())
			continue;

		client->Send(msg);
	}
}

void CServer::Broadcast(std::string msg)
{
	for (auto& [uuid, client] : _clients)
	{
		if (client->IsClose())
			continue;

		client->Send(msg);
	}
}

std::vector<PlayerPostionData> CServer::GetAllPlayerPositions()
{
	std::vector<PlayerPostionData> positions;
	for (auto& [uuid, client] : _clients)
	{
		PlayerPostionData postion;
		postion.PlayerId = client->GetUuid();
		postion.Balls = client->GetBalls();
		postion.Position = client->GetPostion();
		postion.TotalMass = client->GetTotalMass();
		positions.push_back(postion);
	}

	return positions;
}

void CServer::StartAccpet()
{
	std::shared_ptr<CClient> new_session = std::make_shared<CClient>(_ioc, this);

	SendClientTheId(new_session);

	std::cout << "有客户端连接上了服务器：" << new_session->GetSocket().remote_endpoint().address().to_string() << std::endl;

	SyncAllFoodsToClient(new_session);

	SyncAllPositionsToClient(new_session);

	_acceptor.async_accept(
		new_session->GetSocket(),
		std::bind(&CServer::HandleAccept, this, new_session, std::placeholders::_1)
	);
}

void CServer::HandleAccept(std::shared_ptr<CClient> newSeesion, const boost::system::error_code& err)
{
	if (!err)
	{
		newSeesion->Start();
		_clients.insert({ newSeesion->GetUuid(), newSeesion });
	}
	else
	{
		std::cout << "Accpet failed! Err msg" << err.message() << std::endl;
	}

	StartAccpet();
}
