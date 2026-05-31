using System;
using Mirror;

[Serializable]
public class LoveLetterNetworkEvent
{
    public LoveLetterNetworkEventType EventType;

    public int PlayerIndex = -1;
    public int SecondaryPlayerIndex = -1;

    public int CardValue = -1;
    public int[] CardValues = Array.Empty<int>();
    public int[] RelatedPlayerIndexes = Array.Empty<int>();

    public float Duration = 0f;
    public string Message = string.Empty;
}

public enum LoveLetterNetworkEventType
{
    None = 0,
    RoundStarted = 1,
    RoundEnded = 2,
    TurnEnded = 3,
    CardPlayed = 4,
    PlayerEliminated = 5,
    CardsRevealed = 6
}