using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private Transform handPanel;
    [SerializeField] private Transform selectedCardsPanel;

    [Header("Card")]
    [SerializeField] private CardUI cardPrefab;

    [Header("UI")]
    [SerializeField] private TMP_Text availableCostText;
    [SerializeField] private TMP_Text statusText;


    private readonly List<CardData> deck = new();
    private readonly List<CardData> hand = new();

    private readonly List<CardUI> allCards = new();
    private readonly List<CardUI> selectedCards = new();
    private readonly List<CardData> cardDefinitions = new();
    private readonly List<CardUI> revealedCards = new();
    private readonly List<GameObject> localFoldedCards = new();


    private int availableCost;
    private int selectedCost;

    private bool canSelectCards;


    private void Start()
    {
        CreateDeck();
        ShuffleDeck();

        DrawStartingHand();

        SetCardsInteractable(false);

        UpdateCostUI();
    }


    // =====================================================
    // DECK
    // =====================================================

    private void CreateDeck()
    {
        deck.Clear();
        cardDefinitions.Clear();

        deck.Add(new CardData
        {
            id = 1,
            cardName = "Warrior",
            cost = 1,
            power = 2
        });

        deck.Add(new CardData
        {
            id = 2,
            cardName = "Archer",
            cost = 1,
            power = 1
        });

        deck.Add(new CardData
        {
            id = 3,
            cardName = "Knight",
            cost = 2,
            power = 3
        });

        deck.Add(new CardData
        {
            id = 4,
            cardName = "Shield Bearer",
            cost = 2,
            power = 2
        });

        deck.Add(new CardData
        {
            id = 5,
            cardName = "Mage",
            cost = 3,
            power = 4
        });

        deck.Add(new CardData
        {
            id = 6,
            cardName = "Rogue",
            cost = 2,
            power = 3
        });

        deck.Add(new CardData
        {
            id = 7,
            cardName = "Guardian",
            cost = 3,
            power = 5
        });

        deck.Add(new CardData
        {
            id = 8,
            cardName = "Assassin",
            cost = 4,
            power = 6
        });

        deck.Add(new CardData
        {
            id = 9,
            cardName = "Paladin",
            cost = 4,
            power = 5
        });

        deck.Add(new CardData
        {
            id = 10,
            cardName = "Wizard",
            cost = 5,
            power = 7
        });

        deck.Add(new CardData
        {
            id = 11,
            cardName = "Dragon",
            cost = 6,
            power = 9
        });

        deck.Add(new CardData
        {
            id = 12,
            cardName = "Champion",
            cost = 5,
            power = 8
        });
        cardDefinitions.AddRange(deck);
    }


    private void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

            CardData temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }


   

    private void DrawStartingHand()
    {
        DrawCard();
        DrawCard();
        DrawCard();
    }


    public void DrawCard()
    {
        if (deck.Count == 0)
        {
            Debug.Log("Deck is empty.");
            return;
        }

        CardData cardData = deck[0];

        deck.RemoveAt(0);

        hand.Add(cardData);

        CardUI cardUI =
            Instantiate(cardPrefab, handPanel);

        cardUI.Setup(cardData, this);

        allCards.Add(cardUI);

        cardUI.SetInteractable(canSelectCards);
    }


    // =====================================================
    // TURN
    // =====================================================

    public void BeginTurn(int cost)
    {
        availableCost = cost;
        selectedCost = 0;

        canSelectCards = true;

        SetCardsInteractable(true);

        UpdateCostUI();

        if (statusText != null)
        {
            statusText.text =
                "Select cards. Available cost: " +
                availableCost;
        }
    }


    public void StopTurn()
    {
        canSelectCards = false;

        SetCardsInteractable(false);
    }


    // =====================================================
    // CLICK
    // =====================================================

    public void CardClicked(CardUI card)
    {
        if (!canSelectCards)
            return;

        if (selectedCards.Contains(card))
        {
            UnselectCard(card);
        }
        else
        {
            SelectCard(card);
        }
    }


    private void SelectCard(CardUI card)
    {
        int cardCost = card.CardData.cost;

        if (selectedCost + cardCost > availableCost)
        {
            if (statusText != null)
            {
                statusText.text =
                    "Not enough cost available.";
            }

            return;
        }

        selectedCards.Add(card);

        selectedCost += cardCost;

        card.transform.SetParent(
            selectedCardsPanel,
            false
        );

        UpdateCostUI();
    }


    private void UnselectCard(CardUI card)
    {
        selectedCards.Remove(card);

        selectedCost -= card.CardData.cost;

        if (selectedCost < 0)
            selectedCost = 0;

        card.transform.SetParent(
            handPanel,
            false
        );

        UpdateCostUI();
    }


    // =====================================================
    // SELECTED IDs
    // =====================================================

    public int[] GetSelectedCardIds()
    {
        int[] ids =
            new int[selectedCards.Count];

        for (int i = 0;
             i < selectedCards.Count;
             i++)
        {
            ids[i] =
                selectedCards[i].CardData.id;
        }

        return ids;
    }


   

    public int[] FoldSelectedCards(
        GameObject cardBackPrefab)
    {
        
        int[] selectedIds =
            GetSelectedCardIds();


        canSelectCards = false;

        SetCardsInteractable(false);


        
        List<CardUI> cardsToFold =
            new List<CardUI>(selectedCards);


        foreach (CardUI card in cardsToFold)
        {
            hand.Remove(card.CardData);

            allCards.Remove(card);


            // Replace face-up card with CardBack.
            GameObject cardBack =
                Instantiate(
                    cardBackPrefab,
                    selectedCardsPanel
                );

            localFoldedCards.Add(cardBack);


            Destroy(card.gameObject);
        }


        selectedCards.Clear();

        selectedCost = 0;

        UpdateCostUI();

        return selectedIds;
    }


   

    public void ClearFoldedCards()
    {
        foreach (GameObject card in localFoldedCards)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }

        localFoldedCards.Clear();
    }


    // =====================================================
    // INTERACTABLE
    // =====================================================

    private void SetCardsInteractable(bool value)
    {
        foreach (CardUI card in allCards)
        {
            if (card != null)
            {
                card.SetInteractable(value);
            }
        }
    }


    // =====================================================
    // UI
    // =====================================================

    private void UpdateCostUI()
    {
        if (availableCostText == null)
            return;

        availableCostText.text =
            "Cost: " +
            selectedCost +
            " / " +
            availableCost;
    }



    public CardData GetCardDataById(int id)
    {
        foreach (CardData card in cardDefinitions)
        {
            if (card.id == id)
            {
                return card;
            }
        }

        return null;
    }


    public void RevealLocalCard(int cardId)
    {
        CardData data = GetCardDataById(cardId);

        if (data == null)
        {
            Debug.LogError("Card not found: " + cardId);
            return;
        }

        // Remove one face-down card.
        if (localFoldedCards.Count > 0)
        {
            GameObject cardBack = localFoldedCards[0];

            localFoldedCards.RemoveAt(0);

            if (cardBack != null)
            {
                Destroy(cardBack);
            }
        }

        CreateRevealCard(data, selectedCardsPanel);
    }


    public void CreateOpponentRevealCard(
        int cardId,
        Transform opponentPanel)
    {
        CardData data = GetCardDataById(cardId);

        if (data == null)
        {
            Debug.LogError("Card not found: " + cardId);
            return;
        }

        CreateRevealCard(data, opponentPanel);
    }


    private void CreateRevealCard(
        CardData data,
        Transform parent)
    {
        CardUI card =
            Instantiate(cardPrefab, parent);

        card.Setup(data, this);

        card.SetInteractable(false);

        revealedCards.Add(card);
    }


    public void ClearRevealedCards()
    {
        foreach (CardUI card in revealedCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }

        revealedCards.Clear();
    }
}



[Serializable]
public class CardData
{
    public int id;
    public string cardName;
    public int cost;
    public int power;
}