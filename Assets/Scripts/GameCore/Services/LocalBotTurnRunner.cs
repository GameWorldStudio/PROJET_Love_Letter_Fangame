using System.Collections;
using UnityEngine;

public class LocalBotTurnRunner : MonoBehaviour
{
    private readonly LoveLetterAiService aiService = new();
    private readonly AiGameStateBuilder aiBuilder = new();

    public void TryRunBotTurn(LocalGameController controller, GameState state, GameEngine engine, float delay)
    {
        if (state == null || engine == null || controller == null)
            return;

        if (state.RoundFinished)
            return;

        if (!state.Players[state.CurrentTurnIndex].isGhost)
            return;

        StartCoroutine(Run(controller, state, engine, delay));
    }

    private IEnumerator Run(LocalGameController controller, GameState state, GameEngine engine, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (state == null || engine == null || controller == null)
            yield break;

        if (state.RoundFinished)
            yield break;

        int botIndex = state.CurrentTurnIndex;
        var bot = state.Players[botIndex];

        if (bot.isEliminated || !bot.isGhost)
            yield break;

        BotMemory botMemory = GetBotMemory(state, botIndex);

        AiGameState aiState = aiBuilder.Build(state, botIndex, botMemory);
        AiDecision decision = aiService.ChooseBestMove(aiState, botIndex);

        if (decision == null)
            yield break;

        var playResult = engine.Execute(
            new PlayCardCommand(botIndex, decision.cardToPlay),
            state
        );

        controller.ApplyBotResult(playResult);

        if (state.RoundFinished)
            yield break;

        if (state.PendingAction.actionType == PendingActionType.SelectTarget)
        {
            yield return new WaitForSeconds(0.3f);

            var targetResult = engine.Execute(
                new SelectTargetCommand(decision.targetPlayerIndex),
                state
            );

            controller.ApplyBotResult(targetResult);
        }

        if (state.RoundFinished)
            yield break;

        if (state.PendingAction.actionType == PendingActionType.SelectGuardGuess)
        {
            yield return new WaitForSeconds(0.3f);

            var guessResult = engine.Execute(
                new SelectGuardGuessCommand(decision.guessedCard),
                state
            );

            controller.ApplyBotResult(guessResult);
        }
    }

    private BotMemory GetBotMemory(GameState state, int botIndex)
    {
        if (state.BotMemories == null)
            return null;

        if (state.BotMemories.TryGetValue(botIndex, out BotMemory memory))
            return memory;

        return null;
    }
}