using Mirror;
using System;
using UnityEngine;

public class LoveLetterNetworkPlayer : NetworkBehaviour
{


    [SyncVar] public int PlayerIndex = -1;
    [SyncVar] public string PlayerName = "Joueur";

    private LoveLetterRoomManager networkManager;
    private ServerGameController serverGameController;

    public bool IsInitialized => PlayerIndex >= 0;

    public event Action<ClientGameSnapshot> OnSnapshotReceived;
    public event Action<string> OnServerMessageReceived;
    public event Action<LoveLetterNetworkEvent> OnGameEventReceived;


    [Server]
    public void SetPlayerName(string value)
    {
        PlayerName = value;
    }

    [Server]
    public void SetPlayerIndex(int value)
    {
        PlayerIndex = value;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"GamePlayer spawn => {PlayerName} / index {PlayerIndex}");
    }

   
    [Server]
    public void ServerInitialize(
        LoveLetterRoomManager manager,
        ServerGameController controller,
        int playerIndex)
    {
        networkManager = manager;
        serverGameController = controller;

        PlayerIndex = playerIndex;
        PlayerName = $"Joueur {playerIndex + 1}";
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Debug.Log($"NetworkPlayer local démarré. Index courant = {PlayerIndex}");
    }

    [Command]
    public void CmdRequestStartGame()
    {
        EnsureServerRefs();
        if (serverGameController == null)
            return;

        serverGameController.HandleStartGameRequest(this);
    }

    [Command]
    public void CmdPlayCard(int cardValue)
    {
        EnsureServerRefs();
        if (serverGameController == null)
            return;

        serverGameController.HandlePlayCard(this, (CardType)cardValue);
    }

    [Command]
    public void CmdSelectTarget(int targetIndex)
    {
        EnsureServerRefs();
        if (serverGameController == null)
            return;

        serverGameController.HandleSelectTarget(this, targetIndex);
    }

    [Command]
    public void CmdSelectGuardGuess(int guessedCardValue)
    {
        EnsureServerRefs();
        if (serverGameController == null)
            return;

        serverGameController.HandleSelectGuardGuess(this, (CardType)guessedCardValue);
    }

    [Command]
    public void CmdRequestRestartRound()
    {
        EnsureServerRefs();
        if (serverGameController == null)
            return;

        serverGameController.HandleRestartRound(this);
    }

    [TargetRpc]
    public void TargetReceiveSnapshot(NetworkConnection target, ClientGameSnapshot snapshot)
    {
        OnSnapshotReceived?.Invoke(snapshot);
    }

    [TargetRpc]
    public void TargetReceiveServerMessage(NetworkConnection target, string message)
    {
        Debug.Log(message);
        OnServerMessageReceived?.Invoke(message);
    }

    [TargetRpc]
    public void TargetReceiveGameEvent(NetworkConnection target, LoveLetterNetworkEvent gameEvent)
    {
        OnGameEventReceived?.Invoke(gameEvent);
    }

    private void EnsureServerRefs()
    {
        if (serverGameController == null)
            serverGameController = FindFirstObjectByType<ServerGameController>();

        if (networkManager == null)
            networkManager = FindFirstObjectByType<LoveLetterRoomManager>();
    }

    [Serializable]
    public class ClientGameSnapshot
    {
        public int localPlayerIndex;
        public int currentTurnIndex;
        public bool roundFinished;
        public int remainingDeckCount;

        public PendingActionType pendingActionType;
        public CardType pendingSourceCard;
        public int pendingCasterIndex;
        public int pendingSelectedTargetIndex;

        public PlayerPublicView[] players;
        public int[] localHand;
        public int[] visibleBurnedCards;
        public string[] logs;

        public bool canStartGame;
        public bool isHostPlayer;
        public int connectedPlayerCount;
    }

    [Serializable]
    public class PlayerPublicView
    {
        public int playerIndex;
        public string playerName;
        public int handCount;
        public int[] discard;
        public bool isProtected;
        public bool isEliminated;
        public bool isGhost;
        public int score;
    }
}