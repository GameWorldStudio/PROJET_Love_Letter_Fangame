using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalGamePresentation : MonoBehaviour
{
    [SerializeField] private GameObject deckObject;
    [SerializeField] private GameObject hiddenSideDeckCard;

    private Coroutine pendingRefreshCoroutine;

    public float Apply(GameState state, GameFlowResult result)
    {
        if (state == null)
            return 0f;

        if (deckObject != null)
            deckObject.SetActive(state.RemainingDeckCount > 0);

        if (hiddenSideDeckCard != null)
            hiddenSideDeckCard.SetActive(state.HasHiddenBurnedCard);

        float blockingDelay = 0f;
        bool hasRevealEvent = false;

        if (result != null && result.Events != null)
        {
            foreach (var gameEvent in result.Events)
            {
                if (gameEvent is CardsRevealedEvent revealed)
                {
                    hasRevealEvent = true;
                    blockingDelay = Mathf.Max(blockingDelay, revealed.SuggestedDuration);

                    var revealedDict = new Dictionary<int, CardType>();

                    if (revealed.RevealedCards != null)
                    {
                        foreach (var entry in revealed.RevealedCards)
                            revealedDict[entry.PlayerIndex] = entry.Card;
                    }

                    LocalGameUI.Instance?.RevealPlayerCardsTemporarily(
                        revealedDict,
                        revealed.SuggestedDuration
                    );
                }
            }
        }

        if (hasRevealEvent)
        {
            if (pendingRefreshCoroutine != null)
                StopCoroutine(pendingRefreshCoroutine);

            pendingRefreshCoroutine = StartCoroutine(RefreshAfterDelay(state, blockingDelay));
        }
        else
        {
            if (result == null || result.ShouldRefreshUi)
                LocalGameUI.Instance?.Refresh(state);
        }

        return blockingDelay;
    }

    private IEnumerator RefreshAfterDelay(GameState state, float delay)
    {
        yield return new WaitForSeconds(delay);

        LocalGameUI.Instance?.Refresh(state);
        pendingRefreshCoroutine = null;
    }
}