#include "LogicSystem.h"
#include <iostream>

LogicSystem::LogicSystem() : _bStop(false), _server(nullptr)
{
	RegisterMsgHandlers();
	_worker_thread = std::thread(&LogicSystem::DealMsg, this);
}

LogicSystem::~LogicSystem()
{
	_bStop = true;
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
		std::unique_lock lock(_mtx);
		_cv.wait(lock, [this]() {
			return _msg_que.empty() && !_bStop;
			});

		// 退出线程之前，把队列里剩余的消息处理完
		if (_bStop)
		{
			while (!_msg_que.empty())
			{
				auto msg_node = _msg_que.front();
				auto call_back_iter = _handlers.find(msg_node->_recvnode->_msg_id);
				if (call_back_iter == _handlers.end())
				{
					_msg_que.pop();
					continue;
				}

				call_back_iter->second(
					msg_node->_client,
					msg_node->_recvnode->_msg_id,
					std::string(msg_node->_recvnode->_data, msg_node->_recvnode->_total_len)
				);
				_msg_que.pop();
			}
			break;
		}

		auto msg_node = _msg_que.front();
		auto call_back_iter = _handlers.find(msg_node->_recvnode->_msg_id);
		if (call_back_iter == _handlers.end())
		{
			_msg_que.pop();
			continue;
		}

		call_back_iter->second(
			msg_node->_client,
			msg_node->_recvnode->_msg_id,
			std::string(msg_node->_recvnode->_data, msg_node->_recvnode->_total_len)
		);
		_msg_que.pop();
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
	// TODO playerid因该有一个管理playerid的系统来生成和分配，这里先用session id代替
	auto rsp = nlohmann::json{
		{"Type", MSG_PLAYER_JOIN},
		{"PlayerId", session->GetSessionUid()}
	};

	session->Send(rsp.dump());
	std::cout << "PlayerJoin, PlayerId: " << session->GetSessionUid() << std::endl;
}

void LogicSystem::HandleSendPosition(std::shared_ptr<CSession> session, const short&, const std::string&)
{
	_postion = message.Position;
	if (!(message.Position.x == 0 && message.Position.y == 0))
	{
		_balls = message.PlayerPosition.Balls;
		_total_mass = message.PlayerPosition.TotalMass;
	}

	_server->HandlePlayerPosition(shared_from_this());
}

void LogicSystem::HandleRemoveFood(std::shared_ptr<CSession> session, const short&, const std::string&)
{
}

void LogicSystem::HandlePlayerVomit(std::shared_ptr<CSession> session, const short&, const std::string&)
{
}
