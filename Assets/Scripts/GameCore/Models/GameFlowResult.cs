using System.Collections.Generic;

public class GameFlowResult
{
    public bool StateChanged { get; set; }
    public bool ShouldRefreshUi { get; set; }
    public bool ShouldTriggerBotTurn { get; set; }
    public List<GameEvent> Events { get; } = new();
}