#include "CClient.h"
#include "CServer.h"
#include <iostream>
#include <boost/uuid.hpp>

CClient::CClient(boost::asio::io_context& ioc, CServer* server)
	: _socket(ioc), _server(server), _b_close(false), _b_head_parse(false), _total_mass(0.0f)
{
	boost::uuids::uuid uuid = boost::uuids::random_generator()();
	_uuid = boost::uuids::to_string(uuid);
	_recv_head_node = std::make_shared<MsgNode>(HEAD_LEN);
}

CClient::~CClient()
{
	std::cout << "CSession Desturct" << std::endl;
}

void CClient::Start()
{
	memset(_data, '\0', MAX_LEN);
	_socket.async_read_some(
		boost::asio::buffer(_data, MAX_LEN),
		std::bind(&CClient::HandleRead, this, std::placeholders::_1, std::placeholders::_2, shared_from_this())
	);
}

void CClient::Close()
{
	std::lock_guard lock(_session_mtx);
	_b_close = true;
	_socket.close();
}

void CClient::Send(char* msg, int max_len)
{
	std::lock_guard<std::mutex> lock(_send_mtx);
	int send_que_size = _send_que.size();
	if (send_que_size > MAX_SEND_QUE)
	{
		std::cout << "Session: " << _uuid << " send que fulled, size: " << MAX_SEND_QUE << std::endl;
		return;
	}

	_send_que.push(std::make_shared<MsgNode>(msg, max_len));
	if (send_que_size > 0)
	{
		return;
	}

	auto& msg_node = _send_que.front();
	boost::asio::async_write(
		_socket,
		boost::asio::buffer(msg_node->_data, msg_node->_total_len),
		std::bind(&CClient::HandleWrite, this, std::placeholders::_1, shared_from_this())
	);
}

void CClient::Send(std::string msg)
{
	Send(msg.data(), msg.size());
}

void CClient::HandleRead(const boost::system::error_code& err, size_t bytes_transferred,
	std::shared_ptr<CClient> shared_self)
{
	if (err)
	{
		std::cout << "Handle Read failed!, Err code: " << err.value()
			<< ", Err msg: " << err.message() << std::endl;
		Close();
		_server->ClearSession(_uuid);
		return;
	}

	//已经移动的字符数
	int copy_len = 0;
	while (bytes_transferred > 0) {
		if (!_b_head_parse) {
			//收到的数据不足头部大小
			if (bytes_transferred + _recv_head_node->_cur_len < HEAD_LEN) {
				memcpy(_recv_head_node->_data + _recv_head_node->_cur_len, _data + copy_len, bytes_transferred);
				_recv_head_node->_cur_len += bytes_transferred;
				::memset(_data, 0, MAX_LEN);
				_socket.async_read_some(boost::asio::buffer(_data, MAX_LEN),
					std::bind(&CClient::HandleRead, this, std::placeholders::_1, std::placeholders::_2, shared_self));
				return;
			}
			//收到的数据比头部多
			//头部剩余未复制的长度
			int head_remain = HEAD_LEN - _recv_head_node->_cur_len;
			memcpy(_recv_head_node->_data + _recv_head_node->_cur_len, _data + copy_len, head_remain);
			//更新已处理的data长度和剩余未处理的长度
			copy_len += head_remain;
			bytes_transferred -= head_remain;
			//获取头部数据
			short data_len = 0;
			memcpy(&data_len, _recv_head_node->_data, HEAD_LEN);
			data_len = boost::asio::detail::socket_ops::network_to_host_short(data_len);
			std::cout << "data_len is " << data_len << std::endl;
			//头部长度非法
			if (data_len > MAX_LEN) {
				std::cout << "invalid data length is " << data_len << std::endl;
				_server->ClearSession(_uuid);
				return;
			}

			_recv_msg_node = std::make_shared<MsgNode>(data_len);
			//消息的长度小于头部规定的长度，说明数据未收全，则先将部分消息放到接收节点里
			if (bytes_transferred < data_len) {
				memcpy(_recv_msg_node->_data + _recv_msg_node->_cur_len, _data + copy_len, bytes_transferred);
				_recv_msg_node->_cur_len += bytes_transferred;
				::memset(_data, 0, MAX_LEN);
				_socket.async_read_some(boost::asio::buffer(_data, MAX_LEN),
					std::bind(&CClient::HandleRead, this, std::placeholders::_1, std::placeholders::_2, shared_self));
				//头部处理完成
				_b_head_parse = true;
				return;
			}

			memcpy(_recv_msg_node->_data + _recv_msg_node->_cur_len, _data + copy_len, data_len);
			_recv_msg_node->_cur_len += data_len;
			copy_len += data_len;
			bytes_transferred -= data_len;
			_recv_msg_node->_data[_recv_msg_node->_total_len] = '\0';
			//std::cout << "receive data is " << _recv_msg_node->_data << std::endl;
			//此处可以调用Send发送测试
			//Send(_recv_msg_node->_data, _recv_msg_node->_total_len);
			
			//Json::Value data;
			//Json::Reader reader;
			//reader.parse(std::string(_recv_msg_node->_data, _recv_msg_node->_total_len), data);
			//std::cout << data.toStyledString() << std::endl;

			//std::string new_str = "server has received msg, msg data: " + data["data"].asString();
			//Json::Value return_data;
			//return_data["id"] = data["id"].asInt();
			//return_data["data"] = new_str;
			//Send(return_data.toStyledString());
			ProcessMessage(_recv_msg_node);
			
			//继续轮询剩余未处理数据
			_b_head_parse = false;
			_recv_head_node->Clear();
			if (bytes_transferred <= 0) {
				::memset(_data, 0, MAX_LEN);
				_socket.async_read_some(boost::asio::buffer(_data, MAX_LEN),
					std::bind(&CClient::HandleRead, this, std::placeholders::_1, std::placeholders::_2, shared_self));
				return;
			}
			continue;
		}

		//已经处理完头部，处理上次未接受完的消息数据
		//接收的数据仍不足剩余未处理的
		int remain_msg = _recv_msg_node->_total_len - _recv_msg_node->_cur_len;
		if (bytes_transferred < remain_msg) {
			memcpy(_recv_msg_node->_data + _recv_msg_node->_cur_len, _data + copy_len, bytes_transferred);
			_recv_msg_node->_cur_len += bytes_transferred;
			::memset(_data, 0, MAX_LEN);
			_socket.async_read_some(boost::asio::buffer(_data, MAX_LEN),
				std::bind(&CClient::HandleRead, this, std::placeholders::_1, std::placeholders::_2, shared_self));
			return;
		}
		memcpy(_recv_msg_node->_data + _recv_msg_node->_cur_len, _data + copy_len, remain_msg);
		_recv_msg_node->_cur_len += remain_msg;
		bytes_transferred -= remain_msg;
		copy_len += remain_msg;
		_recv_msg_node->_data[_recv_msg_node->_total_len] = '\0';
		//std::cout << "receive data is " << _recv_msg_node->_data << std::endl;
		//此处可以调用Send发送测试
		//Send(_recv_msg_node->_data, _recv_msg_node->_total_len);

		//Json::Value data;
		//Json::Reader reader;
		//reader.parse(std::string(_recv_msg_node->_data, _recv_msg_node->_total_len), data);
		//std::cout << data.toStyledString() << std::endl;

		//std::string new_str = "server has received msg, msg data: " + data["data"].asString();
		//Json::Value return_data;
		//return_data["id"] = data["id"].asInt();
		//return_data["data"] = new_str;
		//Send(return_data.toStyledString());
		ProcessMessage(_recv_msg_node);

		//继续轮询剩余未处理数据
		_b_head_parse = false;
		_recv_head_node->Clear();
		if (bytes_transferred <= 0) {
			::memset(_data, 0, MAX_LEN);
			_socket.async_read_some(boost::asio::buffer(_data, MAX_LEN),
				std::bind(&CClient::HandleRead, this, std::placeholders::_1, std::placeholders::_2, shared_self));
			return;
		}
		continue;
	}
}

