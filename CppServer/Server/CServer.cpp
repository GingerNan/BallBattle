#include "CServer.h"
#include "CSession.h"
#include <iostream>

CServer::CServer(boost::asio::io_context& ioc, unsigned short port)
	: _ioc(ioc), _port(port), 
	_acceptor(ioc, boost::asio::ip::tcp::endpoint(boost::asio::ip::tcp::v4(), port))
{
	std::cout << "Server Start! listen port: " << port << std::endl;
	StartAccpet();
}

void CServer::ClearSession(std::string uuid)
{
	_sessions.erase(uuid);
}

void CServer::StartAccpet()
{
	std::shared_ptr<CSession> new_session = std::make_shared<CSession>(_ioc, this);
	_acceptor.async_accept(
		new_session->GetSocket(),
		std::bind(&CServer::HandleAccept, this, new_session, std::placeholders::_1)
	);
}

void CServer::HandleAccept(std::shared_ptr<CSession> newSeesion, const boost::system::error_code& err)
{
	if (!err)
	{
		newSeesion->Start();
		_sessions.insert({ newSeesion->GetUuid(), newSeesion });
	}
	else
	{
		std::cout << "Accpet failed! Err msg" << err.message() << std::endl;
	}

	StartAccpet();
}
