using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

public class BallController : MonoBehaviour, IEatable
{
    public PlayerController ownerPlayer;
    [SerializeField] private float mass = 1f;
    [SerializeField] private float baseSpeed = 5f;
    
    [SerializeField] private GameObject foodPrefab;
    
    private Rigidbody2D rb;

    // 吐球相关变量
    private bool canVomit = true;
    private float vomitCooldown = 0.2f;
    private float lastVomitTime = -999f;
    
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
    }

    private void FixedUpdate()
    {
        // 检测是否能吃食物
        CheckCanEat();

        // 更新吐球间隔
        UpdateVomitCooldown();
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
        float maxMass = 30f;
        float speedFactor = 1 - Mathf.Pow(Mass / maxMass, 2);
        speedFactor = Mathf.Max(0, speedFactor);    // 确保非负
        
        float currentSpeed = baseSpeed * speedFactor;
        rb.velocity = direction * currentSpeed;
    }
    
    public void Split()
    {
        
    }

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
}
