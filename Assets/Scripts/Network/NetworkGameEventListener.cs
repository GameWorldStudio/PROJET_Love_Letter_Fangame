using UnityEngine;

public class NetworkGameEventListener : MonoBehaviour
{
    [SerializeField] private NetworkGamePresenter presenter;

    private void OnEnable()
    {
        if (presenter != null)
            presenter.OnGameEventReceived += HandleEvent;
    }

    private void OnDisable()
    {
        if (presenter != null)
            presenter.OnGameEventReceived -= HandleEvent;
    }

    private void HandleEvent(LoveLetterNetworkEvent gameEvent)
    {
        switch (gameEvent.EventType)
        {
            case LoveLetterNetworkEventType.CardPlayed:
                HandleCardPlayed(gameEvent);
                break;

            case LoveLetterNetworkEventType.PlayerEliminated:
                HandlePlayerEliminated(gameEvent);
                break;

            case LoveLetterNetworkEventType.CardsRevealed:
                HandleCardsRevealed(gameEvent);
                break;

            case LoveLetterNetworkEventType.RoundStarted:
                Debug.Log("FX/UI : démarrage de manche");
                break;

            case LoveLetterNetworkEventType.RoundEnded:
                Debug.Log("FX/UI : fin de manche");
                break;
        }
    }

    private void HandleCardPlayed(LoveLetterNetworkEvent gameEvent)
    {
        CardType card = (CardType)gameEvent.CardValue;
        Debug.Log($"FX/UI : joueur {gameEvent.PlayerIndex} joue {card}");
    }

    private void HandlePlayerEliminated(LoveLetterNetworkEvent gameEvent)
    {
        Debug.Log($"FX/UI : joueur {gameEvent.PlayerIndex} éliminé");
    }

    private void HandleCardsRevealed(LoveLetterNetworkEvent gameEvent)
    {
        for (int i = 0; i < gameEvent.CardValues.Length; i++)
        {
            int playerIndex = gameEvent.RelatedPlayerIndexes[i];
            CardType card = (CardType)gameEvent.CardValues[i];
            Debug.Log($"FX/UI : reveal joueur {playerIndex} => {card}");
        }
    }
}