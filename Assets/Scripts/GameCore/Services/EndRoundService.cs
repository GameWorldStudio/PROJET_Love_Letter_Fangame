using System.Collections.Generic;
using System.Linq;

public class EndRoundService
{
    public GameFlowResult ResolveEndOfRound(GameState state)
    {
        var result = new GameFlowResult();

        if (state.RoundFinished)
            return result;

        var alivePlayers = GetAlivePlayers(state);

        if (alivePlayers.Count == 1)
        {
            int winnerIndex = alivePlayers[0];
            state.Players[winnerIndex].score++;

            AddLog(state, $"{state.Players[winnerIndex].playerName} remporte la manche.");

            state.RoundFinished = true;
            EmitRevealRemainingPlayers(state, result);

            result.Events.Add(new RoundEndedEvent());
            result.StateChanged = true;
            result.ShouldRefreshUi = true;
            return result;
        }

        if (state.Deck.Count == 0 && state.PendingAction.actionType == PendingActionType.None)
        {
            ResolveDeckEmptyRoundEnd(state, alivePlayers, result);
        }

        return result;
    }

    private void ResolveDeckEmptyRoundEnd(GameState state, List<int> alivePlayers, GameFlowResult result)
    {
        int bestValue = -1;
        List<int> winners = new();

        foreach (int playerIndex in alivePlayers)
        {
            var player = state.Players[playerIndex];

            if (player.hand.Count == 0)
                continue;

            int cardValue = (int)player.hand[0];

            if (cardValue > bestValue)
            {
                bestValue = cardValue;
                winners.Clear();
                winners.Add(playerIndex);
            }
            else if (cardValue == bestValue)
            {
                winners.Add(playerIndex);
            }
        }

        if (winners.Count == 1)
        {
            int winnerIndex = winners[0];
            state.Players[winnerIndex].score++;

            AddLog(state,
                $"{state.Players[winnerIndex].playerName} gagne à la carte la plus forte ({state.Players[winnerIndex].hand[0]}).");
        }
        else
        {
            string winnerNames = string.Join(", ", winners.Select(i => state.Players[i].playerName));
            AddLog(state, $"Égalité à la carte la plus forte entre {winnerNames}.");
        }

        state.RoundFinished = true;
      //  EmitRevealRemainingPlayers(state, result);

        PersistRevealRemainingPlayers(state);
        result.Events.Add(new RoundEndedEvent());
        result.StateChanged = true;
        result.ShouldRefreshUi = true;
    }

    private List<int> GetAlivePlayers(GameState state)
    {
        List<int> alive = new();

        for (int i = 0; i < state.Players.Length; i++)
        {
            if (!state.Players[i].isEliminated)
                alive.Add(i);
        }

        return alive;
    }

    private void EmitRevealRemainingPlayers(GameState state, GameFlowResult result)
    {
        var revealed = new List<RevealedCardInfoGlobal>();

        for (int i = 0; i < state.Players.Length; i++)
        {
            if (!state.Players[i].isEliminated && state.Players[i].hand.Count > 0)
            {
                revealed.Add(new RevealedCardInfoGlobal
                {
                    PlayerIndex = i,
                    Card = state.Players[i].hand[0]
                });
            }
        }

        if (revealed.Count > 0)
        {
            result.Events.Add(new CardsRevealedEvent
            {
                RevealedCards = revealed,
                SuggestedDuration = 3f
            });
        }
    }

    private void PersistRevealRemainingPlayers(GameState state)
    {
        if (state.PersistentlyRevealedCards == null)
            state.PersistentlyRevealedCards = new Dictionary<int, CardType>();
        else
            state.PersistentlyRevealedCards.Clear();

        for (int i = 0; i < state.Players.Length; i++)
        {
            if (state.Players[i].hand != null && state.Players[i].hand.Count > 0)
            {
                state.PersistentlyRevealedCards[i] = state.Players[i].hand[0];
            }
        }
    }

    private void AddLog(GameState state, string message)
    {
        state.Logs.Insert(0, message);

        if (state.Logs.Count > 20)
            state.Logs.RemoveAt(state.Logs.Count - 1);
    }
}