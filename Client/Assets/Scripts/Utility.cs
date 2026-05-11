using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Utility : MonoSingleton<Utility>
{
    [Header("生成随机范围")]
    [SerializeField] private Transform leftUp;
    [SerializeField] private Transform rightUp;

    /// <summary>
    /// 获取地图上随机位置
    /// </summary>
    /// <returns></returns>
    public (float x, float y) GetRandomPositionInMap()
    {
        float yMax = leftUp.position.x;
        float xMax = rightUp.position.x;
        float xMin = leftUp.position.x;
        float yMin = rightUp.position.x;
        
        float xPos = Random.Range(xMin, xMax);
        float yPos = Random.Range(yMin, yMax);
        return (xPos, yPos);
    }
}
