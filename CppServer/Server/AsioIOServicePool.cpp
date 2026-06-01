#include "AsioIOServicePool.h"
#include <iostream>

AsioIOServicePool::AsioIOServicePool(std::size_t size)
	: _ioServices(size == 0 ? 1 : size), _next_ioService(0)
{
	_works.reserve(_ioServices.size());
	for (std::size_t i = 0; i < _ioServices.size(); ++i)
	{
		_works.emplace_back(boost::asio::make_work_guard(_ioServices[i]));
	}

	for (std::size_t i = 0; i < _ioServices.size(); ++i)
	{
		_threads.emplace_back([this, i]() {
			_ioServices[i].run();
			});
	}
}

AsioIOServicePool::~AsioIOServicePool()
{
	Stop();
	std::cout << "[~AsioIOServicePool()] destuct " << std::endl;
}

boost::asio::io_context& AsioIOServicePool::GetIOService()
{
	auto& service = _ioServices[_next_ioService++];
	if (_next_ioService == _ioServices.size())
	{
		_next_ioService = 0;
	}
	return service;
}

void AsioIOServicePool::Stop()
{
	// 因为仅仅执行work.reset并不能让iocontext从run的状态中退出
// 当iocontext已经绑定了读或写的监听时间后，还需要手动stop该服务
	for (auto& io_service : _ioServices)
	{
		io_service.stop();
	}

	for (auto& work : _works)
	{
		work.reset();
	}

	for (auto& t : _threads)
	{
		if (t.joinable())
		{
			t.join();
		}
	}
}