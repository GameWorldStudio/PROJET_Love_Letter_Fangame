using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;

public class CardEffectService
{
    private readonly RoundService roundService;
    private readonly BotMemoryService botMemoryService;

    public CardEffectService(RoundService roundService, BotMemoryService botMemoryService)
    {
        this.roundService = roundService;
        this.botMemoryService = botMemoryService;
    }

    public void ResolveImmediateEffect(
        GameState state,
        int casterIndex,
        CardType playedCard,
        GameFlowResult result)
    {
        var caster = state.Players[casterIndex];

        switch (playedCard)
        {
            case CardType.Handmaid:
                caster.isProtected = true;
                AddLog(state, $"{caster.playerName} joue la Servante.");
                result.StateChanged = true;
                result.ShouldRefreshUi = true;
                break;

            case CardType.Countess:
                AddLog(state, $"{caster.playerName} joue la Comtesse.");
                result.StateChanged = true;
                result.ShouldRefreshUi = true;
                break;

            case CardType.Princess:
                EliminatePlayer(
          state,
          casterIndex,
          result,
          $"{caster.playerName} défausse la Princesse et est éliminé."
      );
                break;
        }
    }

    public void ResolvePriestTarget(
        GameState state,
        int casterIndex,
        int targetIndex,
        GameFlowResult result)
    {
        var caster = state.Players[casterIndex];
        var target = state.Players[targetIndex];

        AddLog(state, $"{caster.playerName} regarde la carte de {target.playerName}.");

        if (target.hand.Count > 0)
        {
            result.Events.Add(new CardsRevealedEvent
            {
                RevealedCards = new List<RevealedCardInfoGlobal>
    {
        new RevealedCardInfoGlobal
        {
            PlayerIndex = targetIndex,
            Card = target.hand[0]
        }
    },
                SuggestedDuration = 2.5f
            });
        }

        if (state.Players[casterIndex].isGhost && target.hand.Count > 0)
        {
            botMemoryService.RememberCardForBot(
                state,
                casterIndex,
                targetIndex,
                target.hand[0],
                "PriestSeen",
                false
            );
        }

        result.StateChanged = true;
        result.ShouldRefreshUi = true;
    }

    public void ResolveBaronTarget(
        GameState state,
        int casterIndex,
        int targetIndex,
        GameFlowResult result)
    {
        var caster = state.Players[casterIndex];
        var target = state.Players[targetIndex];

        if (caster.hand.Count == 0 || target.hand.Count == 0)
            return;

        CardType casterCard = caster.hand[0];
        CardType targetCard = target.hand[0];

        AddLog(state, $"{caster.playerName} compare sa main avec {target.playerName} grâce au Baron.");

        result.Events.Add(new CardsRevealedEvent
        {
            RevealedCards = new List<RevealedCardInfoGlobal>
    {
        new RevealedCardInfoGlobal
        {
            PlayerIndex = casterIndex,
            Card = casterCard
        },
        new RevealedCardInfoGlobal
        {
            PlayerIndex = targetIndex,
            Card = targetCard
        }
    },
            SuggestedDuration = 2.5f
        });

        if (caster.hand.Count > 0 && target.hand.Count > 0)
        {
            if (state.Players[casterIndex].isGhost)
            {
                botMemoryService.RememberCardForBot(
                    state,
                    casterIndex,
                    targetIndex,
                    target.hand[0],
                    "BaronSeen",
                    false
                );
            }

            if (state.Players[targetIndex].isGhost)
            {
                botMemoryService.RememberCardForBot(
                    state,
                    targetIndex,
                    casterIndex,
                    caster.hand[0],
                    "BaronSeen",
                    false
                );
            }
        }

        if (casterCard > targetCard)
        {
            EliminatePlayer(state, targetIndex, result, $"{target.playerName} perd le duel du Baron.");
        }
        else if (targetCard > casterCard)
        {
            EliminatePlayer(state, casterIndex, result, $"{caster.playerName} perd le duel du Baron.");
        }
        else
        {
            AddLog(state, "Égalité avec le Baron.");
        }

        result.StateChanged = true;
        result.ShouldRefreshUi = true;
    }

    public void ResolvePrinceTarget(
        GameState state,
        int casterIndex,
        int targetIndex,
        GameFlowResult result)
    {
        ForceDiscardAndRedraw(state, targetIndex, result);
        result.StateChanged = true;
        result.ShouldRefreshUi = true;
    }

