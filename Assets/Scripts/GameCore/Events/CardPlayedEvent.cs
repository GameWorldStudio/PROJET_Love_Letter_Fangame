using UnityEngine;

public class CardPlayedEvent : GameEvent
{
    public int PlayerIndex { get; set; }
    public CardType Card { get; set; }
}
