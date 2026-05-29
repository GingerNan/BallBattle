#pragma once

#include <random>

namespace RandomUtil
{
	inline std::mt19937& Generator()
	{
		static std::random_device rd;
		static std::mt19937 gen(rd());
		return gen;
	}

	inline float Range(float min, float max)
	{
		std::uniform_real_distribution<float> dis(min, max);
		return dis(Generator());
	}
}
