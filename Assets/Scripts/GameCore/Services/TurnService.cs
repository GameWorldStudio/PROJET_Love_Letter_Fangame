public class TurnService
{
    private readonly RoundService roundService;
    private readonly EndRoundService endRoundService;

    public TurnService(RoundService roundService, EndRoundService endRoundService)
    {
        this.roundService = roundService;
        this.endRoundService = endRoundService;
    }

    public void DrawForCurrentPlayer(GameState state)
    {
        if (state.RoundFinished)
            return;

        var player = state.Players[state.CurrentTurnIndex];

        if (player.isEliminated)
            return;

        player.isProtected = false;

        if (player.hand.Count < 2 && state.Deck.Count > 0)
        {
            player.hand.Add(roundService.PopDeck(state));
            AddLog(state, $"{player.playerName} pioche une carte.");
        }
    }

    public GameFlowResult EndTurn(GameState state)
    {
        var result = new GameFlowResult();

        if (state.RoundFinished)
            return result;

        state.CurrentTurnIndex = GetNextAlivePlayerIndex(state, state.CurrentTurnIndex);
        DrawForCurrentPlayer(state);
        result.Events.Add(new TurnEndedEvent());
        result.StateChanged = true;
        result.ShouldRefreshUi = true;
        result.ShouldTriggerBotTurn =
            !state.RoundFinished &&
            state.Players[state.CurrentTurnIndex].isGhost;

        return result;
    }

    public GameFlowResult FinalizeAction(GameState state)
    {
        var result = new GameFlowResult();

        state.PendingAction.Clear();

        var endResult = endRoundService.ResolveEndOfRound(state);
        Merge(result, endResult);

        if (!state.RoundFinished)
        {
            var endTurnResult = EndTurn(state);
            Merge(result, endTurnResult);
        }

        return result;
    }

    private int GetNextAlivePlayerIndex(GameState state, int currentIndex)
    {
        int playerCount = state.Players.Length;
        int nextIndex = currentIndex;

        for (int i = 0; i < playerCount; i++)
        {
            nextIndex = (nextIndex + 1) % playerCount;

            if (!state.Players[nextIndex].isEliminated)
                return nextIndex;
        }

        return currentIndex;
    }

    private void Merge(GameFlowResult target, GameFlowResult source)
    {
        target.StateChanged |= source.StateChanged;
        target.ShouldRefreshUi |= source.ShouldRefreshUi;
        target.ShouldTriggerBotTurn |= source.ShouldTriggerBotTurn;

        foreach (var e in source.Events)
            target.Events.Add(e);
    }

    private void AddLog(GameState state, string message)
    {
        state.Logs.Insert(0, message);

        if (state.Logs.Count > 20)
            state.Logs.RemoveAt(state.Logs.Count - 1);
    }
}