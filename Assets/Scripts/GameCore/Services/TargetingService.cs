using System.Collections.Generic;

public class TargetingService
{
    public List<int> GetValidTargets(GameState state, int casterIndex, CardType sourceCard)
    {
        List<int> result = new();

        for (int i = 0; i < state.Players.Length; i++)
        {
            if (state.Players[i].isEliminated || state.Players[i].isProtected)
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

    public bool IsValidTarget(GameState state, int casterIndex, CardType sourceCard, int targetIndex)
    {
        return GetValidTargets(state, casterIndex, sourceCard).Contains(targetIndex);
    }
}