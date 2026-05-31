using System;

[Serializable]
public class BotMemoryEvent
{
    public int observerBotIndex;
    public int targetPlayerIndex;
    public CardType card;
    public string eventType; // "PriestSeen", "BaronSeen", "PublicDiscard", "PublicPlay", etc.
    public bool isPublic;
}