void CClient::HandleWrite(const boost::system::error_code& err,
	std::shared_ptr<CClient> shared_self)
{
	if (err)
	{
		std::cout << "Handle Write failed!, Err msg: " << err.message() << std::endl;
		Close();
		_server->ClearSession(_uuid);
		return;
	}

	std::lock_guard<std::mutex> lock(_send_mtx);
	_send_que.pop();
	
	if (!_send_que.empty())
	{
		auto& msg_node = _send_que.front();
		boost::asio::async_write(
			_socket,
			boost::asio::buffer(msg_node->_data, msg_node->_total_len),
			std::bind(&CClient::HandleWrite, this, std::placeholders::_1, shared_self)
		);
	}
}

void CClient::ProcessMessage(std::shared_ptr<MsgNode> msg_node)
{
	NetworkMessage message;
	try
	{
		const std::string payload(msg_node->_data, msg_node->_total_len);
		message = nlohmann::json::parse(payload).get<NetworkMessage>();
	}
	catch (const nlohmann::json::exception& e)
	{
		std::cout << "网络消息解析失败：" << e.what() << std::endl;
		return;
	}

	switch (message.Type)
	{
	case PlayerJoin:
		HandlePlayerJoin();
		break;
	case SendPosition:
		HandleSendPosition(message);
		break;
	case RemoveFood:
		HandleRemoveFood(message.FoodId);
		break;
	case PlayerVomit:
		HandlePlayerVomit(message.VomitData);
		break;
	case PlayerLeave:
		std::cout << "PlayerLeave, PlayerId: " << message.PlayerId << std::endl;
		break;
	default:
		break;
	}
}

void CClient::HandlePlayerJoin()
{
	auto rsp = nlohmann::json{
		{"Type", PlayerJoin},
		{"PlayerId", _uuid}
	};

	Send(rsp.dump());
	std::cout << "PlayerJoin, PlayerId: " << _uuid << std::endl;
}

void CClient::HandleSendPosition(NetworkMessage message)
{
	_postion = message.Position;
	if (!(message.Position.x == 0 && message.Position.y == 0))
	{
		_balls = message.PlayerPosition.Balls;
		_total_mass = message.PlayerPosition.TotalMass;
	}

	_server->HandlePlayerPosition(shared_from_this());
}

void CClient::HandleRemoveFood(std::string foodId)
{
	std::cout << "食物移除: " << foodId << std::endl;
	_server->HandleRemoveFood(foodId, shared_from_this());
}

void CClient::HandlePlayerVomit(VomitData vomidData)
{
	std::cout << "玩家 " << vomidData.PlayerId << "吐球，质量：" << vomidData.Mass << std::endl;
	_server->HandlePlayerVomit(vomidData);
}
