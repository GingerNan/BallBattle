using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

public class BallController : MonoBehaviour, IEatable
{
    private string _ballId;
    
    public PlayerController ownerPlayer;
    [SerializeField] private float mass = 1f;
    [SerializeField] private float baseSpeed = 5f;
    
    [SerializeField] private GameObject foodPrefab;
    
    private Rigidbody2D rb;

    // 吐球相关变量
    private bool canVomit = true;
    private float vomitCooldown = 0.2f;
    private float lastVomitTime = -999f;
    
    // 分裂相关变量
    private bool canSplit = true;
    private float SplitCooldown = 0.2f;
    private float lastSplitTime = -999f;
    
    public float Mass
    {
        get => mass;
        set
        { 
            mass = value;
            EventCenter.Instance.EventTrigger(GameEvent.视野变化);
        } 
    }
    
    #region 生命周期
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        _ballId = Guid.NewGuid().ToString();
    }

    private void FixedUpdate()
    {
        // 检测是否能吃食物
        CheckCanEat();

        // 更新吐球间隔
        UpdateVomitCooldown();

        // 更新分裂间隔
        UpdateSplitCooldown();
    }
    #endregion
    
    private void CheckCanEat()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            transform.position,
            transform.localScale.x / 2,
            LayerMask.GetMask("CanBeEat")
            );

        foreach (var collider in colliders)
        {
            //Debug.Log(collider.gameObject.name);
            if (collider == null || collider.gameObject == this.gameObject)
            {
                continue;
            }
            
            IEatable eatable = collider.GetComponent<IEatable>();
            if (eatable == null) continue;

            if (this.Mass > eatable.Mass * 1.1f)
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                float myRadius = transform.localScale.x / 2;
                if (distance < myRadius)
                {
                    eatable.BeEaten(this);
                }
            }
        }
    }

    /// <summary>
    /// 生成球时初始化
    /// </summary>
    /// <param name="newMass"></param>
    /// <param name="owner"></param>
    public void InitBall(float newMass, PlayerController owner)
    {
        this.ownerPlayer = owner;
        this.Mass = newMass;
        transform.localScale = new Vector3(newMass, newMass, 1);
    }
    
    /// <summary>
    /// 对该球进行移动
    /// </summary>
    /// <param name="direction"></param>
    public void Move(Vector2 direction)
    {
        // 反比例函数 y = k/x 的参数设置
        float k = 30f;  // 比例系数，可调整，建议20-50之间
        float minMass = 1f;     // 最小质量，避免初零和极端情况
        float maxMass = 30f;    //最大质量，避免速度过小
        
        // 确保质量在合理范围内
        float clampedMass = Mathf.Clamp(Mass, minMass, maxMass);
        
        // 使用反比例函数计算速度因子
        float speedFactor = k / clampedMass * 0.1f;
        
        // 可选：添加最小速度限制，避免质量过大时速过小
        float minSpeedFactor = 0.1f;    // 最小速度系数
        speedFactor = Mathf.Max(speedFactor, minSpeedFactor);

        float currentSpeed = baseSpeed * speedFactor;
        rb.velocity = direction * currentSpeed;
    }

    #region 分裂

    private void UpdateSplitCooldown()
    {
        if (!canSplit && Time.time > lastSplitTime + SplitCooldown)
        {
            canSplit = true;
        }
    }
    
    public void Split()
    {
        if (!CanSplit()) return;
        
        // 设置分裂状态
        canSplit = false;
        lastSplitTime = Time.time;

        float splitMass = Mass / 2;
        Mass = splitMass;
        Tween.Scale(transform, new Vector3(Mass, Mass, 1), 0.3f);
        
        // 创建新球
        CreateSplitBall(splitMass);
    }

    public void CreateSplitBall(float splitMass)
    {
        // 分裂方向                      
        Vector2 splitDirection = ownerPlayer.GetLastMoveInput();
        
        // 分裂出来的球的位置
        Vector2 newBallPosition = (Vector2)transform.position + splitDirection * (transform.localScale.x / 2 + 0.75f);

        // 新的球
        BallController newBall = ownerPlayer.CreateBall(newBallPosition);
        
        // 设置球的属性
        newBall.InitBall(splitMass, ownerPlayer);
        
        // 给球方向的推力 施加速度
        newBall.ApplySplitForce(splitDirection);
        // 给自己一个反方向的推力 施加速度
        this.ApplySplitForce(splitDirection);
        
        ownerPlayer.AddBall(newBall);
    }

    private void ApplySplitForce(Vector2 direction)
    {
        // 分裂力度
        float splitForce = Mathf.Clamp(8f / Mass, 2f, 8f);
        rb.AddForce(direction * splitForce, ForceMode2D.Impulse);
    }
    
    private bool CanSplit()
    {
        // 在冷却中 不可分裂
        if (!canSplit) return false;
        
        // 最小分裂质量
        if (Mass < 2f) return false;
        
        if (ownerPlayer == null) return false;

        return true;
    }

    #endregion


    #region 吐球

    private void UpdateVomitCooldown()
    {
        if (!canVomit && Time.time - lastVomitTime >= vomitCooldown)
        {
            canVomit = true;
        }
    }
    
    public void Vomit()
    {
        if (!CanVomit()) return;
        
        // 设置吐球状态
        canVomit = false;
        lastVomitTime = Time.time;

        // 吐出来的质量
        float vomitMass = Mathf.Max(Mass * 0.1f, 0.3f);
        
        // 原球质量减少
        Mass -= vomitMass;
        Tween.Scale(transform, new Vector3(Mass, Mass, 1), 0.2f);

        // 创建食物球
        CreateFoodBall(vomitMass);
    }

    private bool CanVomit()
    {
        // 吐球冷却中
        if (!canVomit) return false;

        // 能够吐球的最小质量
        if (Mass < 1.5f) return false;

        if (ownerPlayer == null) return false;
        
        return true;
    }

    #endregion

    private void CreateFoodBall(float vomitMass)
    {
        // 获取吐球方向
        Vector2 vomitDirection = ownerPlayer.GetLastMoveInput();
        
        // 食物球生成位置 边缘生成
        Vector2 foodPosition = (Vector2)transform.position + (transform.localScale.x / 2 + 1f) * vomitDirection;
        
        // 实例化吐出来球
        GameObject foodObj = Instantiate(
            foodPrefab,
            foodPosition,
            Quaternion.identity,
            FoodSpawner.Instance.foodParent.transform
            );
        
        FoodBall foodBall = foodObj.GetComponent<FoodBall>();
        foodBall.isFromPlayer = true;
        foodBall.SetMass(vomitMass);
        foodBall.InitMovement(vomitDirection, 10 * vomitMass, 1f);
    }
    
    public void BeEaten(BallController playerBall)
    {
        if (playerBall != null)
        {
            playerBall.AddMass(Mass);
        }

        if (ownerPlayer != null)
        {
            ownerPlayer.DecreaseBall(this);
        }
        
        Destroy(gameObject);
    }

    /// <summary>
    /// 增加该球质量
    /// </summary>
    /// <param name="addMass"></param>
    public void AddMass(float addMass)
    {
        Mass += addMass;
        Vector3 targetScale = new Vector3(Mass, Mass, 1);
        Tween.Scale(transform, targetScale, 0.5f);
    }

    public string GetBallId()
    {
        return _ballId;
    }
}
