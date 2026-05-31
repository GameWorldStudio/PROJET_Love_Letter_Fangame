using System;
using System.Collections.Generic;

public class BotMemoryService
{
    public void InitializeForNewRound(GameState state)
    {
        if (state == null || state.Players == null)
            return;

        if (state.BotMemories == null)
            state.BotMemories = new Dictionary<int, BotMemory>();
        else
            state.BotMemories.Clear();

        if (state.KnownByCounts == null)
            state.KnownByCounts = new Dictionary<int, int>();
        else
            state.KnownByCounts.Clear();

        for (int i = 0; i < state.Players.Length; i++)
        {
            if (state.Players[i].isGhost)
            {
                state.BotMemories[i] = new BotMemory();
            }
        }
    }

    public void RememberCardForBot(
        GameState state,
        int observerBotIndex,
        int targetPlayerIndex,
        CardType card,
        string eventType,
        bool isPublic)
    {
        if (state == null || state.BotMemories == null)
            return;

        if (!state.BotMemories.TryGetValue(observerBotIndex, out BotMemory memory) || memory == null)
            return;

        if (memory.KnownCardsByTarget == null)
            memory.KnownCardsByTarget = new Dictionary<int, RevealedCardInfo>();

        memory.KnownCardsByTarget[targetPlayerIndex] = new RevealedCardInfo
        {
            playerIndex = targetPlayerIndex,
            card = card,
            stillValid = true
        };

        AddMemoryEvent(memory, observerBotIndex, targetPlayerIndex, card, eventType, isPublic);

        if (state.KnownByCounts == null)
            state.KnownByCounts = new Dictionary<int, int>();

        if (!state.KnownByCounts.ContainsKey(targetPlayerIndex))
            state.KnownByCounts[targetPlayerIndex] = 0;

        state.KnownByCounts[targetPlayerIndex] = CountBotsKnowingCard(state, targetPlayerIndex);
    }

    public void RememberCardForAllBots(
        GameState state,
        int targetPlayerIndex,
        CardType card,
        string eventType,
        bool isPublic)
    {
        if (state == null || state.BotMemories == null)
            return;

        foreach (var kvp in state.BotMemories)
        {
            int observerBotIndex = kvp.Key;
            BotMemory memory = kvp.Value;

            if (memory == null)
                continue;

            if (memory.KnownCardsByTarget == null)
                memory.KnownCardsByTarget = new Dictionary<int, RevealedCardInfo>();

            memory.KnownCardsByTarget[targetPlayerIndex] = new RevealedCardInfo
            {
                playerIndex = targetPlayerIndex,
                card = card,
                stillValid = true
            };

            AddMemoryEvent(memory, observerBotIndex, targetPlayerIndex, card, eventType, isPublic);
        }

        if (state.KnownByCounts == null)
            state.KnownByCounts = new Dictionary<int, int>();

        state.KnownByCounts[targetPlayerIndex] = CountBotsKnowingCard(state, targetPlayerIndex);
    }

    public void RememberPublicCardPlay(
        GameState state,
        int targetPlayerIndex,
        CardType card,
        string eventType = "PublicPlay")
    {
        if (state == null || state.BotMemories == null)
            return;

        foreach (var kvp in state.BotMemories)
        {
            int observerBotIndex = kvp.Key;
            BotMemory memory = kvp.Value;

            if (memory == null)
                continue;

            AddMemoryEvent(memory, observerBotIndex, targetPlayerIndex, card, eventType, true);
        }
    }

    public void RememberPublicDiscard(
        GameState state,
        int targetPlayerIndex,
        CardType card,
        string eventType = "PublicDiscard")
    {
        if (state == null || state.BotMemories == null)
            return;

        foreach (var kvp in state.BotMemories)
        {
            int observerBotIndex = kvp.Key;
            BotMemory memory = kvp.Value;

            if (memory == null)
                continue;

            AddMemoryEvent(memory, observerBotIndex, targetPlayerIndex, card, eventType, true);
        }
    }

