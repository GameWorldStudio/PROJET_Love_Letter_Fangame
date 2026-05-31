using System.Collections.Generic;
using System.Linq;

public class AiGameStateBuilder
{
    public AiGameState Build(GameState state)
    {
        return Build(state, -1, null);
    }

    public AiGameState Build(GameState state, int botIndex, BotMemory botMemory)
    {
        var aiState = new AiGameState();

        aiState.CurrentTurnIndex = state.CurrentTurnIndex;
        aiState.IsRoundFinished = state.RoundFinished;
        aiState.IsTwoPlayersMode = state.Players.Length == 2;

        // Players
        for (int i = 0; i < state.Players.Length; i++)
        {
            var p = state.Players[i];

            aiState.Players.Add(new AiPlayerState
            {
                PlayerIndex = i,
                Hand = new List<CardType>(p.hand),
                IsEliminated = p.isEliminated,
                IsProtected = p.isProtected,
                IsHuman = !p.isGhost
            });
        }

        // Défausse globale
        foreach (var p in state.Players)
        {
            aiState.DiscardedCards.AddRange(p.discard);
        }

        // Cartes visibles côté (mode 2 joueurs)
        aiState.VisibleSideCards = new List<CardType>(state.VisibleBurnedCards);

        // Carte cachée
        aiState.HiddenCardCount = state.HasHiddenBurnedCard ? 1 : 0;

        // KnownByCounts existant côté state
        if (state.KnownByCounts != null)
        {
            aiState.KnownByCounts = new Dictionary<int, int>(state.KnownByCounts);
        }
        else
        {
            aiState.KnownByCounts = new Dictionary<int, int>();
        }

        // Infos révélées depuis la mémoire du bot
        aiState.RevealedHandInfos = BuildRevealedInfosFromMemory(state, botIndex, botMemory, aiState.KnownByCounts);

        return aiState;
    }

    private List<RevealedCardInfo> BuildRevealedInfosFromMemory(
        GameState state,
        int botIndex,
        BotMemory botMemory,
        Dictionary<int, int> knownByCounts)
    {
        var result = new List<RevealedCardInfo>();

        if (botMemory == null || botMemory.KnownCardsByTarget == null)
            return result;

        foreach (var kvp in botMemory.KnownCardsByTarget)
        {
            int targetIndex = kvp.Key;
            RevealedCardInfo memoryInfo = kvp.Value;

            if (memoryInfo == null)
                continue;

            if (targetIndex < 0 || targetIndex >= state.Players.Length)
                continue;

            var targetPlayer = state.Players[targetIndex];

            if (targetPlayer.isEliminated)
                continue;

            // Si le joueur n'a plus de carte, l'info n'a plus de sens
            if (targetPlayer.hand == null || targetPlayer.hand.Count == 0)
                continue;

            // On ne garde que les infos encore marquées valides
            if (!memoryInfo.stillValid)
                continue;

            // On reconstruit l'info attendue par l'IA
            result.Add(new RevealedCardInfo
            {
                playerIndex = targetIndex,
                card = memoryInfo.card,
                stillValid = true
            });

            // Si on veut signaler qu'au moins un joueur connaît la carte de cette cible,
            // on s'assure qu'il y a une entrée.
            if (!knownByCounts.ContainsKey(targetIndex))
                knownByCounts[targetIndex] = 0;

            // Le bot courant connaît au moins cette carte.
            // On incrémente seulement si ce n'est pas lui-même.
            if (botIndex >= 0 && targetIndex != botIndex)
                knownByCounts[targetIndex] = System.Math.Max(knownByCounts[targetIndex], 1);
        }

        return result;
    }
}