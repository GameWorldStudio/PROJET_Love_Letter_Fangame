using System.Collections.Generic;
using UnityEngine;

public static class LoveLetterGhostAI
{
    public static CardType ChooseCardToPlay(List<CardType> hand)
    {
        if (hand == null || hand.Count == 0)
            return CardType.Guard;

        bool hasCountess = hand.Contains(CardType.Countess);
        bool hasPrinceOrKing = hand.Contains(CardType.Prince) || hand.Contains(CardType.King);

        if (hasCountess && hasPrinceOrKing)
            return CardType.Countess;

        return hand[0];
    }

    public static CardType ChooseGuardGuess()
    {
        int value = Random.Range(2, 9);
        return (CardType)value;
    }

    public static int ChooseTarget(List<int> validTargets)
    {
        if (validTargets == null || validTargets.Count == 0)
            return -1;

        int randomIndex = Random.Range(0, validTargets.Count);
        return validTargets[randomIndex];
    }
}