    public void InvalidateKnownCardForAllBots(GameState state, int targetPlayerIndex)
    {
        if (state == null || state.BotMemories == null)
            return;

        foreach (var kvp in state.BotMemories)
        {
            BotMemory memory = kvp.Value;

            if (memory?.KnownCardsByTarget == null)
                continue;

            if (memory.KnownCardsByTarget.TryGetValue(targetPlayerIndex, out RevealedCardInfo info) && info != null)
            {
                info.stillValid = false;
            }
        }

        if (state.KnownByCounts == null)
            state.KnownByCounts = new Dictionary<int, int>();

        state.KnownByCounts[targetPlayerIndex] = 0;
    }

    public void InvalidateKnownCardsForPlayers(GameState state, params int[] playerIndexes)
    {
        if (playerIndexes == null)
            return;

        foreach (int playerIndex in playerIndexes)
        {
            InvalidateKnownCardForAllBots(state, playerIndex);
        }
    }

    public void ClearBotMemory(GameState state, int botIndex)
    {
        if (state?.BotMemories == null)
            return;

        if (state.BotMemories.ContainsKey(botIndex))
            state.BotMemories[botIndex] = new BotMemory();

        RebuildKnownByCounts(state);
    }

    public void RebuildKnownByCounts(GameState state)
    {
        if (state == null)
            return;

        if (state.KnownByCounts == null)
            state.KnownByCounts = new Dictionary<int, int>();
        else
            state.KnownByCounts.Clear();

        if (state.Players == null)
            return;

        for (int i = 0; i < state.Players.Length; i++)
        {
            state.KnownByCounts[i] = CountBotsKnowingCard(state, i);
        }
    }

    public int CountBotsKnowingCard(GameState state, int targetPlayerIndex)
    {
        if (state?.BotMemories == null)
            return 0;

        int count = 0;

        foreach (var kvp in state.BotMemories)
        {
            BotMemory memory = kvp.Value;

            if (memory?.KnownCardsByTarget == null)
                continue;

            if (memory.KnownCardsByTarget.TryGetValue(targetPlayerIndex, out RevealedCardInfo info) &&
                info != null &&
                info.stillValid)
            {
                count++;
            }
        }

        return count;
    }

    private void AddMemoryEvent(
        BotMemory memory,
        int observerBotIndex,
        int targetPlayerIndex,
        CardType card,
        string eventType,
        bool isPublic)
    {
        if (memory.MemoryEvents == null)
            memory.MemoryEvents = new List<BotMemoryEvent>();

        memory.MemoryEvents.Add(new BotMemoryEvent
        {
            observerBotIndex = observerBotIndex,
            targetPlayerIndex = targetPlayerIndex,
            card = card,
            eventType = eventType,
            isPublic = isPublic
        });
    }

    public void HandleCardPlayedFromHand(GameState state, int playerIndex, CardType playedCard)
    {
        if (state?.BotMemories == null)
            return;

        foreach (var kvp in state.BotMemories)
        {
            BotMemory memory = kvp.Value;

            if (memory?.KnownCardsByTarget == null)
                continue;

            if (!memory.KnownCardsByTarget.TryGetValue(playerIndex, out RevealedCardInfo info) || info == null)
                continue;

            if (!info.stillValid)
                continue;

            // Si la carte connue est précisément celle qui vient d'être jouée,
            // alors la connaissance devient fausse.
            if (info.card == playedCard)
            {
                info.stillValid = false;
            }
            // Sinon, on conserve l'info :
            // cela signifie que le joueur a joué son autre carte,
            // donc la carte connue reste logiquement en main.
        }

        if (state.KnownByCounts == null)
            state.KnownByCounts = new Dictionary<int, int>();

        state.KnownByCounts[playerIndex] = CountBotsKnowingCard(state, playerIndex);
    }
}