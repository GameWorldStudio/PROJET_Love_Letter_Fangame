using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocalGameUI : MonoBehaviour
{
    public static LocalGameUI Instance;

    [Header("References")]
    [SerializeField] private LocalGameController controller;

    [Header("Texts")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text deckText;
    [SerializeField] private TMP_Text yourNameText;
    [SerializeField] private TMP_Text ghostNameText;
    [SerializeField] private TMP_Text logText;

    [Header("Containers")]
    [SerializeField] private Transform yourHandContainer;
    [SerializeField] private Transform ghostHandContainer;
    [SerializeField] private Transform guessContainer;

    [Header("Played Cards")]
    [SerializeField] private Transform yourPlayedContainer;
    [SerializeField] private Transform ghostPlayedContainer;

    [Header("Prefabs")]
    [SerializeField] private CardButtonUI playedCardPrefab;
    [SerializeField] private CardButtonUI opponentHandCardPrefab;
    [SerializeField] private CardButtonUI cardButtonPrefab;
    [SerializeField] private CardButtonUI cardButtonPrefabGuard;

    [Header("Buttons")]
    [SerializeField] private Button restartRoundButton;

    [Header("Burned Cards")]
    [SerializeField] private Transform burnedCardsContainer;
    [SerializeField] private CardButtonUI burnedCardPrefab;

    [Header("Pending Action UI")]
    [SerializeField] private TMP_Text pendingChoiceTitleText;
    [SerializeField] private Transform pendingChoiceContainer;
    [SerializeField] private CardButtonUI playerButton;

    private readonly Dictionary<int, CardType> temporarilyRevealedCards = new();
    private Coroutine revealPlayersCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (controller == null)
            controller = FindFirstObjectByType<LocalGameController>();

        if (restartRoundButton != null)
            restartRoundButton.onClick.AddListener(() => controller?.RestartRound());
    }

    public void Refresh(GameState game)
    {
        if (game == null)
            return;

        RenderHeader(game);
        RenderHands(game);
        RenderPlayedCards(game);
        RenderBurnedCards(game);
        RenderPendingTargetSelection(game);
        RenderPendingGuardGuessSelection(game);
        RenderLogs(game);
    }

    private void RenderPendingTargetSelection(GameState game)
    {
        ClearContainer(pendingChoiceContainer);

        if (pendingChoiceTitleText != null)
            pendingChoiceTitleText.gameObject.SetActive(false);

        bool show =
            game.PendingAction.actionType == PendingActionType.SelectTarget &&
            !game.RoundFinished &&
            game.IsLocalPlayersTurn;

        if (!show)
            return;

        if (pendingChoiceTitleText != null)
        {
            pendingChoiceTitleText.gameObject.SetActive(true);
            pendingChoiceTitleText.text = $"Choisis une cible pour {game.PendingAction.sourceCard}";
        }

        List<int> validTargets = GetValidTargets(game, game.PendingAction.casterIndex, game.PendingAction.sourceCard);

        foreach (int targetIndex in validTargets)
        {
            CardButtonUI button = Instantiate(playerButton, pendingChoiceContainer);

            button.Setup(
                CardType.Guard,
                null,
                true,
                (_) => controller?.ConfirmTargetSelection(targetIndex)
            );

            button.SetCustomLabel(GetPlayerDisplayName(game, targetIndex));
        }
    }

    private void RenderPendingGuardGuessSelection(GameState game)
    {
        ClearContainer(guessContainer);

        if (pendingChoiceTitleText != null)
            pendingChoiceTitleText.gameObject.SetActive(false);

        bool show =
            game.PendingAction.actionType == PendingActionType.SelectGuardGuess &&
            !game.RoundFinished &&
            game.IsLocalPlayersTurn;

        if (!show)
            return;

        if (pendingChoiceTitleText != null)
        {
            pendingChoiceTitleText.gameObject.SetActive(true);
            pendingChoiceTitleText.text = "Choisis une carte à annoncer";
        }

        List<CardType> guessableCards = new()
        {
            CardType.Priest,
            CardType.Baron,
            CardType.Handmaid,
            CardType.Prince,
            CardType.King,
            CardType.Countess,
            CardType.Princess
        };

        foreach (CardType cardType in guessableCards)
        {
            CardButtonUI button = Instantiate(cardButtonPrefabGuard, guessContainer);

            button.Setup(
                cardType,
                CardSpriteLibrary.Instance.GetCardSprite(cardType),
                true,
                selectedCard => controller?.ConfirmGuardGuess(selectedCard)
            );
        }
    }

    private void RenderBurnedCards(GameState game)
    {
        ClearContainer(burnedCardsContainer);

        foreach (CardType card in game.VisibleBurnedCards)
        {
            CardButtonUI cardUI = Instantiate(burnedCardPrefab, burnedCardsContainer);
            cardUI.Setup(card, CardSpriteLibrary.Instance.GetCardSprite(card), false, null);
        }
    }

    private void RenderHeader(GameState game)
    {
        PlayerState human = game.Players[game.LocalPlayerIndex];
        PlayerState ghost = GetGhostPlayer(game);

        if (yourNameText != null)
            yourNameText.text = $"Toi : {human.playerName}";

        if (ghostNameText != null && ghost != null)
            ghostNameText.text = $"Fantôme : {ghost.playerName}";

        if (deckText != null)
            deckText.text = $"Cartes restantes : {game.RemainingDeckCount}";

        if (scoreText != null && ghost != null)
            scoreText.text = $"{human.playerName} {human.score} - {ghost.score} {ghost.playerName}";

        if (statusText != null)
        {
            if (game.RoundFinished)
            {
                statusText.text = "Manche terminée";
            }
            else if (game.PendingAction.actionType == PendingActionType.SelectGuardGuess && game.IsLocalPlayersTurn)
            {
                statusText.text = "Choisis une carte pour la Garde";
            }
            else if (game.PendingAction.actionType == PendingActionType.SelectTarget && game.IsLocalPlayersTurn)
            {
                statusText.text = $"Choisis une cible pour {game.PendingAction.sourceCard}";
            }
            else if (game.IsLocalPlayersTurn)
            {
                statusText.text = "À ton tour";
            }
            else
            {
                statusText.text = "Tour du fantôme";
            }
        }
    }

    private void RenderPlayedCards(GameState game)
    {
        ClearContainer(yourPlayedContainer);
        ClearContainer(ghostPlayedContainer);

        PlayerState human = game.Players[game.LocalPlayerIndex];
        PlayerState ghost = GetGhostPlayer(game);

        foreach (CardType card in human.discard)
        {
            CardButtonUI ui = Instantiate(playedCardPrefab, yourPlayedContainer);
            ui.Setup(card, CardSpriteLibrary.Instance.GetCardSprite(card), false, null);
        }

        if (ghost != null)
        {
            foreach (CardType card in ghost.discard)
            {
                CardButtonUI ui = Instantiate(playedCardPrefab, ghostPlayedContainer);
                ui.Setup(card, CardSpriteLibrary.Instance.GetCardSprite(card), false, null);
            }
        }
    }

    private void RenderHands(GameState game)
    {
        ClearContainer(yourHandContainer);
        ClearContainer(ghostHandContainer);

        PlayerState human = game.Players[game.LocalPlayerIndex];
        PlayerState ghost = GetGhostPlayer(game);

        bool canPlay =
            !game.RoundFinished &&
            game.PendingAction.actionType == PendingActionType.None &&
            game.IsLocalPlayersTurn;

        foreach (CardType card in human.hand)
        {
            CardButtonUI button = Instantiate(cardButtonPrefab, yourHandContainer);
            button.Setup(
                card,
                CardSpriteLibrary.Instance.GetCardSprite(card),
                canPlay,
                OnPlayCardClicked
            );
        }

        if (ghost == null)
            return;

        int ghostIndex = GetGhostPlayerIndex(game);

        for (int i = 0; i < ghost.hand.Count; i++)
        {
            CardButtonUI button = Instantiate(opponentHandCardPrefab, ghostHandContainer);

            if (TryGetPersistentlyRevealedCard(game, ghostIndex, out CardType persistentCard) && i == 0)
            {
                button.Setup(
                    persistentCard,
                    CardSpriteLibrary.Instance.GetCardSprite(persistentCard),
                    false,
                    null
                );
            }
            else if (TryGetTemporarilyRevealedCard(ghostIndex, out CardType revealedCard) && i == 0)
            {
                button.Setup(
                    revealedCard,
                    CardSpriteLibrary.Instance.GetCardSprite(revealedCard),
                    false,
                    null
                );
            }
            else
            {
                button.SetupHidden(CardSpriteLibrary.Instance.GetHiddenSprite());
            }
        }
    }

    private bool TryGetPersistentlyRevealedCard(GameState game, int playerIndex, out CardType card)
    {
        card = CardType.None;

        if (game == null || game.PersistentlyRevealedCards == null)
            return false;

        return game.PersistentlyRevealedCards.TryGetValue(playerIndex, out card);
    }

    private void RenderLogs(GameState game)
    {
        if (logText == null)
            return;

        logText.text = string.Join("\n", game.Logs);
    }

    private void OnPlayCardClicked(CardType cardType)
    {
        controller?.PlayHumanCard(cardType);
    }

    private void ClearContainer(Transform target)
    {
        if (target == null)
            return;

        for (int i = target.childCount - 1; i >= 0; i--)
            Destroy(target.GetChild(i).gameObject);
    }

    public void RevealPlayerCardsTemporarily(Dictionary<int, CardType> revealedCards, float duration = 2.5f)
    {
        temporarilyRevealedCards.Clear();

        foreach (var kvp in revealedCards)
            temporarilyRevealedCards[kvp.Key] = kvp.Value;

        if (revealPlayersCoroutine != null)
            StopCoroutine(revealPlayersCoroutine);

        Refresh(controller != null ? controller.State : null);
        revealPlayersCoroutine = StartCoroutine(HideRevealedPlayerCardsAfterDelay(duration));
    }

    private IEnumerator HideRevealedPlayerCardsAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);

        temporarilyRevealedCards.Clear();
        Refresh(controller != null ? controller.State : null);
        revealPlayersCoroutine = null;
    }

    private bool TryGetTemporarilyRevealedCard(int playerIndex, out CardType card)
    {
        return temporarilyRevealedCards.TryGetValue(playerIndex, out card);
    }

    private string GetPlayerDisplayName(GameState game, int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= game.Players.Length)
            return $"Joueur {playerIndex}";

        return game.Players[playerIndex].playerName;
    }

    private List<int> GetValidTargets(GameState game, int casterIndex, CardType sourceCard)
    {
        List<int> result = new();

        for (int i = 0; i < game.Players.Length; i++)
        {
            if (game.Players[i].isEliminated || game.Players[i].isProtected)
                continue;

            bool isSelf = i == casterIndex;

            switch (sourceCard)
            {
                case CardType.Prince:
                    result.Add(i);
                    break;

                case CardType.Guard:
                case CardType.Priest:
                case CardType.Baron:
                case CardType.King:
                    if (!isSelf)
                        result.Add(i);
                    break;
            }
        }

        return result;
    }

    private PlayerState GetGhostPlayer(GameState game)
    {
        int ghostIndex = GetGhostPlayerIndex(game);
        return ghostIndex >= 0 ? game.Players[ghostIndex] : null;
    }

    private int GetGhostPlayerIndex(GameState game)
    {
        for (int i = 0; i < game.Players.Length; i++)
        {
            if (i == game.LocalPlayerIndex)
                continue;

            if (game.Players[i].isGhost)
                return i;
        }

        for (int i = 0; i < game.Players.Length; i++)
        {
            if (i != game.LocalPlayerIndex)
                return i;
        }

        return -1;
    }
}