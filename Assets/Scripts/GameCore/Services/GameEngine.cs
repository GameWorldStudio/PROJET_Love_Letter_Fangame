using System.Collections.Generic;

public class GameEngine
{
    private readonly RoundService roundService;
    private readonly TurnService turnService;
    private readonly TargetingService targetingService;
    private readonly CardEffectService cardEffectService;

    private readonly BotMemoryService botMemoryService;

    public GameEngine(
        RoundService roundService,
        TurnService turnService,
        TargetingService targetingService,
        CardEffectService cardEffectService,
        BotMemoryService botMemoryService)
    {
        this.roundService = roundService;
        this.turnService = turnService;
        this.targetingService = targetingService;
        this.cardEffectService = cardEffectService;
        this.botMemoryService = botMemoryService;
    }

    public GameFlowResult StartRound(GameState state, GameConfig config)
    {
        var result = roundService.StartRound(state, config);
        turnService.DrawForCurrentPlayer(state);

        result.ShouldTriggerBotTurn =
            !state.RoundFinished &&
            state.Players[state.CurrentTurnIndex].isGhost;

        return result;
    }

    public GameFlowResult Execute(PlayCardCommand command, GameState state)
    {
        var result = new GameFlowResult();

        if (state.RoundFinished)
            return result;

        if (command.PlayerIndex != state.CurrentTurnIndex)
            return result;

        if (state.PendingAction.actionType != PendingActionType.None)
            return result;

        var player = state.Players[command.PlayerIndex];

        if (player.isEliminated || player.hand.Count == 0)
            return result;

        if (!player.hand.Contains(command.Card))
            return result;

        bool hasCountess = player.hand.Contains(CardType.Countess);
        bool hasKingOrPrince = player.hand.Contains(CardType.King) || player.hand.Contains(CardType.Prince);

        if (hasCountess && hasKingOrPrince && command.Card != CardType.Countess)
        {
            AddLog(state, $"{player.playerName} doit jouer la Comtesse.");
            result.ShouldRefreshUi = true;
            return result;
        }

        player.hand.Remove(command.Card);
        player.discard.Add(command.Card);

        // La carte jouée n'est plus en main : si quelqu'un "connaissait" sa main restante
        // de façon obsolète, il faut invalider cette connaissance.
        botMemoryService.HandleCardPlayedFromHand(state, command.PlayerIndex, command.Card);
        botMemoryService.RememberPublicCardPlay(state, command.PlayerIndex, command.Card, "PublicPlay");

        result.Events.Add(new CardPlayedEvent
        {
            PlayerIndex = command.PlayerIndex,
            Card = command.Card
        });

        AddLog(state, $"{player.playerName} joue {GetCardLabel(command.Card)}.");

        switch (command.Card)
        {
            case CardType.Handmaid:
            case CardType.Countess:
            case CardType.Princess:
                cardEffectService.ResolveImmediateEffect(state, command.PlayerIndex, command.Card, result);
                return Merge(result, turnService.FinalizeAction(state));
            case CardType.Guard:
            case CardType.Priest:
            case CardType.Baron:
            case CardType.Prince:
            case CardType.King:
                {

                    var validTargets = targetingService.GetValidTargets(state, command.PlayerIndex, command.Card);

                    // Si aucune cible valide, la carte est jouée dans le vide
                    // et le tour doit simplement se terminer.
                    if (validTargets == null || validTargets.Count == 0)
                    {
                        AddLog(state, $"{player.playerName} joue {GetCardLabel(command.Card)}, mais aucune cible valide n'est disponible.");

                        result.StateChanged = true;
                        result.ShouldRefreshUi = true;

                        return Merge(result, turnService.FinalizeAction(state));
                    }
                    state.PendingAction.actionType = PendingActionType.SelectTarget;
                    state.PendingAction.sourceCard = command.Card;
                    state.PendingAction.casterIndex = command.PlayerIndex;
                    state.PendingAction.selectedTargetIndex = -1;

                    result.StateChanged = true;
                    result.ShouldRefreshUi = true;
                    return result;
                }

           

            default:
                return result;
        }
    }

    public GameFlowResult Execute(SelectTargetCommand command, GameState state)
    {
        var result = new GameFlowResult();

        if (state.RoundFinished)
            return result;

        if (state.PendingAction.actionType != PendingActionType.SelectTarget)
            return result;

        int casterIndex = state.PendingAction.casterIndex;
        CardType sourceCard = state.PendingAction.sourceCard;

        if (!targetingService.IsValidTarget(state, casterIndex, sourceCard, command.TargetIndex))
            return result;

        state.PendingAction.selectedTargetIndex = command.TargetIndex;

        switch (sourceCard)
        {
            case CardType.Guard:
                state.PendingAction.actionType = PendingActionType.SelectGuardGuess;
                result.StateChanged = true;
                result.ShouldRefreshUi = true;
                return result;

            case CardType.Priest:
                cardEffectService.ResolvePriestTarget(state, casterIndex, command.TargetIndex, result);
                state.PendingAction.Clear();
                return Merge(result, turnService.FinalizeAction(state));

            case CardType.Baron:
                cardEffectService.ResolveBaronTarget(state, casterIndex, command.TargetIndex, result);
                state.PendingAction.Clear();
                return Merge(result, turnService.FinalizeAction(state));

            case CardType.Prince:
                cardEffectService.ResolvePrinceTarget(state, casterIndex, command.TargetIndex, result);
                state.PendingAction.Clear();
                return Merge(result, turnService.FinalizeAction(state));

            case CardType.King:
                cardEffectService.ResolveKingTarget(state, casterIndex, command.TargetIndex, result);
                state.PendingAction.Clear();
                return Merge(result, turnService.FinalizeAction(state));

            default:
                return result;
        }
    }

    public GameFlowResult Execute(SelectGuardGuessCommand command, GameState state)
    {
        var result = new GameFlowResult();

        if (state.RoundFinished)
            return result;

        if (state.PendingAction.actionType != PendingActionType.SelectGuardGuess)
            return result;

        if (command.GuessedCard == CardType.Guard)
            return result;

        int casterIndex = state.PendingAction.casterIndex;
        int targetIndex = state.PendingAction.selectedTargetIndex;

        if (targetIndex < 0)
            return result;

        cardEffectService.ResolveGuardGuess(state, casterIndex, targetIndex, command.GuessedCard, result);

        state.PendingAction.Clear();

        return Merge(result, turnService.FinalizeAction(state));
    }

    private GameFlowResult Merge(GameFlowResult target, GameFlowResult source)
    {
        target.StateChanged |= source.StateChanged;
        target.ShouldRefreshUi |= source.ShouldRefreshUi;
        target.ShouldTriggerBotTurn |= source.ShouldTriggerBotTurn;

        foreach (var e in source.Events)
            target.Events.Add(e);

        return target;
    }

    private void AddLog(GameState state, string message)
    {
        state.Logs.Insert(0, message);

        if (state.Logs.Count > 20)
            state.Logs.RemoveAt(state.Logs.Count - 1);
    }

    private string GetCardLabel(CardType card)
    {
        return card switch
        {
            CardType.Guard => "la Garde",
            CardType.Priest => "le Prêtre",
            CardType.Baron => "le Baron",
            CardType.Handmaid => "la Servante",
            CardType.Prince => "le Prince",
            CardType.King => "le Roi",
            CardType.Countess => "la Comtesse",
            CardType.Princess => "la Princesse",
            _ => card.ToString()
        };
    }
}