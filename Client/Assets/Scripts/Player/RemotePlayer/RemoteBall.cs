using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoteBall : MonoBehaviour, IEatable
{
    public string BallId { get; set; }
    public string PlayerId { get; set; }

    private float mass = 1f;

    public float Mass
    {
        get => mass;
        set
        {
            mass = value;
            transform.localScale = new Vector3(mass, mass, 1);
        }
    }
    
    public void BeEaten(BallController playerBall)
    {
        Destroy(gameObject);
    }

    // 更新位置
    public void UpdatePosition(Vector3 position)
    {
        transform.position = position;
    }

    // 更新质量
    public void SetMass(float _mass)
    {
        Mass = _mass;
    }
    
    // 设置球Id
    public void SetBallId(string ballId)
    {
        if (string.IsNullOrEmpty(BallId))
        {
            Debug.Log($"问题Id");
        }
        else
        {
            BallId = ballId;
        }
    }
    
    // 设置玩家Id
    public void SetPlayerId(string playerId)
    {
        PlayerId = playerId;
    }
}
