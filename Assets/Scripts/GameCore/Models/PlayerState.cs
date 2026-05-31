using System.Collections.Generic;

[System.Serializable]
public class PlayerState
{
    public string playerName;
    public List<CardType> hand = new List<CardType>();
    public List<CardType> discard = new List<CardType>();
    public bool isProtected;
    public bool isEliminated;
    public bool isGhost;
    public int score;

    public PlayerState(string playerName, bool isGhost = false)
    {
        this.playerName = playerName;
        this.isGhost = isGhost;
    }
}