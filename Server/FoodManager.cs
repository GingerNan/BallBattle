using System;
using System.Collections.Generic;
using System.Linq;

namespace Server
{
    public class FoodManager
    {
        private static FoodManager _instance;
        public static FoodManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FoodManager();
                }
                return _instance;
            }
        }
        
        private Dictionary<string, FoodData> _foods = new Dictionary<string, FoodData>();
        private readonly object _lock = new object();
        
        private Random _random = new Random();

        public int MaxFoodAmount { get; set; } = 100;
        public float MapWidth { get; set; } = 48;
        public float MapHeight { get; set; } = 48;

        // 初始化生成所有食物
        public void InitializeFoods()
        {
            lock (_lock)
            {
                _foods.Clear();
                for (int i = 0; i < MaxFoodAmount; i++)
                {
                    GenerateFood();
                }
                Console.WriteLine($"初始化生成 {_foods.Count} 个食物");
            }
        }

        /// <summary>
        /// 生成单个食物
        /// </summary>
        /// <returns></returns>
        public FoodData GenerateFood()
        {
            lock (_lock)
            {
                string foodId = Guid.NewGuid().ToString();
                float x = (float)_random.NextDouble() * MapWidth - MapWidth / 2;
                float y = (float)_random.NextDouble() * MapHeight - MapHeight / 2;
                float mass = (float)(_random.NextDouble() * 0.7 + 0.3);
                
                var food = new FoodData()
                {
                    FoodId = foodId,
                    Position = new Vector2(x, y),
                    Mass = mass,
                };

                _foods[foodId] = food;
                return food;
            }
        }

        /// <summary>
        /// 获取当前所有食物
        /// </summary>
        /// <returns></returns>
        public List<FoodData> GetAllFoods()
        {
            lock (_lock)
            {
                return _foods.Values.ToList();
            }
        }

        // 判断该食物存不存在
        public bool FoodExists(string foodId)
        {
            lock (_lock)
            {
                return _foods.ContainsKey(foodId);
            }
        }

        public FoodData HandleFoodRemove(string foodId)
        {
            lock (_lock)
            {
                bool removed = _foods.Remove(foodId);

                if (removed)
                {
                    FoodData newFood = GenerateFood();
                    Console.WriteLine($"生成新食物: {newFood.FoodId}");
                    return newFood;
                }
            }

            return null;
        }
    }
}