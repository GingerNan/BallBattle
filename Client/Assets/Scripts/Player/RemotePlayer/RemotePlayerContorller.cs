using System.Collections;
using System.Collections.Generic;
using Server;
using UnityEngine;
using UnityEngine.tvOS;
using Vector2 = UnityEngine.Vector2;

public class RemotePlayerContorller : MonoBehaviour
{
    public string PlayerId {get;set;}
    
    [HideInInspector] public Transform fatherObj;
    [SerializeField] private GameObject ballPrefab;     //自己分裂出来的球
    private List<RemoteBall> balls = new List<RemoteBall>();
    private Dictionary<string, RemoteBall> ballDict = new Dictionary<string, RemoteBall>();

    // 记录吃掉的球 防止重复创建
    private HashSet<string> eatenBalls = new HashSet<string>();
    
    private void Start()
    {
        if (balls.Count == 0)
        {
            CreateBall(transform.position, "initial_" + PlayerId);
        }
    }
    
    // 生成新的球
    public RemoteBall CreateBall(Vector2 position, string ballId)
    {
        GameObject ballObj = Instantiate(ballPrefab, position, Quaternion.identity, fatherObj);
        RemoteBall newBall = ballObj.GetComponent<RemoteBall>();
        newBall.SetBallId(ballId);
        newBall.SetPlayerId(PlayerId);
        balls.Add(newBall);
        
        ballDict[ballId] = newBall;
        
        return newBall;
    }

    // 更新玩家位置
    public void UpdatePosition(Vector3 position)
    {
        fatherObj.transform.position = position;
    }

    // 更新球的状态
    public void UpdateBalls(List<BallData> ballData)
    {
        if (ballData == null) return;
        
        // 过滤被吃掉的球（无效的球）
        List<BallData> validBallData = new List<BallData>();
        foreach (var ball in ballData)
        {
            if (ball.BallId == null) continue;

            if (!eatenBalls.Contains(ball.BallId))
            {
                validBallData.Add(ball);
            }
        }
        
        // 创建球 或 更新球的位置
        foreach (var data in validBallData)
        {
            if (ballDict.TryGetValue(data.BallId, out RemoteBall existingBall))
            {
                // 更新现有球
                existingBall.UpdatePosition(new Vector3(data.Position.X, data.Position.Y, 0));
                existingBall.SetMass(data.Mass);
            }
            else
            {
                // 创建新球
                RemoteBall newBall = CreateBall(new UnityEngine.Vector2(data.Position.X, data.Position.Y), data.BallId);
                if (newBall != null)
                {
                    newBall.Mass = data.Mass;
                }
            }
        }
        
        // 添加不存在的球到待删除链表
        List<string> ballsToRemove = new List<string>();
        foreach (var ballId in ballDict.Keys)
        {
            bool found = false;
            foreach (var data in validBallData)
            {
                if (ballId == data.BallId)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                ballsToRemove.Add(ballId);
            }
        }

        // 删除不存在的球
        foreach (var ballId in ballsToRemove)
        {
            if (ballDict.TryGetValue(ballId, out RemoteBall ballToRemove))
            {
                balls.Remove(ballToRemove);
                ballDict.Remove(ballId);
                Destroy(ballToRemove.gameObject);
            }
        }
    }
}
