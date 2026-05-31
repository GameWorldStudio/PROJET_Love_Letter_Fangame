using System.Collections.Generic;
using UnityEngine;

public class RoundService
{
    private readonly BotMemoryService botMemoryService;

    public RoundService(BotMemoryService botMemoryService)
    {
        this.botMemoryService = botMemoryService;
    }

    public GameFlowResult StartRound(GameState state, GameConfig config)
    {
        var result = new GameFlowResult();

        state.RoundFinished = false;
        state.CurrentTurnIndex = GetRandomStartingPlayerIndex(state);

        state.Deck.Clear();
        state.Logs.Clear();
        state.VisibleBurnedCards.Clear();
        state.PendingAction.Clear();
        state.PersistentlyRevealedCards.Clear();

        BuildDeck(state);
        ShuffleDeck(state);

        state.HiddenBurnedCard = PopDeck(state);
        state.HasHiddenBurnedCard = true;

        if (config.TwoPlayersMode)
        {
            for (int i = 0; i < config.VisibleBurnedCardsInTwoPlayers; i++)
                state.VisibleBurnedCards.Add(PopDeck(state));
        }

        for (int i = 0; i < state.Players.Length; i++)
        {
            var p = state.Players[i];
            p.hand.Clear();
            p.discard.Clear();
            p.isProtected = false;
            p.isEliminated = false;

            p.hand.Add(PopDeck(state));
        }

        botMemoryService.InitializeForNewRound(state);

        AddLog(state, "Nouvelle manche commencée.");
        AddLog(state, $"{state.Players[state.CurrentTurnIndex].playerName} commence la manche.");

        result.Events.Add(new RoundStartedEvent());
        result.ShouldRefreshUi = true;
        result.StateChanged = true;
        return result;
    }

    private int GetRandomStartingPlayerIndex(GameState state)
    {
        if (state?.Players == null || state.Players.Length == 0)
            return 0;

        return Random.Range(0, state.Players.Length);
    }

    public CardType PopDeck(GameState state)
    {
        CardType card = state.Deck[0];
        state.Deck.RemoveAt(0);
        return card;
    }

    public void BuildDeck(GameState state)
    {
        state.Deck.AddRange(new List<CardType>
        {
            CardType.Guard, CardType.Guard, CardType.Guard, CardType.Guard, CardType.Guard,
            CardType.Priest, CardType.Priest,
            CardType.Baron, CardType.Baron,
            CardType.Handmaid, CardType.Handmaid,
            CardType.Prince, CardType.Prince,
            CardType.King,
            CardType.Countess,
            CardType.Princess
        });
    }

    public void ShuffleDeck(GameState state)
    {
        for (int i = 0; i < state.Deck.Count; i++)
        {
            int randomIndex = Random.Range(i, state.Deck.Count);
            (state.Deck[i], state.Deck[randomIndex]) = (state.Deck[randomIndex], state.Deck[i]);
        }
    }

    private void AddLog(GameState state, string message)
    {
        state.Logs.Insert(0, message);

        if (state.Logs.Count > 20)
            state.Logs.RemoveAt(state.Logs.Count - 1);
    }
}