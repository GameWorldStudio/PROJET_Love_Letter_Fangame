using System;
using System.Linq;
using UnityEngine;

public class NetworkGamePresenter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private LoveLetterNetworkPlayer localPlayer;

    private LoveLetterNetworkPlayer.ClientGameSnapshot currentSnapshot;

    public event Action<LoveLetterNetworkPlayer.ClientGameSnapshot> OnSnapshotUpdated;
    public event Action<LoveLetterNetworkEvent> OnGameEventReceived;

    private void Start()
    {
        if (localPlayer == null)
        {
            Debug.LogError("NetworkGamePresenter : localPlayer non assigné.");
            return;
        }

        localPlayer.OnSnapshotReceived += HandleSnapshot;
        localPlayer.OnGameEventReceived += HandleGameEvent;
    }

    private void OnDestroy()
    {
        if (localPlayer != null)
        {
            localPlayer.OnSnapshotReceived -= HandleSnapshot;
            localPlayer.OnGameEventReceived -= HandleGameEvent;
        }
    }

    private void HandleSnapshot(LoveLetterNetworkPlayer.ClientGameSnapshot snapshot)
    {
        currentSnapshot = snapshot;
        OnSnapshotUpdated?.Invoke(snapshot);
    }

    private void HandleGameEvent(LoveLetterNetworkEvent gameEvent)
    {
        DebugLogEvent(gameEvent);
        OnGameEventReceived?.Invoke(gameEvent);
    }

    public void PlayCard(CardType card)
    {
        if (!IsMyTurn())
            return;

        localPlayer.CmdPlayCard((int)card);
    }

    public void SelectTarget(int targetIndex)
    {
        if (!IsMyTurn())
            return;

        localPlayer.CmdSelectTarget(targetIndex);
    }

    public void SelectGuardGuess(CardType card)
    {
        if (!IsMyTurn())
            return;

        localPlayer.CmdSelectGuardGuess((int)card);
    }

    public void RestartRound()
    {
        localPlayer.CmdRequestRestartRound();
    }

    public bool IsMyTurn()
    {
        if (currentSnapshot == null)
            return false;

        return currentSnapshot.localPlayerIndex == currentSnapshot.currentTurnIndex;
    }

    public bool IsWaitingForTarget()
    {
        return currentSnapshot != null &&
               currentSnapshot.pendingActionType == PendingActionType.SelectTarget &&
               currentSnapshot.pendingCasterIndex == currentSnapshot.localPlayerIndex;
    }

    public bool IsWaitingForGuardGuess()
    {
        return currentSnapshot != null &&
               currentSnapshot.pendingActionType == PendingActionType.SelectGuardGuess &&
               currentSnapshot.pendingCasterIndex == currentSnapshot.localPlayerIndex;
    }

    public CardType[] GetLocalHand()
    {
        if (currentSnapshot?.localHand == null)
            return Array.Empty<CardType>();

        return currentSnapshot.localHand.Select(v => (CardType)v).ToArray();
    }

    public LoveLetterNetworkPlayer.PlayerPublicView[] GetPlayers()
    {
        return currentSnapshot?.players ?? Array.Empty<LoveLetterNetworkPlayer.PlayerPublicView>();
    }

    public string[] GetLogs()
    {
        return currentSnapshot?.logs ?? Array.Empty<string>();
    }

    private void DebugLogEvent(LoveLetterNetworkEvent gameEvent)
    {
        switch (gameEvent.EventType)
        {
            case LoveLetterNetworkEventType.CardPlayed:
                Debug.Log($"[EVENT] Joueur {gameEvent.PlayerIndex} a joué {(CardType)gameEvent.CardValue}");
                break;

            case LoveLetterNetworkEventType.PlayerEliminated:
                Debug.Log($"[EVENT] Joueur {gameEvent.PlayerIndex} éliminé");
                break;

            case LoveLetterNetworkEventType.CardsRevealed:
                Debug.Log($"[EVENT] Reveal de {gameEvent.CardValues.Length} carte(s)");
                break;

            case LoveLetterNetworkEventType.RoundStarted:
                Debug.Log("[EVENT] Manche démarrée");
                break;

            case LoveLetterNetworkEventType.RoundEnded:
                Debug.Log("[EVENT] Manche terminée");
                break;
        }
    }

    public void RequestStartGame()
    {
        if (localPlayer == null)
            return;

        localPlayer.CmdRequestStartGame();
    }
}