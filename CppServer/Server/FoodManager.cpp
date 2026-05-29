#include "FoodManager.h"
#include "RandomUtil.h"
#include <iostream>
#include <boost/uuid.hpp>

FoodManager::FoodManager() {}

void FoodManager::InitializeFoods()
{
	_foods.clear();
	for (int i = 0; i < MAX_FOOD_AMOUNT; ++i)
	{
		auto food = GenerateFood();
		// std::cout << "生成食物: " << food->FoodId << std::endl;
	}
}

std::shared_ptr<FoodData> FoodManager::GenerateFood()
{
	boost::uuids::uuid uuid = boost::uuids::random_generator()();
	std::string foodId = boost::uuids::to_string(uuid);
	float x = RandomUtil::Range(-MAP_WIDTH / 2, MAP_WIDTH / 2);
	float y = RandomUtil::Range(-MAP_HEIGHT / 2, MAP_HEIGHT / 2);
	float mass = RandomUtil::Range(0.3f, 1.0f);

	std::shared_ptr<FoodData> food = std::make_shared<FoodData>();
	food->FoodId = foodId;
	food->Position = Vector2(x, y);
	food->Mass = mass;
	_foods[foodId] = food;
	return food;
}

std::vector<FoodData> FoodManager::GetAllFoods()
{
	std::vector<FoodData> foods;
	for (const auto& pair : _foods)
	{
		foods.push_back(*pair.second);
	}
	return foods;
}

bool FoodManager::IsFoodExist(std::string foodId)
{
	return _foods.find(foodId) != _foods.end();
}

std::shared_ptr<FoodData> FoodManager::RemoveFood(std::string foodId)
{
	size_t num = _foods.erase(foodId);
	if (num > 0)
	{
		auto newFood = GenerateFood();
		std::cout << "生成新食物: " << newFood->FoodId << std::endl;
		return newFood;
	}
	return nullptr;
}
