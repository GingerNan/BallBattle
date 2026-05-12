using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using UnityEngine;
using Random = UnityEngine.Random;

public class FoodSpawner : MonoSingleton<FoodSpawner>
{
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private int maxFoodAmount = 100;
    public GameObject foodParent;
    
    private Dictionary<string, GameObject> _spawnedFoods = new Dictionary<string, GameObject>();
    private int currentFoodAmount;

    private void Start()
    {
        EventCenter.Instance.AddEventListener<FoodData>(GameEvent.食物生成, HandleFoodGenerate);
        EventCenter.Instance.AddEventListener<string>(GameEvent.食物移除, HandleFoodRemove);
        EventCenter.Instance.AddEventListener<List<FoodData>>(GameEvent.同步食物, HandleFoodsSynced);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        EventCenter.Instance.RemoveEventListener<FoodData>(GameEvent.食物生成, HandleFoodGenerate);
        EventCenter.Instance.RemoveEventListener<string>(GameEvent.食物移除, HandleFoodRemove);
        EventCenter.Instance.RemoveEventListener<List<FoodData>>(GameEvent.同步食物, HandleFoodsSynced);
    }
    
    /// <summary>
    /// 生成单个食物
    /// </summary>
    /// <param name="foodData"></param>
    private void SpawnFood(FoodData foodData)
    {
        Vector3 position = new Vector3(foodData.Position.X, foodData.Position.Y, 0);
        GameObject newFood = Instantiate(foodPrefab, position, Quaternion.identity, foodParent.transform);
        
        FoodBall food = newFood.GetComponent<FoodBall>();
        food.SetMass(foodData.Mass);
        food.SetFoodId(foodData.FoodId);
        
        _spawnedFoods[foodData.FoodId] = newFood;
        currentFoodAmount++;
    }

    #region 网络订阅事件

    /// <summary>
    /// 处理服务器生成食物
    /// </summary>
    /// <param name="foodData"></param>
    private void HandleFoodGenerate(FoodData foodData)
    {
        if (!_spawnedFoods.ContainsKey(foodData.FoodId))
        {
            SpawnFood(foodData);
        }
    }
    
    /// <summary>
    /// 处理服务器同步所有食物
    /// </summary>
    /// <param name="foods"></param>
    private void HandleFoodsSynced(List<FoodData> foods)
    {
        // 清空原有
        foreach (var food in _spawnedFoods.Values)
        {
            Destroy(food);
        }
        _spawnedFoods.Clear();

        // 生成新的
        foreach (FoodData food in foods)
        {
            SpawnFood(food);
        }
        
        currentFoodAmount = _spawnedFoods.Count;
        Debug.Log($"同步生成{currentFoodAmount}个食物");
    }

    /// <summary>
    /// 处理服务器移除食物
    /// </summary>
    /// <param name="foodId"></param>
    private void HandleFoodRemove(string foodId)
    {
        if (_spawnedFoods.TryGetValue(foodId, out GameObject foodObj))
        {
            _spawnedFoods.Remove(foodId);
            currentFoodAmount--;
            Destroy(foodObj);
            
            // 发送网络消息告诉服务器食物被吃掉
            NetworkManager.Instance.SendFoodEatenMessage(foodId);
        }
    }
    
    #endregion
    

}
