#pragma once
#include "MsgNode.h"
#include "Data.h"
#include "Common.h"
#include <queue>
#include <mutex>

class CServer;
class CClient : public std::enable_shared_from_this<CClient>
{
public:
	CClient(boost::asio::io_context& ioc, CServer* server);
	~CClient();

	boost::asio::ip::tcp::socket& GetSocket() { return _socket; }
	std::string& GetUuid() { return _uuid; }
	float GetTotalMass() { return _total_mass; }

	const Vector2& GetPostion() const { return _postion; }
	const std::vector<BallData>& GetBalls() const { return _balls; }

	bool IsClose() const { return _b_close; }

	void Start();
	void Close();

	// 发送消息，msg必须保证在发送完成前有效
	void Send(char* msg, int max_len);
	void Send(std::string msg);
private:
	// 玩家初次连接
	void HandlePlayerJoin();

	// 处理玩家位置和状态
	void HandleSendPosition(NetworkMessage message);

	// 处理移除食物
	void HandleRemoveFood(std::string foodId);

	// 处理玩家吐球
	void HandlePlayerVomit(VomitData vomidData);

private:
	void HandleRead(const boost::system::error_code& err, size_t bytes_transferred,
		std::shared_ptr<CClient> shared_self);

	void HandleWrite(const boost::system::error_code& err, std::shared_ptr<CClient> shared_self);

	void ProcessMessage(std::shared_ptr<MsgNode> msg_node);
private:
	std::string _uuid;
	boost::asio::ip::tcp::socket _socket;
	char _data[MAX_LEN];

	CServer* _server;
	
	bool _b_close;

	std::queue<std::shared_ptr<MsgNode>> _send_que;
	std::mutex _send_mtx;

	// 收到的消息结构
	std::shared_ptr<MsgNode> _recv_msg_node;
	bool _b_head_parse;
	std::shared_ptr<MsgNode> _recv_head_node;
	std::mutex _session_mtx;

	//玩家状态信息
	Vector2 _postion;
	std::vector<BallData> _balls;
	float _total_mass;
};

