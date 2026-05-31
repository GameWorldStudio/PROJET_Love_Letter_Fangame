using System.Collections.Generic;

[System.Serializable]
public class PlayerState
{
    public int connectionId;
    public string playerName;
    public List<CardType> hand = new List<CardType>();
    public List<CardType> discard = new List<CardType>();
    public bool isProtected;
    public bool isEliminated;
    public int score;

    public PlayerState(int connectionId, string playerName)
    {
        this.connectionId = connectionId;
        this.playerName = playerName;
        score = 0;
    }
}