using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.Utils;

public class CardManager : MonoBehaviour
{
    public List<Card> CardPool;

    public bool _gameSpeedFast;

    [Header("Cards State")]
    public int CardsInBackDeck;
    public int CardsInDescardedDeck;
    public bool IsAtLeastOneCardDiscarded;

    [Header("Prefabs")]
    public GameObject C1;
    public GameObject C2;
    public GameObject C3;
    public GameObject C4;
    public GameObject C5;
    public GameObject C6;
    public GameObject C7;
    public GameObject C8;
    public GameObject C9;
    public GameObject C10;
    public GameObject C11;
    public GameObject C12;
    public GameObject C13;

    private int _playersCount;
    private int _maxCardValue;
    private int _cardCount;
    private int _initialSplitCards;
    private int _gameRounds;
    private int _gameCicles;

    private int?[] allreadyPickedIndexes;

    public void Init(int playersCount)
    {
        CardPool = new List<Card>();

        _playersCount = playersCount;
        CalculateCardCount();

        Debug.Log(
            "playerCount: " + _playersCount + " \n" +
            "_maxCardValue: " + _maxCardValue + " \n" +
            "_cardCount: " + _cardCount + " \n" +
            "_initialSplitCards: " + _initialSplitCards + " \n" +
            "_gameRounds: " + _gameRounds + " \n" +
            "_gameCicles: " + _gameCicles
            );

        //CardsInBackDeck = 0;
        CreateCards();
    }

    public void Shuffle()
    {
        CardPool.Shuffle();

        int cardIndex = 0;
        foreach (Card card in CardPool)
        {
            card.Index = cardIndex;
            card.gameObject.name = "c_(" + card.Value + ")___[" + card.Index + "]";

            card.CardPlace = CardPlace.InBackDeck;
            card.BackdeckIndex = 0;
            card.CardIndexInHand = 0;

            cardIndex++;
        }
    }

    public int?[] GetInitialCardsForPlayer()
    {
        allreadyPickedIndexes = new int?[4];
        int i = 0;
        for (i = 0; i < allreadyPickedIndexes.Length; i++)
        {
            allreadyPickedIndexes[i] = GetRandomCardIndex();
        }

        var listOfCards = new Card[4];
        i = 0;
        foreach (int index in allreadyPickedIndexes)
        {
            listOfCards[i] = CardPool[index];
            i++;
        }

        return allreadyPickedIndexes;
        //allreadyPickedIndexes = null;
        //return listOfCards;
    }

    public int GetFirstFromBackDeck()
    {
        var cardsInBackDeck = CardPool.Where(c => c.CardPlace == CardPlace.InBackDeck);
        int index = 200;
        foreach (Card card in cardsInBackDeck)
        {
            if (index > card.BackdeckIndex)
                index = card.Index;
        }
        return index;
    }

    public int GetFirstFromDiscardedDeck()
    {
        var cardsInDiscardedDeck = CardPool.Where(c => c.CardPlace == CardPlace.InDiscardedDeck);

        if (cardsInDiscardedDeck == null || cardsInDiscardedDeck.Count() == 0)
            return 200;

        float highestY = -200;
        foreach (Card card in cardsInDiscardedDeck)
        {
            if (highestY < card.transform.position.y)
                highestY = card.transform.position.y;
        }
        var firstCardIndex = cardsInDiscardedDeck.FirstOrDefault(c => c.transform.position.y == highestY).Index;
        return firstCardIndex;
    }

    public int GetFirstFromJustDiscarded()
    {
        return CardPool.FirstOrDefault(c => c.CardPlace == CardPlace.IsJustDiscarded).Index;
    }

    //

    private int GetRandomCardIndex()
    {
        int index = (int)UnityEngine.Random.Range(0, _cardCount - 1);
        bool allreadyPicked = false;
        foreach (int? aP in allreadyPickedIndexes)
        {
            if (aP == null) break;

            allreadyPicked = (aP == index);
            if (allreadyPicked) break;
        }
        if (allreadyPicked)
        {
            return GetRandomCardIndex();
        }
        else
        {
            bool cardAllreadyPicked = CardPool[index].CardPlace != CardPlace.InBackDeck;
            if (cardAllreadyPicked)
                return GetRandomCardIndex();
            else
                return index;
        }
    }

    private void CalculateCardCount()
    {
        switch (_playersCount)
        {
            case 2:
                _maxCardValue = _gameSpeedFast ? 7 : 7;
                break;
            case 3:
                _maxCardValue = _gameSpeedFast ? 8 : 9;
                break;
            case 4:
                _maxCardValue = _gameSpeedFast ? 9 : 10;
                break;
            case 5:
                _maxCardValue = _gameSpeedFast ? 10 : 11;
                break;
            case 6:
                _maxCardValue = _gameSpeedFast ? 11 : 12;
                break;
            case 7:
                _maxCardValue = _gameSpeedFast ? 23 : 13;
                break;
            default: break;
        }

        _cardCount = 0;
        for (var i = _maxCardValue; i > 1; i--)
        {
            _cardCount = _cardCount + i;
        }

        // add the ones
        _cardCount = _cardCount + (1 * _playersCount);

        // calculate game properties
        _initialSplitCards = (4 * _playersCount);
        _gameRounds = _cardCount - _initialSplitCards;
        _gameCicles = _gameRounds / _playersCount;
    }

    public List<int> GetCardsCurrentlyDiscarded()
    {
        var indexList = new List<int>();
        foreach (Card card in CardPool.Where(c => c.CardPlace == CardPlace.IsJustDiscarded))
        {
            indexList.Add(card.Index);
        }
        return indexList;
    }

    private void CreateCards()
    {
        for (var cardValue = _maxCardValue; cardValue > 1; cardValue--)
        {
            for (var i = 1; i <= cardValue; i++)
            {
                CreateCard(cardValue, i);
            }
        }
        for (var i = 0; i < _playersCount; i++)
        {
            CreateCard(1, i);
        }
    }

    private void CreateCard(int cardValue, int index)
    {
        var card = Instantiate(GetCardPrefab(cardValue)).GetComponent<Card>();
        card.transform.position = new Vector3(0, 15, 0);
        card.transform.eulerAngles = Vector3.zero;

        CardPool.Add(card);
    }

    private GameObject GetCardPrefab(int cardValue)
    {
        switch (cardValue)
        {
            case 1:
                return C1;
            case 2:
                return C2;
            case 3:
                return C3;
            case 4:
                return C4;
            case 5:
                return C5;
            case 6:
                return C6;
            case 7:
                return C7;
            case 8:
                return C8;
            case 9:
                return C9;
            case 10:
                return C10;
            case 11:
                return C11;
            case 12:
                return C12;
            case 13:
                return C13;
            default: throw new Exception("No such card! Something is wrong..");
        }
    }
}
