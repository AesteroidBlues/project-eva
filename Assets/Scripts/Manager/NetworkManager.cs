﻿using UnityEngine;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour {

	public GameObject playerPrefab;

	// NETWORK CONSTANTS
	const int DEFAULT_PORT = 31337;
	const int MAX_CONNECTIONS = 16;

	private struct Player 
	{
		public GameObject avatar;
		public NetworkPlayer playerInfo;
	}
	private List<Player> otherPlayers;
	private Player my;

	private GameManager gameManager;

	void Start() {
		DontDestroyOnLoad( this );
		otherPlayers = new List<Player>();
		gameManager = GameObject.FindGameObjectWithTag( Tags.GameController ).GetComponent<GameManager>();
	}

	public static void StartServer() {
		bool useNAT = !Network.HavePublicAddress();
		Network.InitializeServer( MAX_CONNECTIONS, DEFAULT_PORT, useNAT );
	}

	public static void ConnectToServer(string ip) {
		Network.Connect( ip, DEFAULT_PORT );
	}

	public static void DisconnectFromServer() {
		Network.Disconnect();
	}

	public void EnterGame() {
		GameObject myAvatar = gameManager.SpawnPlayer();
		my = new Player();
		my.avatar = myAvatar;
		my.playerInfo = Network.player;

		gameManager.AssignCamera( myAvatar );

		// Tell other players we've connected
		networkView.RPC( "GetNewPlayerState", RPCMode.Others, my.playerInfo, myAvatar.networkView.viewID, myAvatar.transform.position, myAvatar.transform.rotation );

		// Request each other player's state at the time of connection
		networkView.RPC( "RequestIntialPlayerState", RPCMode.OthersBuffered, Network.player );
	}

	/////////////////////////
	//	EVENTS
	/////////////////////////

	// Called when the server goes up
	void OnServerInitialized() {
		Debug.Log( "Server Intitalized" );
		Application.LoadLevel( 2 );
	}

	// Caled when a player connects (server side)
	void PlayerConnected( NetworkPlayer playerInfo ) {
		
	}

	// Called when the player connects (client side)
	void OnConnectedToServer() {
		Application.LoadLevel( 2 );
	}

	// Called when a player disconnects (server side)
	void OnPlayerDisconnected( NetworkPlayer player ) {
		NetworkViewID id = otherPlayers.Find( ( x => x.playerInfo == player ) ).avatar.networkView.viewID;
		Network.RemoveRPCs( player );
		networkView.RPC( "RemoveObject", RPCMode.Others, id );
	}

	// Called when we disconnect (client side)
	void OnDisconnectedFromServer( NetworkDisconnection info ) {
		if ( info == NetworkDisconnection.LostConnection ) {
		}
		otherPlayers.Clear();
		Application.LoadLevel( Levels.Main );
	}

	void OnLevelWasLoaded( int levelID ) {
		if ( gameManager.IsGameplayLevel( levelID ) ) {
			gameManager.CollectCurrentLevelSpawns();
			EnterGame();
		}
	}

	/////////////////////////
	//	RPCS
	/////////////////////////
	[RPC]
	void RequestIntialPlayerState( NetworkPlayer requester ) {
		networkView.RPC( "GetCurrentPlayerState", requester, my.playerInfo, my.avatar.networkView.viewID, my.avatar.transform.position, my.avatar.transform.rotation );
	}

	[RPC]
	void GetNewPlayerState( NetworkPlayer playerInfo, NetworkViewID avatarID, Vector3 initialPosition, Quaternion initialRotation ) {
		Player newPlayer = new Player();
		newPlayer.playerInfo = playerInfo;
		GameObject playerAvatar = NetworkView.Find( avatarID ).gameObject;
		newPlayer.avatar = playerAvatar;
		otherPlayers.Add( newPlayer );
	}

	[RPC]
	void GetCurrentPlayerState( NetworkPlayer playerInfo, NetworkViewID avatarID, Vector3 initialPosition, Quaternion initialRotation ) {
		Player newPlayer = new Player();
		newPlayer.playerInfo = playerInfo;
		GameObject playerAvatar = gameManager.SpawnPlayer( initialPosition, initialRotation );
		playerAvatar.networkView.viewID = avatarID;
		newPlayer.avatar = playerAvatar;
		otherPlayers.Add( newPlayer );
	}

	[RPC]
	void RemoveObject( NetworkViewID id ) {
		Destroy( NetworkView.Find( id ).gameObject );
		Debug.Log( "Object with NetworkViewId " + id.ToString() + " removed" );
	}
}
