#pragma once
#include "CSession.h"
#include <map>

class CServer
{
public:
	CServer(boost::asio::io_context& ioc, unsigned short port);

	void ClearSession(std::string uuid);
private:
	void StartAccpet();

	void HandleAccept(std::shared_ptr<CSession> newSeesion, const boost::system::error_code& err);

private:
	boost::asio::io_context& _ioc;
	boost::asio::ip::tcp::acceptor _acceptor;
	unsigned short _port;
	std::map<std::string, std::shared_ptr<CSession>> _sessions;
};

