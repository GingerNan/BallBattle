#pragma once
#include <functional>
#include <mutex>
#include <thread>
#include <queue>
#include <condition_variable>
#include <map>

#include "CSession.h"

class CServer;
typedef std::function<void(std::shared_ptr<CSession>, const short& msg_id, const std::string&)> MessageHandler;

class LogicSystem
{
public:
	static LogicSystem& GetInstance()
	{
		static LogicSystem instance;
		return instance;
	}

	LogicSystem(const LogicSystem&) = delete;
	LogicSystem& operator=(const LogicSystem&) = delete;
	~LogicSystem();

	void PostMsgToQue(std::shared_ptr<LogicNode> msg);
	void SetServer(std::shared_ptr<CServer> server);

private:
	LogicSystem();

	void DealMsg();

	void RegisterMsgHandlers();

private:
	// 玩家处理连接
	void HandlePlayerJoin(std::shared_ptr<CSession> session, const short&, const std::string&);

	// 处理玩家位置和状态
	void HandleSendPosition(std::shared_ptr<CSession> session, const short&, const std::string&);

	// 处理移除食物
	void HandleRemoveFood(std::shared_ptr<CSession> session, const short&, const std::string&);

	// 处理玩家吐球
	void HandlePlayerVomit(std::shared_ptr<CSession> session, const short&, const std::string&);

private:
	std::thread _worker_thread;
	std::queue<std::shared_ptr<LogicNode>> _msg_que;
	std::mutex _mtx;
	std::condition_variable _cv;
	bool _bStop;
	std::map<short, MessageHandler> _handlers;
	std::shared_ptr<CServer> _server;
};

