using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private Button button;

    private CardData cardData;
    private HandManager handManager;

    public CardData CardData => cardData;


    public void Setup(CardData data, HandManager manager)
    {
        cardData = data;
        handManager = manager;

        cardNameText.text = data.cardName;
        costText.text = "Cost: " + data.cost;
        powerText.text = "Power: " + data.power;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnCardClicked);
    }


    private void OnCardClicked()
    {
        handManager.CardClicked(this);
    }


    public void SetInteractable(bool value)
    {
        button.interactable = value;
    }
}