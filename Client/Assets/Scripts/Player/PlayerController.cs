using System.Collections;
using System.Collections.Generic;
using PrimeTweenDemo;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [HideInInspector] public Transform fatherObj;
    [SerializeField] private GameObject ballPrefab;     //自己分裂出来的球
    List<BallController> balls = new List<BallController>();

    public PlayerInput playerInput;
    private Vector2 moveInput;
    private Vector3 lastInput;
    
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

        // 更新玩家视野大小
        UpdateCameraFOV();
    }

    private void OnDisable()
    {
        if (playerInput != null)
        {
            playerInput.PlayerActions.Disable();
        }
    }

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

    /// <summary>
    /// 更新摄像机FOV
    /// </summary>
    private void UpdateCameraFOV()
    {
        if (balls.Count == 0)
        {
            CameraController.Instance.SetTargetFOV(5f);
            return;
        }

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (var ball in balls)
        {
            Vector2 pos = ball.transform.position;
            float radius = ball.Mass;   // localScale = (Mass, Mass, 1)
            
            minX = Mathf.Min(minX, pos.x - radius);
            maxX = Mathf.Max(maxX, pos.x + radius);
            minY = Mathf.Min(minY, pos.y - radius);
            maxY = Mathf.Max(maxY, pos.y + radius);
        }
        
        // 构成的包围盒的范围
        float width = maxX - minX;
        float height = maxY - minY;
        
        // 目标视野大小
        float targetSize = Mathf.Max(width, height) / 2 * 3f;
        targetSize = Mathf.Max(targetSize, 5f);
        
        // 更新FOV
        CameraController.Instance.SetTargetFOV(targetSize);
    }
    #endregion
    
    /// <summary>
    /// 生成新的球
    /// </summary>
    /// <param name="position"></param>
    private BallController CreateBall(Vector3 position)
    {
        GameObject ballObj = Instantiate(ballPrefab, position, Quaternion.identity, fatherObj);
        BallController ball = ballObj.GetComponent<BallController>();
        ball.ownerPlayer = this;
        balls.Add(ball);
        
        return ball;
    }
}
