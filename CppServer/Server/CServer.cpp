#include "CServer.h"
#include "CSession.h"
#include "FoodManager.h"
#include "AsioIOServicePool.h"
#include "PlayerManager.h"
#include "Player.h"

#include <vector>
#include <iostream>
#include <boost/uuid.hpp>

CServer::CServer(boost::asio::io_context& ioc, unsigned short port)
	: _ioc(ioc), _port(port), 
	_acceptor(ioc, boost::asio::ip::tcp::endpoint(boost::asio::ip::tcp::v4(), port))
{
	std::cout << "Server Start! listen port: " << port << std::endl;
	FoodManager::GetInstance().InitializeFoods();
	StartAccpet();
}

void CServer::ClearSession(std::string uuid)
{
	_sessions.erase(uuid);
}

void CServer::HandlePlayerPosition(std::shared_ptr<CSession> session)
{
	BroadcastPlayerPosition(session);
}

void CServer::HandleRemoveFood(std::string foodId, std::shared_ptr<CSession> session)
{
	if (!FoodManager::GetInstance().IsFoodExist(foodId))
	{
		std::cout << "Food " << foodId << " not fund!" << std::endl;
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

void CServer::SendPlayerId(std::shared_ptr<CSession> session)
{
	auto player = PlayerManager::GetInstance().CreatePlayer();
	session->BindPlayer(player->GetId());

	NetworkMessage msg;
	msg.Type = MSG_GIVE_PLAYER_ID;
	msg.PlayerId = player->GetId();

	nlohmann::json json_msg = msg;
	session->Send(MSG_GIVE_PLAYER_ID, json_msg.dump());

	std::cout << "SendPlayerId, PlayerId: " << player->GetId() << std::endl;
}

void CServer::SyncAllFoodsTosession(std::shared_ptr<CSession> session)
{
	auto foods = FoodManager::GetInstance().GetAllFoods();
	NetworkMessage msg;
	msg.Type = MSG_SYNC_FOODS;
	msg.Foods = foods;

	nlohmann::json json_msg = msg;
	session->Send(MSG_SYNC_FOODS, json_msg.dump());
	std::cout << "SyncAllFoodsTosession, SessionUid: " << session->GetSessionUid() << ",Food Num: " << foods.size() << std::endl;
}

void CServer::SyncAllPositionsTosession(std::shared_ptr<CSession> session)
{
	auto playerPositions = PlayerManager::GetInstance().GetAllPlayerPositions();
	NetworkMessage msg;
	msg.Type = MSG_SYNC_POSITIONS;
	msg.PlayerPositions = playerPositions;

	nlohmann::json json_msg = msg;
	session->Send(MSG_SYNC_POSITIONS, json_msg.dump());
}

void CServer::BroadcastPlayerPosition(std::shared_ptr<CSession> session)
{
	auto player = PlayerManager::GetInstance().FindPlayerById(session->GetPlayerId());

	PlayerPostionData postion;
	postion.PlayerId = player->GetId();
	postion.Balls = player->GetBalls();
	postion.Position = player->GetPosition();
	postion.TotalMass = player->GetTotalMass();

	NetworkMessage message;
	message.Type = MSG_SYNC_POSITIONS;
	message.PlayerPosition = postion;

	nlohmann::json json_msg = message;
	BroadcastToOthers(MSG_SYNC_POSITIONS, json_msg.dump(), session);
}

void CServer::BroadcastFoodRemove(std::string foodId)
{
	NetworkMessage msg;
	msg.Type = MSG_REMOVE_FOOD;
	msg.FoodId = foodId;

	nlohmann::json json_msg = msg;
	Broadcast(MSG_REMOVE_FOOD, json_msg.dump());
}

void CServer::BroadcastFoodGenerated(std::shared_ptr<FoodData> food)
{
	NetworkMessage msg;
	msg.Type = MSG_GENERATE_FOOD;
	msg.Food = *food;

	nlohmann::json json_msg = msg;
	Broadcast(MSG_GENERATE_FOOD, json_msg.dump());
	//std::cout << "广播生成食物:" << food->FoodId << std::endl;
}

void CServer::BroadcastPlayerVomit(VomitData vomitData)
{
	boost::uuids::uuid uuid = boost::uuids::random_generator()();
	vomitData.FoodId = boost::uuids::to_string(uuid);

	NetworkMessage msg;
	msg.Type = MSG_PLAYER_VOMIT;
	msg.VomitData = vomitData;

	nlohmann::json json_msg = msg;
	Broadcast(MSG_PLAYER_VOMIT, json_msg.dump());
}

void CServer::BroadcastToOthers(short msg_id, std::string msg, std::shared_ptr<CSession> exclude_session)
{
	for (auto& [uuid, session] : _sessions)
	{
		if (session == exclude_session || session->IsClose())
			continue;

		session->Send(msg_id, msg);
	}
}

void CServer::Broadcast(short msg_id, std::string msg)
{
	for (auto& [uuid, session] : _sessions)
	{
		if (session->IsClose())
			continue;

		session->Send(msg_id, msg);
	}
}

void CServer::StartAccpet()
{
	auto& ioc = AsioIOServicePool::GetInstance().GetIOService();
	std::shared_ptr<CSession> new_session = std::make_shared<CSession>(ioc, this);

	_acceptor.async_accept(
		new_session->GetSocket(),
		std::bind(&CServer::HandleAccept, this, new_session, std::placeholders::_1)
	);
}

void CServer::HandleAccept(std::shared_ptr<CSession> newSeesion, const boost::system::error_code& err)
{
	if (!err)
	{
		_sessions.insert({ newSeesion->GetSessionUid(), newSeesion });
		std::cout << "Accept Client: " << newSeesion->GetSocket().remote_endpoint().address().to_string() << std::endl;
		newSeesion->Start();
		SendPlayerId(newSeesion);
		SyncAllFoodsTosession(newSeesion);
		SyncAllPositionsTosession(newSeesion);
	}
	else
	{
		std::cout << "Accpet failed! Err msg" << err.message() << std::endl;
	}

	StartAccpet();
}
