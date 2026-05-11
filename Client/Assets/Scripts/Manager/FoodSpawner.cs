using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class FoodSpawner : MonoBehaviour
{
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private int maxFoodAmount = 100;
    public GameObject foodParent;
    private int currentFoodAmount;

    private void Start()
    {
        StartCoroutine(SpawnAllFood());
    }

    private IEnumerator SpawnAllFood()
    {
        for (int i = 0; i < maxFoodAmount; i++)
        {
            SpawnFood();

            if (i % 5 == 0)
            {
                yield return new WaitForEndOfFrame();
            }
            
            Debug.Log($"当前生成了{currentFoodAmount}个食物");
        }
    }

    public void SpawnFood()
    {
        (float xPos, float yPos) = Utility.Instance.GetRandomPositionInMap();
        GameObject newFood = Instantiate(foodPrefab, new Vector3(xPos, yPos), Quaternion.identity,foodParent.transform);
        
        float randomMass = Random.Range(0.3f, 1f);
        FoodBall food = newFood.GetComponent<FoodBall>();
        food.SetMass(randomMass);
        
        currentFoodAmount++;
    }
}
