using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

public class BallController : MonoBehaviour, IEatable
{
    public PlayerController ownerPlayer;
    [SerializeField] private float mass = 1f;
    [SerializeField] private float baseSpeed = 5f;
    private Rigidbody2D rb;

    public float Mass
    {
        get => mass;
        set => mass = value;
    }
    
    #region 生命周期
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        CheckCanEat();
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
    
    public void BeEaten(BallController playerBall)
    {
        
    }

    public void AddMass(float addMass)
    {
        Mass += addMass * 0.3f;
        Vector3 targetScale = new Vector3(Mass, Mass, 1);
        Tween.Scale(transform, targetScale, 0.5f);
    }
}
