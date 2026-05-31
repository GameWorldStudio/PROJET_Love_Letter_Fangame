[System.Serializable]
public class PendingActionState
{
    public PendingActionType actionType = PendingActionType.None;
    public CardType sourceCard;
    public int casterIndex = -1;
    public int selectedTargetIndex = -1;

    public void Clear()
    {
        actionType = PendingActionType.None;
        sourceCard = 0;
        casterIndex = -1;
        selectedTargetIndex = -1;
    }
}

public enum PendingActionType
{
    None,
    SelectTarget,
    SelectGuardGuess
}