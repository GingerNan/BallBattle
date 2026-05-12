using System.Collections;
using System.Collections.Generic;
using Server;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class FoodBall : MonoBehaviour, IEatable
{
    private string _foodId;
    [SerializeField] private float mass = 0.3f;
    public bool isFromPlayer = false;
    
    private bool isMoving = true;
    private float spawnTime;
    private Vector2 moveDirection;
    private float moveSpeed = 3f;
    private float moveDuration = 1f;
    private float moveElapsed = 0f;
    
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
    /// 初始化ID
    /// </summary>
    /// <param name="foodId"></param>
    public void SetFoodId(string foodId)
    {
        _foodId = foodId;
    }

    public void InitMovement(Vector2 direction, float speed, float duration)
    {
        moveDirection = direction;
        moveSpeed = speed;
        moveDuration = duration;
        spawnTime = Time.time;

        StartCoroutine(MoveCoroutine());
    }

    /// <summary>
    /// 吐出来的球移动
    /// </summary>
    /// <returns></returns>
    private IEnumerator MoveCoroutine()
    {
        while (moveElapsed < moveDuration && isMoving)
        {
            moveElapsed += Time.deltaTime;
            
            // 移动距离
            Vector2 movement = moveDirection * ( moveSpeed * Time.deltaTime);
            transform.position += (Vector3)movement;
            
            // 逐渐减速
            moveSpeed = Mathf.Lerp(moveSpeed, 0f, moveElapsed / moveDuration);
            
            yield return null;
        }
        
        isMoving = false;
    }
    
    /// <summary>
    /// 自己被吃掉了
    /// </summary>
    /// <param name="playerBall"></param>
    public void BeEaten(BallController playerBall)
    {
        float missFactor = 0.3f;
        if (isFromPlayer)
        {
            missFactor = 1f;
        }
        else
        {
            // 自然生成的食物被吃了，生成新的，维持量在100
            EventCenter.Instance.EventTrigger<string>(GameEvent.食物移除, _foodId);
        }
        playerBall.AddMass(this.Mass * missFactor);
        
        Destroy(gameObject);
    }
}
