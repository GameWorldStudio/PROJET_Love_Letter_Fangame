using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



public enum AiActionType
{
    PlayCard,
    PlayCardWithTarget,
    PlayGuardGuess
}

[Serializable]
public class AiDecision
{
    public CardType cardToPlay;
    public int targetPlayerIndex = -1;
    public CardType guessedCard = CardType.None;
    public float score = float.MinValue;
    public string reason = "";
}

[Serializable]
public class RevealedCardInfo
{
    public int playerIndex;
    public CardType card;
    public bool stillValid = true;
}

/// <summary>
/// Représentation minimale attendue côté IA.
/// Adapte cette classe à ton modèle existant si besoin.
/// </summary>
public class AiGameState
{
    public List<AiPlayerState> Players = new List<AiPlayerState>();

    /// <summary>Défausse globale de la manche.</summary>
    public List<CardType> DiscardedCards = new List<CardType>();

    /// <summary>Cartes visibles mises de côté en partie à 2 joueurs.</summary>
    public List<CardType> VisibleSideCards = new List<CardType>();

    /// <summary>Infos connues par effets type Prêtre / révélations fin de manche / Baron.</summary>
    public List<RevealedCardInfo> RevealedHandInfos = new List<RevealedCardInfo>();

    public int CurrentTurnIndex;
    public bool IsTwoPlayersMode;

    /// <summary>
    /// Si tu as déjà une règle côté moteur, garde-la là-bas.
    /// L’IA suppose ici que les coups générés sont jouables.
    /// </summary>
    public bool IsRoundFinished;

    public Dictionary<int, int> KnownByCounts = new Dictionary<int, int>();

    public int HiddenCardCount = 0;
}

[Serializable]
public class AiPlayerState
{
    public int PlayerIndex;
    public List<CardType> Hand = new List<CardType>(); // normalement 2 cartes au tour du joueur
    public bool IsEliminated;
    public bool IsProtected;
    public bool IsHuman;

}

/// <summary>
/// Connaissance reconstruite par l’IA.
/// </summary>
public class AiKnowledge
{
    public Dictionary<CardType, int> RemainingCounts = new Dictionary<CardType, int>();
    public Dictionary<int, CardType?> KnownPlayerCard = new Dictionary<int, CardType?>();
    public Dictionary<int, Dictionary<CardType, float>> EstimatedPlayerCardProbabilities = new Dictionary<int, Dictionary<CardType, float>>();
    public HashSet<CardType> VisibleSideCards = new HashSet<CardType>();

    public bool IsMyCardKnown = false;
    public CardType KnownMyCard = CardType.None;
    public int KnownByPlayerCount = 0;
}

/// <summary>
/// IA Love Letter basée sur un système de génération de coups + scoring.
/// </summary>
public class LoveLetterAiService
{
    public static readonly Dictionary<CardType, int> BaseDeckCounts = new Dictionary<CardType, int>
    {
        { CardType.Guard, 5 },
        { CardType.Priest, 2 },
        { CardType.Baron, 2 },
        { CardType.Handmaid, 2 },
        { CardType.Prince, 2 },
        { CardType.King, 1 },
        { CardType.Countess, 1 },
        { CardType.Princess, 1 }
    };

    public AiDecision ChooseBestMove(AiGameState game, int aiPlayerIndex)
    {
        var legalMoves = GenerateLegalMoves(game, aiPlayerIndex);
        if (legalMoves.Count == 0)
        {
            Debug.LogWarning($"[AI] Aucun coup généré pour le joueur {aiPlayerIndex}. Etat probablement incohérent.");
            return null;
        }

        var knowledge = BuildKnowledge(game, aiPlayerIndex);

        AiDecision best = null;

        foreach (var move in legalMoves)
        {
            if (move.cardToPlay == CardType.Guard && move.targetPlayerIndex >= 0)
                move.guessedCard = ChooseBestGuardGuess(knowledge, move.targetPlayerIndex);

            move.score = EvaluateMove(game, knowledge, aiPlayerIndex, move);

            Debug.Log(
                $"[AI] Move={move.cardToPlay} " +
                $"Target={move.targetPlayerIndex} " +
                $"Guess={move.guessedCard} " +
                $"Score={move.score:F2} " +
                $"Reason={move.reason}"
            );

            if (best == null || move.score > best.score)
                best = move;
        }

        return best;
    }

