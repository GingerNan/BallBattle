#include "CServer.h"
#include "AsioIOServicePool.h"

#include <boost/asio.hpp>
#include <csignal>
#include <thread>
#include <iostream>


int main()
{
	try
	{
		auto& pool = AsioIOServicePool::GetInstance();


		boost::asio::io_context ioc;
		auto server = std::make_shared<CServer>(ioc, 8888);

		boost::asio::signal_set signals(ioc, SIGINT, SIGTERM);
		signals.async_wait([&ioc, &pool](auto, auto) {
			ioc.stop();
			pool.Stop();
			});

		ioc.run();	// 在使用异步模式时,要run()
	}
	catch (std::exception& e)
	{
		std::cout << "Exception: " << e.what() << std::endl;
	}
	return 0;
}