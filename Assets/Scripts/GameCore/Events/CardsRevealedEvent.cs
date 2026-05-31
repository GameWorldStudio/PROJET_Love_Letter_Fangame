using System.Collections.Generic;

public class RevealedCardInfoGlobal
{
    public int PlayerIndex { get; set; }
    public CardType Card { get; set; }
}

public class CardsRevealedEvent : GameEvent
{
    public List<RevealedCardInfoGlobal> RevealedCards { get; set; } = new();
    public float SuggestedDuration { get; set; } = 2.5f;
}