#include "CSession.h"
#include "CServer.h"
#include "LogicSystem.h"
#include <iostream>
#include <boost/uuid.hpp>

CSession::CSession(boost::asio::io_context& ioc, CServer* server)
	: _socket(ioc), _server(server), _b_close(false), _b_head_parse(false), _total_mass(0.0f)
{
	boost::uuids::uuid uuid = boost::uuids::random_generator()();
	_session_uid = boost::uuids::to_string(uuid);

	_recv_head_node = std::make_shared<MsgNode>(HEAD_TOTAL_LEN);
}

CSession::~CSession()
{
	std::cout << "CServer Desturct" << std::endl;
}

void CSession::Start()
{
	memset(_data, '\0', MAX_LEN);
	_socket.async_read_some(
		boost::asio::buffer(_data, MAX_LEN),
		std::bind(&CSession::HandleRead, this, std::placeholders::_1, std::placeholders::_2, shared_from_this())
	);
}

void CSession::Close()
{
	std::lock_guard lock(_session_mtx);
	_b_close = true;
	_socket.close();
}

void CSession::Send(char* msg, int max_len)
{
	std::lock_guard<std::mutex> lock(_send_mtx);
	int send_que_size = _send_que.size();
	if (send_que_size > MAX_SEND_QUE)
	{
		std::cout << "Session: " << _session_uid << " send que fulled, size: " << MAX_SEND_QUE << std::endl;
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
		std::bind(&CSession::HandleWrite, this, std::placeholders::_1, shared_from_this())
	);
}

void CSession::Send(std::string msg)
{
	Send(msg.data(), msg.size());
}

void CSession::HandleRead(const boost::system::error_code& err, size_t bytes_transferred,
	std::shared_ptr<CSession> shared_self)
{
	if (err)
	{
		std::cout << "Handle Read failed!, Err code: " << err.value()
			<< ", Err msg: " << err.message() << std::endl;
		Close();
		_server->ClearSession(_session_uid);
		return;
	}

	//已经移动的字符数
	int copy_len = 0;
	while (bytes_transferred > 0)
	{
		if (!_b_head_parse)
		{
			// 处理消息头部
			if (bytes_transferred + _recv_head_node->_cur_len < HEAD_TOTAL_LEN)
			{
				memcpy(_recv_head_node->_data + _recv_head_node->_cur_len, _data + copy_len, bytes_transferred);
				_recv_head_node->_cur_len += bytes_transferred;
				::memset(_data, 0, MAX_LEN);
				_socket.async_read_some(boost::asio::buffer(_data, MAX_LEN),
					std::bind(&CSession::HandleRead, this, std::placeholders::_1, std::placeholders::_2, shared_self));
				return;
			}

			// 处理消息内容
			int head_remain = HEAD_TOTAL_LEN - _recv_head_node->_cur_len;
			memcpy(_recv_head_node->_data + _recv_head_node->_cur_len, _data + copy_len, head_remain);
			copy_len += head_remain;
			bytes_transferred -= head_remain;

			// 获取消息Id
			short msg_id = 0;
			memcpy(&msg_id, _recv_head_node->_data, HEAD_ID_LEN);
			msg_id = boost::asio::detail::socket_ops::network_to_host_short(msg_id);

			// 获取消息长度
			short data_len = 0;
			memcpy(&data_len, _recv_head_node->_data + HEAD_ID_LEN, HEAD_DATA_LEN);
			data_len = boost::asio::detail::socket_ops::network_to_host_short(data_len);

			//头部长度非法
			if (data_len < 0 || data_len > MAX_LEN) {
				std::cout << "invalid data length is " << data_len << std::endl;
				_server->ClearSession(_session_uid);
				return;
			}
			std::cout << "收到的消息Id: " << msg_id << ", 消息长度: " << data_len << std::endl;

			_recv_msg_node = std::make_shared<RecvNode>(msg_id, data_len);
			//消息的长度小于头部规定的长度，说明数据未收全，则先将部分消息放到接收节点里
			if (bytes_transferred < data_len) {
				memcpy(_recv_msg_node->_data + _recv_msg_node->_cur_len, _data + copy_len, bytes_transferred);
				_recv_msg_node->_cur_len += bytes_transferred;
				::memset(_data, 0, MAX_LEN);
				_socket.async_read_some(boost::asio::buffer(_data, MAX_LEN),
					std::bind(&CSession::HandleRead, this, std::placeholders::_1, std::placeholders::_2, shared_self));
				//头部处理完成
				_b_head_parse = true;
				return;
			}

			memcpy(_recv_msg_node->_data + _recv_msg_node->_cur_len, _data + copy_len, data_len);
			_recv_msg_node->_cur_len += data_len;
			copy_len += data_len;
			bytes_transferred -= data_len;
			_recv_msg_node->_data[_recv_msg_node->_total_len] = '\0';

			// 收到完整的消息，处理消息
			LogicSystem::GetInstance().PostMsgToQue(std::make_shared<LogicNode>(shared_from_this(), _recv_msg_node));
			
			//继续轮询剩余未处理数据
			_b_head_parse = false;
			_recv_head_node->Clear();
			if (bytes_transferred <= 0) {
				::memset(_data, 0, MAX_LEN);
				_socket.async_read_some(boost::asio::buffer(_data, MAX_LEN),
					std::bind(&CSession::HandleRead, this, std::placeholders::_1, std::placeholders::_2, shared_self));
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
				std::bind(&CSession::HandleRead, this, std::placeholders::_1, std::placeholders::_2, shared_self));
			return;
		}
		memcpy(_recv_msg_node->_data + _recv_msg_node->_cur_len, _data + copy_len, remain_msg);
		_recv_msg_node->_cur_len += remain_msg;
		bytes_transferred -= remain_msg;
		copy_len += remain_msg;
		_recv_msg_node->_data[_recv_msg_node->_total_len] = '\0';
		LogicSystem::GetInstance().PostMsgToQue(std::make_shared<LogicNode>(shared_from_this(), _recv_msg_node));

		//继续轮询剩余未处理数据
		_b_head_parse = false;
		_recv_head_node->Clear();
		if (bytes_transferred <= 0) {
			::memset(_data, 0, MAX_LEN);
			_socket.async_read_some(boost::asio::buffer(_data, MAX_LEN),
				std::bind(&CSession::HandleRead, this, std::placeholders::_1, std::placeholders::_2, shared_self));
			return;
		}
		continue;
	}
}

void CSession::HandleWrite(const boost::system::error_code& err,
	std::shared_ptr<CSession> shared_self)
{
	if (err)
	{
		std::cout << "Handle Write failed!, Err msg: " << err.message() << std::endl;
		Close();
		_server->ClearSession(_session_uid);
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
			std::bind(&CSession::HandleWrite, this, std::placeholders::_1, shared_self)
		);
	}
}

LogicNode::LogicNode(std::shared_ptr<CSession> client, std::shared_ptr<RecvNode> node)
	: _client(client), _recvnode(node)
{
}
