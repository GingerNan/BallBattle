#pragma once
#include "MsgNode.h"
#include "Data.h"
#include "Common.h"
#include <queue>
#include <mutex>

class CServer;
class CSession : public std::enable_shared_from_this<CSession>
{
	friend class LogicSystem;

public:
	CSession(boost::asio::io_context& ioc, CServer* server);
	~CSession();

	boost::asio::ip::tcp::socket& GetSocket() { return _socket; }
	std::string& GetSessionUid() { return _session_uid; }

	bool IsClose() const { return _b_close; }

	void Start();
	void Close();

	void Send(short msg_id, char* msg, int msg_len);
	void Send(short msg_id, std::string msg);

	void BindPlayer(std::string playerId);
	std::string GetPlayerId();
private:
	void HandleRead(const boost::system::error_code& err, size_t bytes_transferred,
		std::shared_ptr<CSession> shared_self);

	void HandleWrite(const boost::system::error_code& err, std::shared_ptr<CSession> shared_self);
private:
	std::string _session_uid;
	boost::asio::ip::tcp::socket _socket;
	char _data[MAX_LEN];

	CServer* _server;
	
	bool _b_close;

	std::queue<std::shared_ptr<SendNode>> _send_que;
	std::mutex _send_mtx;

	// 收到的消息结构
	std::shared_ptr<RecvNode> _recv_msg_node;
	bool _b_head_parse;
	std::shared_ptr<MsgNode> _recv_head_node;
	std::mutex _session_mtx;

	// 玩家Id
	std::string _playerId;
};

class LogicNode
{
	friend class LogicSystem;
public:
	LogicNode(std::shared_ptr<CSession> client, std::shared_ptr<RecvNode> node);
private:
	std::shared_ptr<CSession> _client;
	std::shared_ptr<RecvNode> _recvnode;
};
