using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodBall : MonoBehaviour, IEatable
{
    [SerializeField] private float mass = 0.3f;
    public float Mass { get => mass; set => mass = value; }

    /// <summary>
    /// 初始化质量
    /// </summary>
    /// <param name="newMass"></param>
    public void SetMass(float newMass)
    {
        Mass = newMass;
        transform.localScale = new Vector3(newMass, newMass, 1);
    }
    
    /// <summary>
    /// 自己被吃掉了
    /// </summary>
    /// <param name="playerBall"></param>
    public void BeEaten(BallController playerBall)
    {
        playerBall.AddMass(mass);
        Destroy(gameObject);
    }
}
