#include "LogicSystem.h"
#include "CServer.h"
#include <iostream>

namespace
{
bool ParseNetworkMessage(const std::string& msg_data, NetworkMessage& message)
{
	try
	{
		message = nlohmann::json::parse(msg_data).get<NetworkMessage>();
		return true;
	}
	catch (const nlohmann::json::exception& e)
	{
		std::cout << "Network message parse failed: " << e.what() << std::endl;
		return false;
	}
}
}

LogicSystem::LogicSystem() : _bStop(false), _server(nullptr)
{
	RegisterMsgHandlers();
	_worker_thread = std::thread(&LogicSystem::DealMsg, this);
}

LogicSystem::~LogicSystem()
{
	{
		std::lock_guard lock(_mtx);
		_bStop = true;
	}
	_cv.notify_one();
	if (_worker_thread.joinable())
	{
		_worker_thread.join();
	}
}

void LogicSystem::PostMsgToQue(std::shared_ptr<LogicNode> msg)
{
	std::unique_lock lock(_mtx);
	_msg_que.push(msg);
	if (_msg_que.size() == 1)
	{
		lock.unlock();
		_cv.notify_one();
	}
}

void LogicSystem::SetServer(std::shared_ptr<CServer> server)
{
	_server = server;
}

void LogicSystem::DealMsg()
{
	for (;;)
	{
		std::shared_ptr<LogicNode> msg_node;
		{
			std::unique_lock lock(_mtx);
			_cv.wait(lock, [this]() {
				return !_msg_que.empty() || _bStop;
				});

			if (_bStop && _msg_que.empty())
			{
				break;
			}

			msg_node = _msg_que.front();
			_msg_que.pop();
		}

		auto call_back_iter = _handlers.find(msg_node->_recvnode->_msg_id);
		if (call_back_iter == _handlers.end())
		{
			continue;
		}

		call_back_iter->second(
			msg_node->_client,
			msg_node->_recvnode->_msg_id,
			std::string(msg_node->_recvnode->_data, msg_node->_recvnode->_total_len)
		);
	}
}

void LogicSystem::RegisterMsgHandlers()
{
	_handlers[MSG_PLAYER_JOIN] = std::bind(&LogicSystem::HandlePlayerJoin, this, std::placeholders::_1, std::placeholders::_2, std::placeholders::_3);
	_handlers[MSG_SEND_POSITION] = std::bind(&LogicSystem::HandleSendPosition, this, std::placeholders::_1, std::placeholders::_2, std::placeholders::_3);
	_handlers[MSG_REMOVE_FOOD] = std::bind(&LogicSystem::HandleRemoveFood, this, std::placeholders::_1, std::placeholders::_2, std::placeholders::_3);
	_handlers[MSG_PLAYER_VOMIT] = std::bind(&LogicSystem::HandlePlayerVomit, this, std::placeholders::_1, std::placeholders::_2, std::placeholders::_3);
}

void LogicSystem::HandlePlayerJoin(std::shared_ptr<CSession> session, const short&, const std::string&)
{
	NetworkMessage rsp;
	rsp.Type = MSG_PLAYER_JOIN;
	rsp.PlayerId = session->GetSessionUid();

	nlohmann::json json_msg = rsp;
	session->Send(json_msg.dump());
	std::cout << "PlayerJoin, PlayerId: " << session->GetSessionUid() << std::endl;
}

void LogicSystem::HandleSendPosition(std::shared_ptr<CSession> session, const short&, const std::string& msg_data)
{
	NetworkMessage message;
	if (!ParseNetworkMessage(msg_data, message))
	{
		return;
	}

	session->_postion = message.Position;
	if (!(message.Position.x == 0 && message.Position.y == 0))
	{
		session->_balls = message.PlayerPosition.Balls;
		session->_total_mass = message.PlayerPosition.TotalMass;
	}

	_server->HandlePlayerPosition(session);
}

void LogicSystem::HandleRemoveFood(std::shared_ptr<CSession> session, const short&, const std::string& msg_data)
{
	NetworkMessage message;
	if (!ParseNetworkMessage(msg_data, message))
	{
		return;
	}

	std::cout << "Remove food: " << message.FoodId << std::endl;
	_server->HandleRemoveFood(message.FoodId, session);
}

void LogicSystem::HandlePlayerVomit(std::shared_ptr<CSession> session, const short&, const std::string& msg_data)
{
	NetworkMessage message;
	if (!ParseNetworkMessage(msg_data, message))
	{
		return;
	}

	std::cout << "Player " << message.VomitData.PlayerId << " vomit, mass: " << message.VomitData.Mass << std::endl;
	_server->HandlePlayerVomit(message.VomitData);
}
