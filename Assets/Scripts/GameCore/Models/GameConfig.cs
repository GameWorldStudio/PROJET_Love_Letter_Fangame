public class GameConfig
{
    public string HumanPlayerName { get; set; } = "Toi";
    public string GhostPlayerName { get; set; } = "Fantôme";
    public bool TwoPlayersMode { get; set; } = true;
    public int VisibleBurnedCardsInTwoPlayers { get; set; } = 3;
}