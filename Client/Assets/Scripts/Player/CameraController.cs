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

    /// <summary>
    /// 设置跟随目标
    /// </summary>
    /// <param name="target"></param>
    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    /// <summary>
    /// 设置目标FOV
    /// </summary>
    /// <param name="fov"></param>
    public void SetTargetFOV(float fov)
    {
        targetFOV = fov;
    }
}