    #region Generate Legal Moves

    private List<AiDecision> GenerateLegalMoves(AiGameState game, int aiPlayerIndex)
    {
        var results = new List<AiDecision>();
        var player = game.Players[aiPlayerIndex];

        if (player == null || player.IsEliminated || player.Hand == null || player.Hand.Count == 0)
            return results;

        List<CardType> playableCards = GetPlayableCardsRespectingRules(player.Hand);

        foreach (var card in playableCards)
        {
            if (!CardNeedsTarget(card))
            {
                results.Add(new AiDecision
                {
                    cardToPlay = card,
                    targetPlayerIndex = -1
                });

                continue;
            }

            var validTargets = GetValidTargets(game, aiPlayerIndex, card);

            if (validTargets.Count > 0)
            {
                foreach (int targetIndex in validTargets)
                {
                    results.Add(new AiDecision
                    {
                        cardToPlay = card,
                        targetPlayerIndex = targetIndex
                    });
                }
            }
            else
            {
                // Aucun joueur ciblable :
                // on génère quand même le play "dans le vide"
                // sauf pour le Prince qui doit idéalement se jouer sur soi.
                if (card == CardType.Prince)
                {
                    results.Add(new AiDecision
                    {
                        cardToPlay = card,
                        targetPlayerIndex = aiPlayerIndex
                    });
                }
                else
                {
                    results.Add(new AiDecision
                    {
                        cardToPlay = card,
                        targetPlayerIndex = -1
                    });
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Applique ici les contraintes de règles fortes :
    /// - Comtesse obligatoire si Comtesse + Roi/Prince
    /// </summary>
    private List<CardType> GetPlayableCardsRespectingRules(List<CardType> hand)
    {
        var playable = new List<CardType>(hand);

        bool hasCountess = hand.Contains(CardType.Countess);
        bool hasKing = hand.Contains(CardType.King);
        bool hasPrince = hand.Contains(CardType.Prince);

        if (hasCountess && (hasKing || hasPrince))
        {
            return new List<CardType> { CardType.Countess };
        }

        return playable;
    }

    private bool CardNeedsTarget(CardType card)
    {
        switch (card)
        {
            case CardType.Guard:
            case CardType.Priest:
            case CardType.Baron:
            case CardType.Prince:
            case CardType.King:
                return true;

            default:
                return false;
        }
    }

    private List<int> GetValidTargets(AiGameState game, int aiPlayerIndex, CardType card)
    {
        var validTargets = new List<int>();

        for (int i = 0; i < game.Players.Count; i++)
        {
            var player = game.Players[i];

            if (player.IsEliminated)
                continue;

            // Prince peut se cibler soi-même
            bool selfAllowed = (card == CardType.Prince);

            if (i == aiPlayerIndex && !selfAllowed)
                continue;

            // joueurs protégés non ciblables
            if (player.IsProtected)
                continue;

            validTargets.Add(i);
        }

        // Cas spécial : le Prince doit pouvoir se jouer sur soi si aucun autre joueur valide
        if (card == CardType.Prince)
        {
            var self = game.Players[aiPlayerIndex];
            bool selfAlreadyIn = validTargets.Contains(aiPlayerIndex);

            if (!self.IsEliminated && !selfAlreadyIn)
                validTargets.Add(aiPlayerIndex);
        }

        return validTargets;
    }

    #endregion

    #region Knowledge

    private AiKnowledge BuildKnowledge(AiGameState game, int aiPlayerIndex)
    {
        AiKnowledge k = new AiKnowledge();

        foreach (var kvp in BaseDeckCounts)
            k.RemainingCounts[kvp.Key] = kvp.Value;

        // 1. Défausse globale
        foreach (var discarded in game.DiscardedCards)
            DecrementIfExists(k.RemainingCounts, discarded);

        // 2. Cartes visibles de côté (parties à 2)
        foreach (var side in game.VisibleSideCards)
        {
            k.VisibleSideCards.Add(side);
            DecrementIfExists(k.RemainingCounts, side);
        }

        // 3. Main de l'IA
        var aiPlayer = game.Players[aiPlayerIndex];
        foreach (var card in aiPlayer.Hand)
            DecrementIfExists(k.RemainingCounts, card);

        // 4. Cartes connues / révélées encore valides
        foreach (var info in game.RevealedHandInfos)
        {
            if (!info.stillValid)
                continue;

            if (info.playerIndex < 0 || info.playerIndex >= game.Players.Count)
                continue;

            if (game.Players[info.playerIndex].IsEliminated)
                continue;

            k.KnownPlayerCard[info.playerIndex] = info.card;
        }

        // 5. Carte de l'IA connue de un ou des joueurs
        if (game.KnownByCounts != null && game.KnownByCounts.TryGetValue(aiPlayerIndex, out int knownCount))
        {
            k.KnownByPlayerCount = knownCount;

            if (knownCount > 0 && game.Players[aiPlayerIndex].Hand != null && game.Players[aiPlayerIndex].Hand.Count > 0)
            {
                // Après retrait de la carte jouée, le bot garde normalement une seule carte "réelle" importante.
                // Ici on prend la meilleure approximation : si une carte est connue de l'extérieur,
                // on considère que l'une des cartes en main est exposée.
                // Le scoring précis se fera sur "l'autre carte" quand on évalue le coup.
                k.IsMyCardKnown = true;
            }
        }

        // Si l'IA a une carte précisément connue (ex: vue avec Prêtre), on la récupère
        if (k.KnownPlayerCard.TryGetValue(aiPlayerIndex, out var knownMyCard) && knownMyCard.HasValue)
        {
            k.KnownMyCard = knownMyCard.Value;

            // Une Garde connue n'est pas vraiment problématique dans ta règle métier
            if (knownMyCard.Value != CardType.Guard)
                k.IsMyCardKnown = true;
        }

        // 5. Probabilités par joueur
        BuildProbabilities(game, aiPlayerIndex, k);

        return k;
    }

    private float EvaluateExposurePenalty(AiKnowledge k, CardType remainingCard)
    {
        if (!k.IsMyCardKnown || k.KnownByPlayerCount <= 0)
            return 0f;

        float penalty = 0f;

        switch (remainingCard)
        {
            case CardType.Princess:
                penalty = 14f;
                break;
            case CardType.Countess:
                penalty = 9f;
                break;
            case CardType.King:
                penalty = 8f;
                break;
            case CardType.Prince:
                penalty = 7f;
                break;
            case CardType.Baron:
                penalty = 5f;
                break;
            case CardType.Priest:
                penalty = 4f;
                break;
            case CardType.Handmaid:
                penalty = 1f; // si on garde Servante, ce n’est pas trop grave
                break;
            case CardType.Guard:
                penalty = 2f;
                break;
        }

        // plus de joueurs au courant = plus dangereux
        penalty *= 1f + (k.KnownByPlayerCount - 1) * 0.5f;

        return penalty;
    }

    private void BuildProbabilities(AiGameState game, int aiPlayerIndex, AiKnowledge k)
    {
        foreach (var player in game.Players)
        {
            if (player.PlayerIndex == aiPlayerIndex || player.IsEliminated)
                continue;

            var probs = new Dictionary<CardType, float>();

            // Si on connaît exactement la carte du joueur, proba = 100%
            if (k.KnownPlayerCard.TryGetValue(player.PlayerIndex, out var known) && known.HasValue)
            {
                foreach (CardType ct in Enum.GetValues(typeof(CardType)))
                {
                    if (ct == CardType.None)
                        continue;

                    probs[ct] = (ct == known.Value) ? 1f : 0f;
                }

                k.EstimatedPlayerCardProbabilities[player.PlayerIndex] = probs;
                continue;
            }

            // Nombre total de cartes encore inconnues dans l'univers restant
            int totalUnknownCards = 0;
            foreach (var kvp in k.RemainingCounts)
            {
                if (kvp.Value > 0)
                    totalUnknownCards += kvp.Value;
            }

            // Combien de joueurs adverses inconnus sont encore en lice
            int unknownOpponentCount = 0;
            foreach (var other in game.Players)
            {
                if (other.PlayerIndex == aiPlayerIndex || other.IsEliminated)
                    continue;

                bool isKnown =
                    k.KnownPlayerCard.TryGetValue(other.PlayerIndex, out var otherKnown)
                    && otherKnown.HasValue;

                if (!isKnown)
                    unknownOpponentCount++;
            }

            // Nombre de "slots" inconnus qui peuvent contenir une carte :
            // - mains adverses inconnues
            // - carte cachée brûlée
            int hiddenSlots = Mathf.Max(0, game.HiddenCardCount);
            int totalSlots = unknownOpponentCount + hiddenSlots;

            // Sécurité : si jamais totalSlots == 0, on retombe sur une distribution simple
            if (totalSlots <= 0 || totalUnknownCards <= 0)
            {
                foreach (CardType ct in Enum.GetValues(typeof(CardType)))
                {
                    if (ct == CardType.None)
                        continue;

                    probs[ct] = 0f;
                }

                k.EstimatedPlayerCardProbabilities[player.PlayerIndex] = probs;
                continue;
            }

            Debug.Log($"[AI] BuildProbabilities target={player.PlayerIndex} totalUnknownCards={totalUnknownCards} totalSlots={totalSlots} hiddenCards={game.HiddenCardCount}");

            foreach (CardType ct in Enum.GetValues(typeof(CardType)))
            {
                if (ct == CardType.None)
                    continue;

                int count = k.RemainingCounts.ContainsKey(ct) ? k.RemainingCounts[ct] : 0;

                if (count <= 0)
                {
                    probs[ct] = 0f;
                    continue;
                }

                // Hypothèse simple mais juste pour Love Letter :
                // chaque carte restante a autant de chances d'être dans n'importe quel slot inconnu
                // donc P(carte chez CE joueur) = count / totalUnknownCards
                float probability = (float)count / totalUnknownCards;
                probs[ct] = probability;

                Debug.Log($"[AI]  -> card={ct}, remaining={count}, p={probability:F4}");
            }

            k.EstimatedPlayerCardProbabilities[player.PlayerIndex] = probs;
        }
    }

    private void NormalizeProbabilities(Dictionary<CardType, float> probs)
    {
        float sum = probs.Values.Sum();

        if (sum <= 0f)
            return;

        var keys = probs.Keys.ToList();
        foreach (var key in keys)
            probs[key] /= sum;
    }

    #endregion

    #region Scoring

    private float EvaluateMove(AiGameState game, AiKnowledge k, int aiPlayerIndex, AiDecision move)
    {
        float score = 0f;
        string reason = "";

        CardType remainingCard = GetOtherCardInHand(game.Players[aiPlayerIndex].Hand, move.cardToPlay);

        // Score lié à la cible
        if (move.targetPlayerIndex >= 0)
            score += EvaluateTargetPriority(game, k, aiPlayerIndex, move.targetPlayerIndex);

        switch (move.cardToPlay)
        {
            case CardType.Princess:
                score += EvaluatePrincessMove(game, aiPlayerIndex, move, out reason);
                break;

            case CardType.Countess:
                score += EvaluateCountessMove(game, aiPlayerIndex, move, out reason);
                break;

            case CardType.Guard:
                score += EvaluateGuardMove(game, k, aiPlayerIndex, move, out reason);
                break;

            case CardType.Priest:
                score += EvaluatePriestMove(game, k, aiPlayerIndex, move, out reason);
                break;

            case CardType.Baron:
                score += EvaluateBaronMove(game, k, aiPlayerIndex, move, out reason);
                break;

            case CardType.Handmaid:
                score += EvaluateHandmaidMove(game, k, aiPlayerIndex, move, out reason);
                break;

            case CardType.Prince:
                score += EvaluatePrinceMove(game, k, aiPlayerIndex, move, out reason);
                break;

            case CardType.King:
                score += EvaluateKingMove(game, k, aiPlayerIndex, move, out reason);
                break;

            default:
                reason = "Carte non gérée.";
                break;
        }

        // Nouvelle logique : si la carte qu'on garde est connue, on pénalise
        // sauf si le coup joué protège / change / tue / esquive naturellement déjà via son score.
        float exposurePenalty = EvaluateExposurePenalty(k, remainingCard);

        // La Servante protège justement la carte connue
        if (move.cardToPlay == CardType.Handmaid)
            exposurePenalty *= 0.15f;

        // Le Roi change la main, donc exposition beaucoup moins grave
        if (move.cardToPlay == CardType.King)
            exposurePenalty *= 0.25f;

        // Le Prince sur soi remplace la main
        if (move.cardToPlay == CardType.Prince && move.targetPlayerIndex == aiPlayerIndex)
            exposurePenalty *= 0.1f;

        score -= exposurePenalty;

        if (exposurePenalty > 0f)
            reason += $" | pénalité exposition={exposurePenalty:F1}";

        score += EvaluatePlayingKnownCardBonus(k, move.cardToPlay, remainingCard);
        move.reason = reason;
        return score;
    }

    private float EvaluatePrincessMove(AiGameState game, int aiPlayerIndex, AiDecision move, out string reason)
    {
        // Toujours catastrophique, sauf si c’est le seul coup généré.
        reason = "Princesse à éviter absolument sauf si aucun autre coup n'est possible.";
        return -10000f;
    }

    private float EvaluateCountessMove(AiGameState game, int aiPlayerIndex, AiDecision move, out string reason)
    {
        reason = "Comtesse jouée (souvent contrainte de règle).";
        return 1f;
    }

    private float EvaluateGuardMove(AiGameState game, AiKnowledge k, int aiPlayerIndex, AiDecision move, out string reason)
    {
        float score = 0f;

        if (move.targetPlayerIndex < 0)
        {
            CardType otherCard = GetOtherCardInHand(game.Players[aiPlayerIndex].Hand, CardType.Guard);
            int otherValue = GetCardStrength(otherCard);

            score += 1f;

            if (otherCard == CardType.Princess)
                score += 5f;
            else if (otherValue >= 5)
                score += 3f;

            reason = "Garde jouée sans cible valide pour se débarrasser d'une carte faible.";
            return score;
        }

        var target = game.Players[move.targetPlayerIndex];

        if (target.IsProtected || target.IsEliminated)
        {
            reason = "Cible invalide pour la Garde.";
            return -100f;
        }

        float guessProbability = 0f;
        if (k.EstimatedPlayerCardProbabilities.TryGetValue(move.targetPlayerIndex, out var probs))
        {
            if (probs.TryGetValue(move.guessedCard, out var p))
                guessProbability = p;
        }

        float reward = GuessValue(move.guessedCard);

        score += guessProbability * reward * 10f;

        if (k.KnownPlayerCard.TryGetValue(move.targetPlayerIndex, out var known) && known.HasValue)
        {
            if (known.Value == move.guessedCard)
                score += 50f;
        }

        int aliveCount = CountAlivePlayers(game);
        if (aliveCount <= 2)
            score += 3f;

        if (k.IsMyCardKnown && k.KnownMyCard != CardType.Guard)
        {
            CardType otherCard = GetOtherCardInHand(game.Players[aiPlayerIndex].Hand, CardType.Guard);

            if (otherCard == k.KnownMyCard)
                score -= 3f;
        }

        reason = $"Garde sur J{move.targetPlayerIndex} en annonçant {move.guessedCard} (proba={guessProbability:F2}).";
        return score;
    }

    private float EvaluatePriestMove(AiGameState game, AiKnowledge k, int aiPlayerIndex, AiDecision move, out string reason)
    {
        float score = 4f;

        if (move.targetPlayerIndex < 0)
        {
            CardType otherCard = GetOtherCardInHand(game.Players[aiPlayerIndex].Hand, CardType.Priest);
            int otherValue = GetCardStrength(otherCard);

            score = 0.5f;

            if (otherCard == CardType.Princess)
                score += 4f;
            else if (otherValue >= 5)
                score += 2f;

            reason = "Prêtre joué sans cible valide, principalement pour conserver une meilleure carte.";
            return score;
        }

        int aliveCount = CountAlivePlayers(game);

        if (aliveCount >= 4) score += 3f;
        else if (aliveCount == 3) score += 2f;
        else score += 0.5f;

        if (k.KnownPlayerCard.TryGetValue(move.targetPlayerIndex, out var known) && known.HasValue)
            score -= 4f;

        CardType other = GetOtherCardInHand(game.Players[aiPlayerIndex].Hand, CardType.Priest);
        int myValue = GetCardStrength(other);

        if (myValue >= 5)
            score += 1.5f;

        reason = $"Prêtre sur J{move.targetPlayerIndex} pour gagner de l'information.";
        return score;
    }

    private float EvaluateBaronMove(AiGameState game, AiKnowledge k, int aiPlayerIndex, AiDecision move, out string reason)
    {
        CardType myOtherCard = GetOtherCardInHand(game.Players[aiPlayerIndex].Hand, CardType.Baron);
        int myValue = GetCardStrength(myOtherCard);

        if (move.targetPlayerIndex < 0)
        {
            float score = -1f;

            // Si on a une petite carte, jeter le Baron peut être correct
            if (myValue <= 2)
                score += 2f;
            else if (myValue >= 6)
                score -= 2f;

            reason = "Baron joué sans cible valide ; défausse opportuniste.";
            return score;
        }

        float totalScore = 0f;

        if (myOtherCard == CardType.Princess)
            totalScore += 2f;

        if (k.KnownPlayerCard.TryGetValue(move.targetPlayerIndex, out var known) && known.HasValue)
        {
            int enemyValue = GetCardStrength(known.Value);

            if (myValue > enemyValue)
            {
                totalScore += 12f;
                reason = $"Baron favorable certain contre {known.Value}.";
            }
            else if (myValue < enemyValue)
            {
                totalScore -= 12f;
                reason = $"Baron défavorable certain contre {known.Value}.";
            }
            else
            {
                totalScore -= 1f;
                reason = $"Baron neutre contre {known.Value}.";
            }

            return totalScore;
        }

        float winChance = 0f;
        float loseChance = 0f;

        if (k.EstimatedPlayerCardProbabilities.TryGetValue(move.targetPlayerIndex, out var probs))
        {
            foreach (var kvp in probs)
            {
                int enemyValue = GetCardStrength(kvp.Key);

                if (myValue > enemyValue) winChance += kvp.Value;
                else if (myValue < enemyValue) loseChance += kvp.Value;
            }
        }

        totalScore += winChance * 10f;
        totalScore -= loseChance * 12f;

        reason = $"Baron sur J{move.targetPlayerIndex} avec chance victoire={winChance:F2}, défaite={loseChance:F2}.";
        return totalScore;
    }

    private float EvaluateHandmaidMove(AiGameState game, AiKnowledge k, int aiPlayerIndex, AiDecision move, out string reason)
    {
        float score = 3f;

        CardType otherCard = GetOtherCardInHand(game.Players[aiPlayerIndex].Hand, CardType.Handmaid);
        int otherValue = GetCardStrength(otherCard);
        int aliveCount = CountAlivePlayers(game);

        if (otherValue >= 6) score += 6f;
        else if (otherValue >= 5) score += 4f;
        else if (otherValue >= 3) score += 2f;

        score += Mathf.Clamp(aliveCount - 2, 0, 4);

        // Nouveau : énorme bonus si la carte gardée est connue
        if (k.IsMyCardKnown)
        {
            score += 6f + (k.KnownByPlayerCount - 1) * 2f;

            if (otherCard == CardType.Princess || otherCard == CardType.Prince || otherCard == CardType.King)
                score += 3f;
        }

        reason = $"Servante jouée pour protéger une carte de valeur {otherValue}.";
        return score;
    }

    private float EvaluatePlayingKnownCardBonus(AiKnowledge k, CardType playedCard, CardType remainingCard)
    {
        if (!k.IsMyCardKnown || k.KnownByPlayerCount <= 0)
            return 0f;

        // Si la carte connue est une Garde, on ne force pas de comportement spécial
        if (k.KnownMyCard == CardType.Guard)
            return 0f;

        float bonus = 0f;

        // Très important : si on joue précisément la carte exposée, on est récompensé
        if (playedCard == k.KnownMyCard)
        {
            bonus += 6f + (k.KnownByPlayerCount - 1) * 2f;

            // Encore mieux si on garde une carte qui protège ou qui a de la valeur stratégique
            if (remainingCard == CardType.Handmaid)
                bonus += 2f;

            if (remainingCard == CardType.Prince || remainingCard == CardType.King || remainingCard == CardType.Princess)
                bonus += 1.5f;

            return bonus;
        }

        // Si on garde la carte exposée, légère pénalité supplémentaire
        if (remainingCard == k.KnownMyCard)
        {
            bonus -= 4f + (k.KnownByPlayerCount - 1) * 1.5f;

            // Cas typique que tu décris :
            // je garde une carte connue (ex: Servante) et je joue un Garde spéculatif
            if (playedCard == CardType.Guard)
                bonus -= 3f;
        }

        // Ancienne logique conservée, mais affaiblie
        if (GetCardStrength(playedCard) > GetCardStrength(remainingCard))
            bonus += 1.5f;

        return bonus;
    }

    private float EvaluatePrinceMove(AiGameState game, AiKnowledge k, int aiPlayerIndex, AiDecision move, out string reason)
    {
        float score = 0f;

        bool selfTarget = move.targetPlayerIndex == aiPlayerIndex;
        CardType otherCard = GetOtherCardInHand(game.Players[aiPlayerIndex].Hand, CardType.Prince);

        if (selfTarget)
        {
            score -= 4f;

            if (otherCard == CardType.Princess)
            {
                score -= 500f;
                reason = "Prince sur soi avec Princesse en main : quasi-suicide, à éviter sauf si forcé.";
                return score;
            }

            // Utilité modérée si on veut reroll une main faible
            int value = GetCardStrength(otherCard);
            if (value <= 2) score += 4f;
            else if (value <= 4) score += 1f;
            else score -= 2f;

            reason = $"Prince sur soi pour renouveler une carte de valeur {value}.";
            return score;
        }

        // Sur un adversaire
        score += 6f;

        if (k.KnownPlayerCard.TryGetValue(move.targetPlayerIndex, out var known) && known.HasValue)
        {
            if (known.Value == CardType.Princess)
            {
                score += 100f;
                reason = "Prince sur adversaire tenant la Princesse : élimination immédiate très forte.";
                return score;
            }

            int enemyValue = GetCardStrength(known.Value);
            if (enemyValue >= 5)
                score += 8f;

            reason = $"Prince sur adversaire avec carte connue {known.Value}.";
            return score;
        }

        if (k.EstimatedPlayerCardProbabilities.TryGetValue(move.targetPlayerIndex, out var probs))
        {
            float princessProb = probs.ContainsKey(CardType.Princess) ? probs[CardType.Princess] : 0f;
            float highCardProb =
                GetProbability(probs, CardType.Princess) +
                GetProbability(probs, CardType.Countess) +
                GetProbability(probs, CardType.King) +
                GetProbability(probs, CardType.Prince);

            score += princessProb * 40f;
            score += highCardProb * 10f;

            reason = $"Prince sur J{move.targetPlayerIndex}, princessProb={princessProb:F2}, highCardProb={highCardProb:F2}.";
            return score;
        }

        reason = "Prince sur adversaire sans info précise.";
        return score;
    }

    private float EvaluateKingMove(AiGameState game, AiKnowledge k, int aiPlayerIndex, AiDecision move, out string reason)
    {
        float score = 0f;

        CardType myOtherCard = GetOtherCardInHand(game.Players[aiPlayerIndex].Hand, CardType.King);
        int myValue = GetCardStrength(myOtherCard);

        if (move.targetPlayerIndex < 0)
        {
            score = -2f;

            if (myValue <= 2)
                score += 1f;
            if (myValue >= 6)
                score -= 3f;

            reason = "Roi joué sans cible valide ; coup subi pour se défausser.";
            return score;
        }

        if (myValue >= 7)
            score -= 8f;
        else if (myValue >= 5)
            score -= 3f;
        else if (myValue <= 2)
            score += 4f;

        if (k.KnownPlayerCard.TryGetValue(move.targetPlayerIndex, out var known) && known.HasValue)
        {
            int enemyValue = GetCardStrength(known.Value);
            score += (enemyValue - myValue) * 4f;

            if (known.Value == CardType.Princess)
                score += 20f;

            reason = $"Roi sur carte connue {known.Value} contre ma carte {myOtherCard}.";
            return score;
        }

        if (k.EstimatedPlayerCardProbabilities.TryGetValue(move.targetPlayerIndex, out var probs))
        {
            float expectedValue = 0f;
            foreach (var kvp in probs)
                expectedValue += GetCardStrength(kvp.Key) * kvp.Value;

            score += (expectedValue - myValue) * 3f;

            reason = $"Roi sur J{move.targetPlayerIndex}, valeur attendue adverse={expectedValue:F2}, ma valeur={myValue}.";
            return score;
        }

        reason = "Roi joué sans information.";
        return score;
    }

    private float EvaluateTargetPriority(AiGameState game, AiKnowledge k, int aiPlayerIndex, int targetPlayerIndex)
    {
        if (targetPlayerIndex < 0 || targetPlayerIndex >= game.Players.Count)
            return -100f;

        var target = game.Players[targetPlayerIndex];

        if (target.IsEliminated || target.IsProtected)
            return -100f;

        float score = 1f;

        int aliveCount = CountAlivePlayers(game);
        if (aliveCount <= 2)
            score += 3f;
        else if (aliveCount == 3)
            score += 1.5f;

        if (k.KnownPlayerCard.ContainsKey(targetPlayerIndex))
            score += 4f;

        return score;
    }

    #endregion

    #region Guard Guess

    private CardType ChooseBestGuardGuess(AiKnowledge knowledge, int targetPlayerIndex)
    {
        if (!knowledge.EstimatedPlayerCardProbabilities.TryGetValue(targetPlayerIndex, out var probs))
        {
            Debug.LogWarning($"[AI] Aucun dictionnaire de probas pour la cible {targetPlayerIndex}. Fallback Priest.");
            return CardType.Priest;
        }

        CardType best = CardType.None;
        float bestScore = -1f;

        Debug.Log($"[AI] ---- Guard Guess Debug target={targetPlayerIndex} ----");

        foreach (var kvp in probs)
        {
            if (kvp.Key == CardType.Guard || kvp.Key == CardType.None)
                continue;

            float weightedScore = kvp.Value * GuessValue(kvp.Key);

            Debug.Log($"[AI] GuessCandidate={kvp.Key} proba={kvp.Value:F4} weighted={weightedScore:F4}");

            if (weightedScore > bestScore)
            {
                bestScore = weightedScore;
                best = kvp.Key;
            }
            else if (Mathf.Abs(weightedScore - bestScore) < 0.0001f && best != CardType.None)
            {
                // En cas d'égalité, on préfère la carte la plus forte
                if (GetCardStrength(kvp.Key) > GetCardStrength(best))
                    best = kvp.Key;
            }
        }

        // Sécurité : si tout est à 0 ou aucune meilleure option trouvée,
        // on prend la carte encore possible la plus forte
        if (best == CardType.None || bestScore <= 0f)
        {
            CardType fallback = CardType.Priest;
            int bestStrength = -1;

            foreach (var kvp in knowledge.RemainingCounts)
            {
                if (kvp.Key == CardType.Guard || kvp.Key == CardType.None)
                    continue;

                if (kvp.Value <= 0)
                    continue;

                int strength = GetCardStrength(kvp.Key);
                if (strength > bestStrength)
                {
                    bestStrength = strength;
                    fallback = kvp.Key;
                }
            }

            Debug.LogWarning($"[AI] Toutes les probas de Garde sont nulles. Fallback sur {fallback}.");
            return fallback;
        }

        Debug.Log($"[AI] BestGuardGuess={best} bestScore={bestScore:F4}");
        return best;
    }

    private float GuessValue(CardType card)
    {
        switch (card)
        {
            case CardType.Princess: return 5.0f;
            case CardType.Countess: return 3.5f;
            case CardType.King: return 3.0f;
            case CardType.Prince: return 2.5f;
            case CardType.Handmaid: return 1.8f;
            case CardType.Baron: return 1.6f;
            case CardType.Priest: return 1.2f;
            default: return 1f;
        }
    }

    #endregion

    #region Helpers

    private void DecrementIfExists(Dictionary<CardType, int> dict, CardType card)
    {
        if (!dict.ContainsKey(card))
            return;

        dict[card] = Mathf.Max(0, dict[card] - 1);
    }

    private float GetProbability(Dictionary<CardType, float> probs, CardType card)
    {
        return probs.ContainsKey(card) ? probs[card] : 0f;
    }

    private int CountAlivePlayers(AiGameState game)
    {
        return game.Players.Count(p => !p.IsEliminated);
    }

    private int GetCardStrength(CardType card)
    {
        switch (card)
        {
            case CardType.Guard: return 1;
            case CardType.Priest: return 2;
            case CardType.Baron: return 3;
            case CardType.Handmaid: return 4;
            case CardType.Prince: return 5;
            case CardType.King: return 7;
            case CardType.Countess: return 8;
            case CardType.Princess: return 9;
            default: return 0;
        }
    }

    private CardType GetOtherCardInHand(List<CardType> hand, CardType playedCard)
    {
        if (hand == null || hand.Count == 0)
            return CardType.None;

        bool removedOnce = false;

        foreach (var card in hand)
        {
            if (card == playedCard && !removedOnce)
            {
                removedOnce = true;
                continue;
            }

            return card;
        }

        return CardType.None;
    }

    #endregion
}