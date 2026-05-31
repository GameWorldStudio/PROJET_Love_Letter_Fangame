using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class LoveLetterRoomManager : NetworkRoomManager
{
    public static LoveLetterRoomManager Instance { get; private set; }

    [Header("Lobby Settings")]
    [SyncVar] public string roomDisplayName = "Salon LAN";
    [SyncVar] public int roomPlayerLimit = 2;

    [Header("Runtime")]
    [SerializeField] private bool gameStarted = false;

    public readonly List<LoveLetterRoomPlayer> RoomPlayers = new();

    public override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    public override void Start()
    {
        base.Start();

        // sécurité : le maxConnections du manager suit la valeur de roomPlayerLimit
        maxConnections = Mathf.Max(2, roomPlayerLimit);
    }

    public void ConfigureRoom(string roomName, int playerLimit)
    {
        roomDisplayName = string.IsNullOrWhiteSpace(roomName) ? "Salon LAN" : roomName;
        roomPlayerLimit = Mathf.Max(2, playerLimit);
        maxConnections = roomPlayerLimit;
    }

    public override void OnRoomStartHost()
    {
        base.OnRoomStartHost();
        gameStarted = false;
        RoomPlayers.Clear();
    }

    public override void OnRoomStopHost()
    {
        base.OnRoomStopHost();
        gameStarted = false;
        RoomPlayers.Clear();
    }

    public override void OnRoomServerConnect(NetworkConnectionToClient conn)
    {
        // si la partie a démarré, on bloque les nouveaux entrants
        if (gameStarted)
        {
            conn.Disconnect();
            return;
        }

        // si la room est pleine, on bloque aussi
        if (numPlayers >= maxConnections)
        {
            conn.Disconnect();
            return;
        }

        base.OnRoomServerConnect(conn);
    }

    public override void OnRoomServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn != null && conn.identity != null)
        {
            LoveLetterRoomPlayer player = conn.identity.GetComponent<LoveLetterRoomPlayer>();
            if (player != null)
            {
                RoomPlayers.Remove(player);
            }
        }

        base.OnRoomServerDisconnect(conn);
    }

    public override void OnRoomServerAddPlayer(NetworkConnectionToClient conn)
    {
        // utilise le flow standard NetworkRoomManager
        base.OnRoomServerAddPlayer(conn);
    }

    public override GameObject OnRoomServerCreateRoomPlayer(NetworkConnectionToClient conn)
    {
        NetworkRoomPlayer roomPlayerPrefab1 = roomPlayerPrefab;
        GameObject roomPlayer = Instantiate(roomPlayerPrefab1.gameObject);

        LoveLetterRoomPlayer player = roomPlayer.GetComponent<LoveLetterRoomPlayer>();
        if (player != null)
        {
            player.SetDisplayName(MenuNetworkData.PlayerName);
        }

        return roomPlayer;
    }

    public override void OnRoomServerSceneChanged(string sceneName)
    {
        base.OnRoomServerSceneChanged(sceneName);
    }

    public override void OnRoomServerPlayersReady()
    {
        // on ne veut PAS d'auto start
        // le host lancera via bouton
    }

    public override bool OnRoomServerSceneLoadedForPlayer(
        NetworkConnectionToClient conn,
        GameObject roomPlayer,
        GameObject gamePlayer)
    {
        LoveLetterRoomPlayer room = roomPlayer.GetComponent<LoveLetterRoomPlayer>();
        LoveLetterNetworkPlayer game = gamePlayer.GetComponent<LoveLetterNetworkPlayer>();

        if (room != null && game != null)
        {
            game.SetPlayerName(room.DisplayName);
            game.SetPlayerIndex(room.index);
        }

        return true;
    }

    [Server]
    public void StartMatch()
    {
        if (gameStarted)
            return;

        if (numPlayers < 2)
        {
            Debug.LogWarning("Impossible de démarrer : il faut au moins 2 joueurs.");
            return;
        }

        gameStarted = true;

        // Change automatiquement vers gameplayScene
        ServerChangeScene(GameplayScene);
    }

    public bool CanStartMatch()
    {
        return NetworkServer.active && !gameStarted && numPlayers >= 2;
    }
}