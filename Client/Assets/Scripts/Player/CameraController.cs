using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoSingleton<CameraController>
{
    [SerializeField] private Transform followTarget;
    [SerializeField] private float smoothTime_Follow = 3f;
    [SerializeField] private float smoothTime_View = 3f;
    
    private float targetFOV = 5f;
    private float currentFOV;
    private Camera cam;

    protected override void Awake()
    {
        base.Awake();
        cam = GetComponent<Camera>();
        currentFOV = cam.orthographicSize;
    }

    private void Start()
    {
        EventCenter.Instance.AddEventListener(GameEvent.视野变化, UpdateCameraFOV);
    }
    
    private void LateUpdate()
    {
        if (!followTarget) return;
        
        // 线性追随目标
        Vector3 targetPosition = new  Vector3(followTarget.position.x, followTarget.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothTime_Follow);
        
        // 线性变化FOV
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * smoothTime_View);
        cam.orthographicSize = currentFOV;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventCenter.Instance.RemoveEventListener(GameEvent.视野变化, UpdateCameraFOV);
    }

    /// <summary>
    /// 设置跟随目标
    /// </summary>
    /// <param name="target"></param>
    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    /// <summary>
    /// 更新摄像机FOV
    /// </summary>
    private void UpdateCameraFOV()
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (var ball in PlayerController.Instance.balls)
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
        targetFOV = targetSize;
    }
}
