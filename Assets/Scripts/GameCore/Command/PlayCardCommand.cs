public class PlayCardCommand
{
    public int PlayerIndex { get; }
    public CardType Card { get; }

    public PlayCardCommand(int playerIndex, CardType card)
    {
        PlayerIndex = playerIndex;
        Card = card;
    }
}