using System.Collections;
using System.Collections.Generic;
using PrimeTweenDemo;
using UnityEngine;

public class PlayerController : MonoSingleton<PlayerController>
{
    [HideInInspector] public Transform fatherObj;
    [SerializeField] private GameObject ballPrefab;     //自己分裂出来的球
    public List<BallController> balls = new List<BallController>();

    public PlayerInput playerInput;
    private Vector2 moveInput;
    private Vector3 lastInput;
    
    // 网络相关
    public string PlayerId { get; set; }
    public bool IsLocalPlayer { get; set; }

    private float positionSendTimer = 0f;
    private const float POSITION_SEND_INTERVAL = 0.1f;  // 每100毫秒同步一次位置
    
    private void OnEnable()
    {
        if (playerInput == null)
        {
            playerInput = new PlayerInput();
        }
        
        playerInput.PlayerActions.Enable();
        playerInput.PlayerActions.Move.performed += ctx =>
        {
            moveInput = ctx.ReadValue<Vector2>();
            lastInput = moveInput;
        };
        
        playerInput.PlayerActions.Move.canceled += input => moveInput = Vector2.zero;
        playerInput.PlayerActions.Split.performed += input => SplitAllBalls();
        playerInput.PlayerActions.Vomit.performed += input => VomitBall();
    }
    
    private void Start()
    {
        if (balls.Count == 0)
        {
            CreateBall(transform.position);
        }
    }

    private void Update()
    {
        // 同时控制所有球各自移动
        foreach (var ball in balls)
        {
            ball.Move(moveInput.normalized);
        }

        UpdatePlayerPosition();

        // 定时发送位置给服务器
        positionSendTimer += Time.deltaTime;
        if (positionSendTimer >= POSITION_SEND_INTERVAL)
        {
            positionSendTimer = 0;
            SendStateToServer();
        }
    }

    private void OnDisable()
    {
        if (playerInput != null)
        {
            playerInput.PlayerActions.Disable();
        }
    }

    #region 分裂

    private void SplitAllBalls()
    {
        for (int i = balls.Count - 1; i >= 0; i--)
        {
            balls[i].Split();
        }
    }
    
    #endregion
    
    #region 吐球

    private void VomitBall()
    {
        for (int i = balls.Count - 1; i >= 0; i--)
        {
            balls[i].Vomit();
        }
    }
    
    #endregion
    
    #region 摄像机追踪
    
    /// <summary>
    /// 更新玩家的相对位置
    /// </summary>
    private void UpdatePlayerPosition()
    {
        if (balls.Count == 0) return;

        Vector2 center = Vector2.zero;
        foreach (var ball in balls)
        {
            center += (Vector2)ball.transform.position;
        }
        
        center /= balls.Count;
        transform.position = center;
    }
    
    #endregion

    private void SendStateToServer()
    {
        Vector2 postion = new Vector2(transform.position.x, transform.position.y);
        NetworkManager.Instance.SendPlayerPosition(postion, balls);
    }
    
    /// <summary>
    /// 生成新的球
    /// </summary>
    /// <param name="position"></param>
    public BallController CreateBall(Vector2 position)
    {
        GameObject ballObj = Instantiate(ballPrefab, position, Quaternion.identity, fatherObj);
        BallController newBall = ballObj.GetComponent<BallController>();
        
        newBall.InitBall(1, this);
        balls.Add(newBall);
        
        return newBall;
    }

    /// <summary>
    /// 向控制链表添加球
    /// </summary>
    /// <param name="ball"></param>
    public void AddBall(BallController ball)
    {
        if (!balls.Contains(ball))
        {
            balls.Add(ball);
            ball.ownerPlayer = this;
        }
    }

    /// <summary>
    /// 向控制链表减少球
    /// </summary>
    /// <param name="ball"></param>
    public void DecreaseBall(BallController ball)
    {
        if (balls.Contains(ball))
        {
            balls.Remove(ball);
        }
    }
    
    public Vector2 GetLastMoveInput()
    {
        return lastInput.normalized;
    }
}
