using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image cardImage;
    [SerializeField] private TMP_Text valueText;

    [SerializeField] private TMP_Text customLabelText;

    private CardType currentType;
    private Action<CardType> callback;

    public void Setup(CardType cardType, Sprite sprite, bool interactable, Action<CardType> onClick)
    {
        currentType = cardType;
        callback = onClick;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = interactable;

            if (onClick != null)
                button.onClick.AddListener(() => callback.Invoke(currentType));
        }

        if (cardImage != null)
            cardImage.sprite = sprite;

        if (valueText != null)
            valueText.text = ((int)cardType).ToString();

        //if (customLabelText != null)
        //    customLabelText.text = "";
    }

    public void SetupHidden(Sprite hiddenSprite)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = false;
        }

        if (cardImage != null)
            cardImage.sprite = hiddenSprite;

        if (valueText != null)
            valueText.text = "";
    }

    public void SetCustomLabel(string text)
    {
        if (customLabelText != null)
            customLabelText.text = text;
    }
}