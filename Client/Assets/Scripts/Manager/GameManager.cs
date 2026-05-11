using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
	[SerializeField] private GameObject playerPrefab;

	public void Start()
	{
		CreateNewPlayer();
	}
	
	public void CreateNewPlayer()
	{
		// 玩家所有物体的父物体
		GameObject father = new GameObject("Father");
		// 随机玩家位置
		(float xPos, float yPos) = Utility.Instance.GetRandomPositionInMap();
		GameObject playerObj = Instantiate(playerPrefab, new Vector3(xPos, yPos, 0), Quaternion.identity,father.transform);
		// 拿到玩家控制器
		PlayerController player = playerObj.GetComponent<PlayerController>();
		player.fatherObj = father.transform;
		
		CameraController.Instance.SetFollowTarget(playerObj.transform);
	}
}