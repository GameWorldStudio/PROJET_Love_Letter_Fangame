using UnityEngine;

public class CardSpriteLibrary : MonoBehaviour
{
    public static CardSpriteLibrary Instance;

    [Header("Visible cards")]
    [SerializeField] private Sprite guardSprite;
    [SerializeField] private Sprite priestSprite;
    [SerializeField] private Sprite baronSprite;
    [SerializeField] private Sprite handmaidSprite;
    [SerializeField] private Sprite princeSprite;
    [SerializeField] private Sprite kingSprite;
    [SerializeField] private Sprite countessSprite;
    [SerializeField] private Sprite princessSprite;

    [Header("Special")]
    [SerializeField] private Sprite hiddenCardSprite;

    private void Awake()
    {
        Instance = this;
    }

    public Sprite GetCardSprite(CardType type)
    {
        switch (type)
        {
            case CardType.Guard: return guardSprite;
            case CardType.Priest: return priestSprite;
            case CardType.Baron: return baronSprite;
            case CardType.Handmaid: return handmaidSprite;
            case CardType.Prince: return princeSprite;
            case CardType.King: return kingSprite;
            case CardType.Countess: return countessSprite;
            case CardType.Princess: return princessSprite;
            default: return hiddenCardSprite;
        }
    }

    public Sprite GetHiddenSprite()
    {
        return hiddenCardSprite;
    }
}