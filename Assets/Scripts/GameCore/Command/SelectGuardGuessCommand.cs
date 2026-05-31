public class SelectGuardGuessCommand
{
    public CardType GuessedCard { get; }

    public SelectGuardGuessCommand(CardType guessedCard)
    {
        GuessedCard = guessedCard;
    }
}