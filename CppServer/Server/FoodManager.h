#pragma once
#include <unordered_map>
#include <string>
#include <vector>
#include "Data.h"

class FoodManager
{
	static constexpr int MAX_FOOD_AMOUNT = 100;		// 最大食物的数量
	static constexpr float MAP_WIDTH = 48;			// 地图的高度
	static constexpr float MAP_HEIGHT = 48;			// 地图的宽度
public:
 	static FoodManager& GetInstance()
	{
		static FoodManager instance;
		return instance;
	}

	FoodManager(const FoodManager&) = delete;
	FoodManager& operator=(const FoodManager&) = delete;

public:
	// 初始化生成所有食物
	void InitializeFoods();

	// 生成单个食物
	std::shared_ptr<FoodData> GenerateFood();

	// 获取当前所有食物
	std::vector<FoodData> GetAllFoods();

	// 判断食该食物存不存在
	bool IsFoodExist(std::string foodId);

	// 移除食物
	std::shared_ptr<FoodData> RemoveFood(std::string foodId);
private:
	FoodManager();

private:
	std::unordered_map<std::string, std::shared_ptr<FoodData>> _foods;
};