    public void ResolveKingTarget(
        GameState state,
        int casterIndex,
        int targetIndex,
        GameFlowResult result)
    {
        var caster = state.Players[casterIndex];
        var target = state.Players[targetIndex];

        if (caster.hand.Count == 0 || target.hand.Count == 0)
            return;

        CardType tmp = caster.hand[0];
        caster.hand[0] = target.hand[0];
        target.hand[0] = tmp;

        if (state.Players[casterIndex].isGhost && target.hand.Count > 0)
        {
            botMemoryService.RememberCardForBot(
                state,
                casterIndex,
                targetIndex,
                target.hand[0],
                "KingSeenAfterSwap",
                false
            );
        }

        if (state.Players[targetIndex].isGhost && caster.hand.Count > 0)
        {
            botMemoryService.RememberCardForBot(
                state,
                targetIndex,
                casterIndex,
                caster.hand[0],
                "KingSeenAfterSwap",
                false
            );
        }

        AddLog(state, $"{caster.playerName} échange sa main avec {target.playerName}.");

        result.StateChanged = true;
        result.ShouldRefreshUi = true;
    }

    public void ResolveGuardGuess(
        GameState state,
        int casterIndex,
        int targetIndex,
        CardType guessedCard,
        GameFlowResult result)
    {
        var caster = state.Players[casterIndex];
        var target = state.Players[targetIndex];

        if (target.hand.Count == 0)
            return;

        if (target.hand[0] == guessedCard)
        {
            EliminatePlayer(
          state,
          targetIndex,
          result,
          $"{caster.playerName} annonce correctement {guessedCard} contre {target.playerName}."
      );
        }
        else
        {
            AddLog(state, $"{caster.playerName} se trompe avec la Garde ({guessedCard}).");
        }

        result.StateChanged = true;
        result.ShouldRefreshUi = true;
    }

    public void ForceDiscardAndRedraw(
        GameState state,
        int targetIndex,
        GameFlowResult result)
    {
        var target = state.Players[targetIndex];
        botMemoryService.InvalidateKnownCardForAllBots(state, targetIndex);

        if (target.hand.Count == 0)
            return;

        CardType discarded = target.hand[0];

        target.hand.Clear();
        target.discard.Add(discarded);
        target.isProtected = false;

        AddLog(state, $"{target.playerName} défausse {discarded}.");

        if (discarded == CardType.Princess)
        {
            if (discarded == CardType.Princess)
            {
                EliminatePlayer(
                    state,
                    targetIndex,
                    result,
                    $"{target.playerName} est éliminé en défaussant la Princesse."
                );
                return;
            }
        }

        if (state.Deck.Count > 0)
        {
            target.hand.Add(roundService.PopDeck(state));
            AddLog(state, $"{target.playerName} repioche une carte.");
        }
        else if (state.HasHiddenBurnedCard)
        {
            target.hand.Add(state.HiddenBurnedCard);
            state.HasHiddenBurnedCard = false;
            AddLog(state, $"{target.playerName} repioche la carte face cachée.");
        }
        else
        {
            AddLog(state, $"{target.playerName} ne peut pas repiocher.");
        }
        botMemoryService.RememberPublicDiscard(state, targetIndex, discarded, "PublicDiscard");
    }

    private void EliminatePlayer(GameState state, int playerIndex, GameFlowResult result, string logMessage = null)
    {
        var player = state.Players[playerIndex];

        if (player.isEliminated)
            return;

        player.isEliminated = true;
        player.isProtected = false;

        if (!string.IsNullOrEmpty(logMessage))
            AddLog(state, logMessage);

        // Règle globale : un joueur éliminé défausse sa carte restante
        if (player.hand != null && player.hand.Count > 0)
        {
            CardType remainingCard = player.hand[0];
            player.hand.Clear();
            player.discard.Add(remainingCard);

            AddLog(state, $"{player.playerName} défausse {remainingCard} en étant éliminé.");
            botMemoryService?.RememberPublicDiscard(state, playerIndex, remainingCard, "EliminationDiscard");
        }

        botMemoryService?.InvalidateKnownCardForAllBots(state, playerIndex);

        result.Events.Add(new PlayerEliminatedEvent
        {
            PlayerIndex = playerIndex
        });

        result.StateChanged = true;
        result.ShouldRefreshUi = true;
    }
    private void AddLog(GameState state, string message)
    {
        state.Logs.Insert(0, message);

        if (state.Logs.Count > 20)
            state.Logs.RemoveAt(state.Logs.Count - 1);
    }
}