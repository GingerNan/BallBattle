#include <iostream>
#include "CServer.h"
#include <boost/asio.hpp>

int main()
{
	try
	{
		boost::asio::io_context ioc;
		CServer server(ioc, 10086);
		ioc.run();	// 在使用异步模式时，要run()
	}
	catch (std::exception& e)
	{
		std::cout << "Exception: " << e.what() << std::endl;
	}
	return 0;
}