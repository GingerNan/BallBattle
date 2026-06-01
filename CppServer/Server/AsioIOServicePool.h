#pragma once
#include <vector>
#include <boost/asio.hpp>

class AsioIOServicePool
{
	using IOService = boost::asio::io_context;
	using Work = boost::asio::executor_work_guard<IOService::executor_type>;
public:
	static AsioIOServicePool& GetInstance()
	{
		static AsioIOServicePool instance;
		return instance;
	}

	AsioIOServicePool(const AsioIOServicePool&) = delete;
	AsioIOServicePool& operator=(const AsioIOServicePool&) = delete;
	~AsioIOServicePool();

	boost::asio::io_context& GetIOService();

	void Stop();
private:
	AsioIOServicePool(std::size_t size = std::thread::hardware_concurrency());
private:
	std::vector<IOService> _ioServices;
	std::vector<Work> _works;
	std::vector<std::thread> _threads;
	std::size_t _next_ioService;
};

