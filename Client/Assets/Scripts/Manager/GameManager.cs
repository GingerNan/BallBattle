using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
	[SerializeField] private GameObject playerPrefab;
	[SerializeField] private GameObject remotePlayerPrefab;
	Dictionary<string, PlayerController> players = new Dictionary<string, PlayerController>();
	Dictionary<string, RemotePlayerContorller> remotePlayers = new Dictionary<string, RemotePlayerContorller>();

	protected override void Awake()
	{
		base.Awake();
		
		Application.targetFrameRate = 60;
		Application.runInBackground = true;
	}

	public void Start()
	{
		EventCenter.Instance.AddEventListener<PlayerPositionData>(GameEvent.玩家位置更新, OnPlayerPositionUpdate);
		EventCenter.Instance.AddEventListener<List<PlayerPositionData>>(GameEvent.同步玩家位置, OnSyncAllPositions);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		EventCenter.Instance.RemoveEventListener<PlayerPositionData>(GameEvent.玩家位置更新, OnPlayerPositionUpdate);
		EventCenter.Instance.RemoveEventListener<List<PlayerPositionData>>(GameEvent.同步玩家位置, OnSyncAllPositions);
	}
	
	public void CreateNewPlayer(string playerId, bool isLocalPlayer = false)
	{
		if (players.ContainsKey(playerId))
		{
			Console.WriteLine($"玩家 {playerId} 已存在");
			return;
		}
		
		// 本地玩家
		if (isLocalPlayer)
		{
			// 玩家所有物体的父物体
			GameObject father = new GameObject("Player_" + playerId);
			// 随机玩家位置
			(float xPos, float yPos) = Utility.Instance.GetRandomPositionInMap();
			GameObject playerObj = Instantiate(playerPrefab, new Vector3(xPos, yPos, 0), Quaternion.identity,father.transform);
			// 拿到玩家控制器
			PlayerController player = playerObj.GetComponent<PlayerController>();
			player.fatherObj = father.transform;
			player.PlayerId = playerId;
			player.IsLocalPlayer = true;
			players[playerId] = player;
			
			CameraController.Instance.SetFollowTarget(playerObj.transform);
		}
		else
		{
			// 玩家所有物体的父物体
			GameObject father = new GameObject("RemotePlayerFather_" + playerId);
			GameObject remotePlayerObj = Instantiate(remotePlayerPrefab, father.transform);
			RemotePlayerContorller remotePlayer = remotePlayerObj.GetComponent<RemotePlayerContorller>();
			remotePlayer.fatherObj = father.transform;
			remotePlayer.PlayerId = playerId;
			remotePlayers[playerId] = remotePlayer;
		}
	}

	// 同步所有玩家
	private void OnSyncAllPositions(List<PlayerPositionData> playerPositions)
	{
		foreach (var positionData in playerPositions)
		{
			if (positionData.PlayerId == NetworkManager.Instance.GetPlayerId())
				continue;
			
			UpdateRemotePlayerState(positionData);
		}
	}
	
	// 更新单个玩家
	private void OnPlayerPositionUpdate(PlayerPositionData positionData)
	{
		if (positionData.PlayerId == NetworkManager.Instance.GetPlayerId())
			return;

		UpdateRemotePlayerState(positionData);
	}

	// 更新单个玩家位置状态
	private void UpdateRemotePlayerState(PlayerPositionData positionData)
	{
		if (!remotePlayers.ContainsKey(positionData.PlayerId))
		{
			// 如果不存在 则创建
			CreateNewPlayer(positionData.PlayerId);
		}

		if (remotePlayers.TryGetValue(positionData.PlayerId, out RemotePlayerContorller remotePlayer))
		{
			// 更新位置
			Vector3 position = new Vector3(positionData.Position.X, positionData.Position.Y, 0);
			remotePlayer.UpdatePosition(position);
			
			// 更新每个球
			remotePlayer.UpdateBalls(positionData.Balls);
		}
	}
}