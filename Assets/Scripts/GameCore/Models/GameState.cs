using System.Collections.Generic;
using UnityEngine;

public class GameState
{
    public List<CardType> Deck { get; } = new();
    public List<string> Logs { get; } = new();
    public List<CardType> VisibleBurnedCards { get; } = new();

    public PlayerState[] Players { get; set; }
    public PendingActionState PendingAction { get; set; } = new();

    public CardType HiddenBurnedCard { get; set; }
    public bool HasHiddenBurnedCard { get; set; }

    public int CurrentTurnIndex { get; set; }
    public bool RoundFinished { get; set; }

    public int LocalPlayerIndex { get; set; } = 0;

    public Dictionary<int, BotMemory> BotMemories { get; set; } = new();
    public Dictionary<int, int> KnownByCounts { get; set; } = new();

    public int RemainingDeckCount => Deck.Count;
    public bool IsLocalPlayersTurn => CurrentTurnIndex == LocalPlayerIndex;

    public Dictionary<int, CardType> PersistentlyRevealedCards { get; set; } = new();
}
