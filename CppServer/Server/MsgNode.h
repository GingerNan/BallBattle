#pragma once
#include <cstring>
#include <boost/asio.hpp>
#include "Common.h"

struct MsgNode
{
	MsgNode(short max_len): _total_len(max_len), _cur_len(0)
	{
		_data = new char[max_len+1]();
		_data[_total_len] = '\0';
	}

	~MsgNode()
	{
		delete[] _data;
	}

	void Clear()
	{
		::memset(_data, '\0', _total_len);
		_cur_len = 0;
	}

	short _cur_len;
	short _total_len;
	char* _data;
};

class RecvNode : public MsgNode
{
	friend class LogicSystem;
public:
	RecvNode(short msg_id, short max_len);
private:
	short _msg_id;
};

class SendNode : public MsgNode
{
	friend class LogicSystem;
public:
	SendNode(short msg_id, const char* msg, short max_len);
private:
	short _msg_id;
};
