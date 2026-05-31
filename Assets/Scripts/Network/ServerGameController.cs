using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ServerGameController : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private bool twoPlayersMode = true;
    [SerializeField] private int visibleBurnedCardsInTwoPlayers = 3;

    private readonly List<LoveLetterNetworkPlayer> players = new();

    private GameState gameState;
    private GameEngine gameEngine;

    private BotMemoryService botMemoryService;
    private RoundService roundService;
    private EndRoundService endRoundService;
    private TurnService turnService;
    private TargetingService targetingService;
    private CardEffectService cardEffectService;

    public GameState State => gameState;
    public bool HasStartedGame { get; private set; }

    public override void OnStartServer()
    {
        base.OnStartServer();
        BuildServices();
    }

    [Server]
    private void BuildServices()
    {
        botMemoryService = new BotMemoryService();
        roundService = new RoundService(botMemoryService);
        endRoundService = new EndRoundService();
        turnService = new TurnService(roundService, endRoundService);
        targetingService = new TargetingService();
        cardEffectService = new CardEffectService(roundService, botMemoryService);

        gameEngine = new GameEngine(
            roundService,
            turnService,
            targetingService,
            cardEffectService,
            botMemoryService
        );
    }

    [Server]
    public void RegisterPlayer(LoveLetterNetworkPlayer player)
    {
        if (player == null || players.Contains(player))
            return;

        players.Add(player);
        SendServerMessageTo(player, $"Inscrit côté serveur avec l'index {player.PlayerIndex}.");
    }

    [Server]
    public void UnregisterPlayer(LoveLetterNetworkPlayer player)
    {
        if (player == null)
            return;

        players.Remove(player);
    }

    [Server]
    public void TryStartGame()
    {
        if (HasStartedGame || players.Count < 2)
            return;

        InitializeGameState();

        var config = new GameConfig
        {
            TwoPlayersMode = twoPlayersMode,
            VisibleBurnedCardsInTwoPlayers = visibleBurnedCardsInTwoPlayers,
            HumanPlayerName = "Toi",
            GhostPlayerName = "Fantôme"
        };

        GameFlowResult result = gameEngine.StartRound(gameState, config);

        HasStartedGame = true;
        ProcessResult(result);
    }

    [Server]
    private void InitializeGameState()
    {
        gameState = new GameState
        {
            Players = new LocalPlayerState[players.Count]
        };

        for (int i = 0; i < players.Count; i++)
        {
            string playerName = string.IsNullOrWhiteSpace(players[i].PlayerName)
                ? $"Joueur {i + 1}"
                : players[i].PlayerName;

            gameState.Players[i] = new LocalPlayerState(playerName, isGhost: false);
        }
    }

    [Server]
    public void HandlePlayCard(LoveLetterNetworkPlayer sender, CardType card)
    {
        if (!CanAcceptAction(sender))
            return;

        GameFlowResult result = gameEngine.Execute(
            new PlayCardCommand(sender.PlayerIndex, card),
            gameState
        );

        ProcessResult(result);
    }

    [Server]
    public void HandleSelectTarget(LoveLetterNetworkPlayer sender, int targetIndex)
    {
        if (!CanAcceptAction(sender))
            return;

        if (gameState.PendingAction.casterIndex != sender.PlayerIndex)
        {
            SendServerMessageTo(sender, "Ce n'est pas à toi de choisir la cible.");
            return;
        }

        GameFlowResult result = gameEngine.Execute(
            new SelectTargetCommand(targetIndex),
            gameState
        );

        ProcessResult(result);
    }

    [Server]
    public void HandleSelectGuardGuess(LoveLetterNetworkPlayer sender, CardType guessedCard)
    {
        if (!CanAcceptAction(sender))
            return;

        if (gameState.PendingAction.casterIndex != sender.PlayerIndex)
        {
            SendServerMessageTo(sender, "Ce n'est pas à toi de choisir l'annonce de la Garde.");
            return;
        }

        GameFlowResult result = gameEngine.Execute(
            new SelectGuardGuessCommand(guessedCard),
            gameState
        );

        ProcessResult(result);
    }

    [Server]
    public void HandleRestartRound(LoveLetterNetworkPlayer sender)
    {
        if (!HasStartedGame)
            return;

        if (!gameState.RoundFinished)
        {
            SendServerMessageTo(sender, "La manche n'est pas encore terminée.");
            return;
        }

        var config = new GameConfig
        {
            TwoPlayersMode = twoPlayersMode,
            VisibleBurnedCardsInTwoPlayers = visibleBurnedCardsInTwoPlayers,
            HumanPlayerName = "Toi",
            GhostPlayerName = "Fantôme"
        };

        GameFlowResult result = gameEngine.StartRound(gameState, config);
        ProcessResult(result);
    }

    [Server]
    private bool CanAcceptAction(LoveLetterNetworkPlayer sender)
    {
        if (sender == null)
            return false;

        if (!HasStartedGame || gameState == null)
        {
            SendServerMessageTo(sender, "La partie n'est pas encore prête.");
            return false;
        }

        if (sender.PlayerIndex < 0 || sender.PlayerIndex >= gameState.Players.Length)
            return false;

        return true;
    }

    [Server]
    private void ProcessResult(GameFlowResult result)
    {
        if (result == null)
            return;

        DispatchNetworkEvents(result);
        BroadcastSnapshots();

        // Plus tard :
        // if (result.ShouldTriggerBotTurn) { ... }
    }

    [Server]
    private void DispatchNetworkEvents(GameFlowResult result)
    {
        if (result.Events == null || result.Events.Count == 0)
            return;

        foreach (GameEvent gameEvent in result.Events)
        {
            switch (gameEvent)
            {
                case CardPlayedEvent cardPlayed:
                    BroadcastToAll(new LoveLetterNetworkEvent
                    {
                        EventType = LoveLetterNetworkEventType.CardPlayed,
                        PlayerIndex = cardPlayed.PlayerIndex,
                        CardValue = (int)cardPlayed.Card
                    });
                    break;

                case PlayerEliminatedEvent eliminated:
                    BroadcastToAll(new LoveLetterNetworkEvent
                    {
                        EventType = LoveLetterNetworkEventType.PlayerEliminated,
                        PlayerIndex = eliminated.PlayerIndex
                    });
                    break;

                case RoundStartedEvent:
                    BroadcastToAll(new LoveLetterNetworkEvent
                    {
                        EventType = LoveLetterNetworkEventType.RoundStarted
                    });
                    break;

                case RoundEndedEvent:
                    BroadcastToAll(new LoveLetterNetworkEvent
                    {
                        EventType = LoveLetterNetworkEventType.RoundEnded
                    });
                    break;

                case TurnEndedEvent:
                    BroadcastToAll(new LoveLetterNetworkEvent
                    {
                        EventType = LoveLetterNetworkEventType.TurnEnded
                    });
                    break;

                case CardsRevealedEvent revealed:
                    DispatchRevealEvent(revealed);
                    break;
            }
        }
    }

    [Server]
    private void DispatchRevealEvent(CardsRevealedEvent revealed)
    {
        LoveLetterNetworkEvent dto = ConvertRevealEvent(revealed);

        // Cas 1 : fin de manche => reveal public
        if (gameState.RoundFinished)
        {
            BroadcastToAll(dto);
            return;
        }

        // Cas 2 : reveal durant une action
        CardType sourceCard = gameState.PendingAction.sourceCard;
        int casterIndex = gameState.PendingAction.casterIndex;
        int targetIndex = gameState.PendingAction.selectedTargetIndex;

        // Prêtre : seul le caster voit
        if (sourceCard == CardType.Priest)
        {
            SendToPlayerIndex(casterIndex, dto);
            return;
        }

        // Baron : les deux concernés voient
        if (sourceCard == CardType.Baron)
        {
            SendToPlayerIndex(casterIndex, dto);
            if (targetIndex != casterIndex)
                SendToPlayerIndex(targetIndex, dto);
            return;
        }

        // Fallback : public
        BroadcastToAll(dto);
    }

    [Server]
    private LoveLetterNetworkEvent ConvertRevealEvent(CardsRevealedEvent revealed)
    {
        int count = revealed.RevealedCards?.Count ?? 0;

        var playerIndexes = new int[count];
        var cardValues = new int[count];

        for (int i = 0; i < count; i++)
        {
            playerIndexes[i] = revealed.RevealedCards[i].PlayerIndex;
            cardValues[i] = (int)revealed.RevealedCards[i].Card;
        }

        return new LoveLetterNetworkEvent
        {
            EventType = LoveLetterNetworkEventType.CardsRevealed,
            RelatedPlayerIndexes = playerIndexes,
            CardValues = cardValues,
            Duration = revealed.SuggestedDuration
        };
    }

    [Server]
    private void BroadcastSnapshots()
    {
        foreach (LoveLetterNetworkPlayer player in players)
        {
            if (player == null || player.connectionToClient == null)
                continue;

            LoveLetterNetworkPlayer.ClientGameSnapshot snapshot = BuildSnapshotFor(player.PlayerIndex);
            player.TargetReceiveSnapshot(player.connectionToClient, snapshot);
        }
    }

    [Server]
    private void BroadcastToAll(LoveLetterNetworkEvent gameEvent)
    {
        foreach (LoveLetterNetworkPlayer player in players)
        {
            if (player == null || player.connectionToClient == null)
                continue;

            player.TargetReceiveGameEvent(player.connectionToClient, gameEvent);
        }
    }

    [Server]
    private void SendToPlayerIndex(int playerIndex, LoveLetterNetworkEvent gameEvent)
    {
        LoveLetterNetworkPlayer player = GetPlayerByIndex(playerIndex);
        if (player == null || player.connectionToClient == null)
            return;

        player.TargetReceiveGameEvent(player.connectionToClient, gameEvent);
    }

    [Server]
    private LoveLetterNetworkPlayer GetPlayerByIndex(int playerIndex)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].PlayerIndex == playerIndex)
                return players[i];
        }

        return null;
    }

    [Server]
    private void SendServerMessageTo(LoveLetterNetworkPlayer player, string message)
    {
        if (player == null || player.connectionToClient == null)
            return;

        player.TargetReceiveServerMessage(player.connectionToClient, message);
    }

    [Server]
    private LoveLetterNetworkPlayer.ClientGameSnapshot BuildSnapshotFor(int localPlayerIndex)
    {
        bool hasState = gameState != null;
        var snapshot = new LoveLetterNetworkPlayer.ClientGameSnapshot
        {
            localPlayerIndex = localPlayerIndex,
            currentTurnIndex = hasState ? gameState.CurrentTurnIndex : -1,
            roundFinished = hasState && gameState.RoundFinished,
            remainingDeckCount = hasState ? gameState.RemainingDeckCount : 0,

            pendingActionType = hasState ? gameState.PendingAction.actionType : PendingActionType.None,
            pendingSourceCard = hasState ? gameState.PendingAction.sourceCard : default,
            pendingCasterIndex = hasState ? gameState.PendingAction.casterIndex : -1,
            pendingSelectedTargetIndex = hasState ? gameState.PendingAction.selectedTargetIndex : -1,

            canStartGame = !HasStartedGame && players.Count >= 2,
            isHostPlayer = localPlayerIndex == 0,
            connectedPlayerCount = players.Count
        };

        snapshot.players = new LoveLetterNetworkPlayer.PlayerPublicView[gameState.Players.Length];

        for (int i = 0; i < gameState.Players.Length; i++)
        {
            LocalPlayerState source = gameState.Players[i];

            snapshot.players[i] = new LoveLetterNetworkPlayer.PlayerPublicView
            {
                playerIndex = i,
                playerName = source.playerName,
                handCount = source.hand.Count,
                discard = ToIntArray(source.discard),
                isProtected = source.isProtected,
                isEliminated = source.isEliminated,
                isGhost = source.isGhost,
                score = source.score
            };
        }

        snapshot.localHand = ToIntArray(gameState.Players[localPlayerIndex].hand);
        snapshot.visibleBurnedCards = ToIntArray(gameState.VisibleBurnedCards);
        snapshot.logs = gameState.Logs.ToArray();

        return snapshot;
    }

    private static int[] ToIntArray(List<CardType> cards)
    {
        if (cards == null || cards.Count == 0)
            return Array.Empty<int>();

        int[] values = new int[cards.Count];
        for (int i = 0; i < cards.Count; i++)
            values[i] = (int)cards[i];

        return values;
    }

    [Server]
    public bool CanStartGame()
    {
        return !HasStartedGame && players.Count >= 2;
    }

    [Server]
    public void HandleStartGameRequest(LoveLetterNetworkPlayer sender)
    {
        if (sender == null)
            return;

        if (HasStartedGame)
        {
            SendServerMessageTo(sender, "La partie a déjà commencé.");
            return;
        }

        if (players.Count < 2)
        {
            SendServerMessageTo(sender, "Il faut au moins 2 joueurs pour commencer.");
            return;
        }

        // Version simple :
        // seul le joueur d'index 0 peut lancer la partie
        if (sender.PlayerIndex != 0)
        {
            SendServerMessageTo(sender, "Seul l'hôte peut lancer la partie.");
            return;
        }

        TryStartGame();
    }
}