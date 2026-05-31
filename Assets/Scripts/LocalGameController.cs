using UnityEngine;

public class LocalGameController : MonoBehaviour
{
    [SerializeField] private string humanPlayerName = "Toi";
    [SerializeField] private string ghostPlayerName = "Fantôme";
    [SerializeField] private float ghostPlayDelay = 1f;
    [SerializeField] private LocalGamePresentation presentation;
    [SerializeField] private LocalBotTurnRunner botTurnRunner;

    private GameState state;
    private GameConfig config;
    private GameEngine engine;

    public GameState State => state;

    private void Awake()
    {
        var botMemoryService = new BotMemoryService();
        var roundService = new RoundService( botMemoryService);
        var endRoundService = new EndRoundService();
        var turnService = new TurnService(roundService, endRoundService);
        var targetingService = new TargetingService();
        var cardEffectService = new CardEffectService(roundService, botMemoryService);

        engine = new GameEngine(
            roundService,
            turnService,
            targetingService,
            cardEffectService,
            botMemoryService
        );
    }
    private void Start()
    {
        config = new GameConfig
        {
            HumanPlayerName = humanPlayerName,
            GhostPlayerName = ghostPlayerName,
            TwoPlayersMode = true
        };

        state = new GameState
        {
            Players = new[]
            {
                new LocalPlayerState(humanPlayerName, false),
                new LocalPlayerState(ghostPlayerName, true)
            }
        };

        Apply(engine.StartRound(state, config));
    }

    public void PlayHumanCard(CardType card)
    {
        if (state.RoundFinished) return;
        if (state.CurrentTurnIndex != 0) return;
        Apply(engine.Execute(new PlayCardCommand(0, card), state));
    }

    public void ConfirmTargetSelection(int targetIndex)
    {
        Apply(engine.Execute(new SelectTargetCommand(targetIndex), state));
    }

    public void ConfirmGuardGuess(CardType guessedCard)
    {
        Apply(engine.Execute(new SelectGuardGuessCommand(guessedCard), state));
    }

    public void RestartRound()
    {
        Apply(engine.StartRound(state, config));
    }

    private void Apply(GameFlowResult result)
    {
        float presentationDelay = 0f;

        if (presentation != null)
            presentationDelay = presentation.Apply(state, result);

        if (result != null && result.ShouldTriggerBotTurn && botTurnRunner != null)
            botTurnRunner.TryRunBotTurn(this, state, engine, ghostPlayDelay + presentationDelay);
    }

    public void ApplyBotResult(GameFlowResult result)
    {
        Apply(result);
    }
}