using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIPlayer : MonoBehaviour
{
    private PlayerObject _playerObject;
    private bool _weCouldWin;
    Dictionary<int, int> _totalOfValueCards;

    public void Init(PlayerObject playerObject)
    {
        _playerObject = playerObject;
    }

    internal void PlayTurn()
    {
        //Debug.Log("--------------------------" + _playerObject.PlayerIndex + "-------------------");

        ThinkOfYourNextMove();

        if (_weCouldWin)
        {
            Game.Instance.PlayerThatCalledBold = _playerObject.PlayerIndex;
            Game.Instance.Bold();


            return;
        }

        if (_playerObject.AfterDiscardAction != AfterDiscardAction.TakeFromBackDeck)
        {
            _playerObject.AllreadyTookCard = true;
            int takeCardIndex = 0;
            switch (_playerObject.AfterDiscardAction)
            {
                case AfterDiscardAction.TakeFromDiscardedDeck:

                    takeCardIndex = Game.Instance.CardManager.GetFirstFromDiscardedDeck();
                    break;
                case AfterDiscardAction.TakeFromJustDiscarded:

                    takeCardIndex = Game.Instance.CardManager.GetFirstFromJustDiscarded();

                    Game.Instance.CardManager.IsAtLeastOneCardDiscarded = false;
                    break;
            }
            var card = Game.Instance.CardManager.CardPool[takeCardIndex];

            var cpi = _playerObject.GetAvailableHandCardPosition();
            var pos = _playerObject.CardPositions[cpi].position;
            var rot = _playerObject.CardPositions[cpi].eulerAngles;

            card.GoInHand(pos, rot, afterTakeCard: () =>
            {
                card.CardIndexInHand = cpi;
                _playerObject.CardPositionsOccupied[cpi] = true;

                _playerObject.CardsInHand.Add(takeCardIndex);

                _playerObject.DiscardedCardCurrentIndex = 0;
                StartCoroutine(_playerObject.DiscardCard());
            });
        }
        else
        {
            _playerObject.DiscardedCardCurrentIndex = 0;
            StartCoroutine(_playerObject.DiscardCard());
        }
    }

    private void ThinkOfYourNextMove()
    {
        _totalOfValueCards = new Dictionary<int, int>();
        foreach (int cardIndex in _playerObject.CardsInHand)
        {
            var card = Game.Instance.CardManager.CardPool[cardIndex];
            if (_totalOfValueCards.ContainsKey(card.Value))
                _totalOfValueCards[card.Value] += card.Value;
            else
                _totalOfValueCards.Add(card.Value, card.Value);
        }

        _weCouldWin = CheckIfWeCouldWin(_playerObject.PointsInHand);
        if (_weCouldWin)
        {
            return;
        }

        if (_playerObject.AIStrategy == AIStrategy.TwoMovesInAdvance && Game.Instance.CardManager.CardsInDescardedDeck > 2)
        {
            var ignoreValue = CheckDiscardedTake(excludeCardsToBeDiscarded: false);
            if (ignoreValue == 200)
                return;
            ;
            var copyOfTotalOfValueCards = _totalOfValueCards;

            copyOfTotalOfValueCards.Remove(ignoreValue);

            if (copyOfTotalOfValueCards == null || copyOfTotalOfValueCards.Count == 0)
            {
                KeepThinking();
                return;
            }

            //Debug.Log("ignoreValue: " + ignoreValue);
            var deleted = _totalOfValueCards.Count - copyOfTotalOfValueCards.Count;

            var secondGreatestValue = copyOfTotalOfValueCards.Values.Max();
            var secondKeyOfGreatestValue = copyOfTotalOfValueCards.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;

            //Debug.Log("secondGreatestValue: " + secondGreatestValue);

            if (secondGreatestValue == 2 || secondGreatestValue == 1)
            {
                KeepThinking();
                return;
            }

            if (deleted == 0)
            {
                //Debug.Log("_totalOfValueCards.Count: " + _totalOfValueCards.Count);
                KeepThinking();
                return;
            }

            return;
        }

        KeepThinking();
    }

    private bool CheckIfWeCouldWin(int totalValueInHand)
    {
        if (totalValueInHand <= 3)
            return true;

        if (totalValueInHand <= 5)
        {
            var isItSafe = false;
            foreach (var player in Game.Instance.Players)
            {
                if (player.CardsInHand.Count < 3)
                    isItSafe = true;
                else
                    isItSafe = false;
            }
            return isItSafe;
        }

        if (totalValueInHand <= 7)
        {
            var isItSafe = false;
            foreach (var player in Game.Instance.Players)
            {
                if (player.CardsInHand.Count < 4)
                    isItSafe = true;
                else
                    isItSafe = false;
            }
            return isItSafe;
        }

        return false;
    }

    private void KeepThinking()
    {
        if (_totalOfValueCards == null || _totalOfValueCards.Count == 0)
        {
            _totalOfValueCards = new Dictionary<int, int>();
            foreach (int cardIndex in _playerObject.CardsInHand)
            {
                var card = Game.Instance.CardManager.CardPool[cardIndex];
                if (_totalOfValueCards.ContainsKey(card.Value))
                    _totalOfValueCards[card.Value] += card.Value;
                else
                    _totalOfValueCards.Add(card.Value, card.Value);
            }
        }

        var greatestValue = _totalOfValueCards.Values.Max();
        var keyOfGreatestValue = _totalOfValueCards.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;

        _playerObject.CardsYouWillDiscard = new List<int>();
        foreach (int cardIndex in _playerObject.CardsInHand)
        {
            var card = Game.Instance.CardManager.CardPool[cardIndex];
            if (card.Value == keyOfGreatestValue)
                _playerObject.CardsYouWillDiscard.Add(cardIndex);
        }

        if (_playerObject.AIStrategy != AIStrategy.Simple)
        {
            CheckDiscardedTake(excludeCardsToBeDiscarded: true);
        }
        else
        {
            CheckForBetterAlternativeToBackDeck(greatestValue);
        }
    }

    private int CheckForBetterAlternativeToBackDeck(int valueDiscarded)
    {
        var totalValueAfterDiscard = 0;
        foreach (int cardIndex in _playerObject.CardsInHand)
        {
            var exists = _playerObject.CardsYouWillDiscard.Exists(cyud => cyud == cardIndex);
            if (exists == false)
            {
                var card = Game.Instance.CardManager.CardPool[cardIndex];
                totalValueAfterDiscard += card.Value;
            }
        }

        var di = Game.Instance.CardManager.GetFirstFromDiscardedDeck();
        if (di == 200)
            return 200;

        var firstDiscardedCard = Game.Instance.CardManager.CardPool[di];

        var jdci = Game.Instance.CardManager.GetFirstFromJustDiscarded();
        var justDiscardedCard = Game.Instance.CardManager.CardPool[jdci];

        if (firstDiscardedCard.Value < valueDiscarded / 1.8f)
        {
            _playerObject.AfterDiscardAction = AfterDiscardAction.TakeFromDiscardedDeck;
            Debug.Log("firstDiscardedCard.Value: " + firstDiscardedCard.Value + ", justDiscardedCard.Value: " + justDiscardedCard.Value + ", valueDiscarded: " + valueDiscarded + ", totalValueAfterDiscard: " + totalValueAfterDiscard);
        }
        else if (justDiscardedCard.Value < valueDiscarded / 1.8f)
        {
            _playerObject.AfterDiscardAction = AfterDiscardAction.TakeFromJustDiscarded;
            Debug.Log("firstDiscardedCard.Value: " + firstDiscardedCard.Value + ", justDiscardedCard.Value: " + justDiscardedCard.Value + ", valueDiscarded: " + valueDiscarded + ", totalValueAfterDiscard: " + totalValueAfterDiscard);
        }

        return 0;
    }

    private int CheckDiscardedTake(bool excludeCardsToBeDiscarded)
    {
        var di = Game.Instance.CardManager.GetFirstFromDiscardedDeck();
        if (di == 200)
            return 200;

        var firstDiscardedCard = Game.Instance.CardManager.CardPool[di];

        var jdci = Game.Instance.CardManager.GetFirstFromJustDiscarded();
        var justDiscardedCard = Game.Instance.CardManager.CardPool[jdci];

        bool weHaveThatValueInHand = false;
        int highestValueThatWeCanDoDouble = 0;
        foreach (int cardIndex in _playerObject.CardsInHand)
        {
            if (excludeCardsToBeDiscarded)
            {
                var exists = _playerObject.CardsYouWillDiscard.Exists(cyud => cyud == cardIndex);
                if (exists == false)
                {
                    var card = Game.Instance.CardManager.CardPool[cardIndex];
                    if (card.Value == firstDiscardedCard.Value)
                    {
                        weHaveThatValueInHand = true;
                        highestValueThatWeCanDoDouble = firstDiscardedCard.Value;
                    }
                    if (card.Value == justDiscardedCard.Value)
                    {
                        weHaveThatValueInHand = true;
                        highestValueThatWeCanDoDouble = justDiscardedCard.Value;
                    }
                }
            }
            else
            {
                var card = Game.Instance.CardManager.CardPool[cardIndex];
                if (card.Value == firstDiscardedCard.Value)
                {
                    weHaveThatValueInHand = true;
                    highestValueThatWeCanDoDouble = firstDiscardedCard.Value;
                }
                if (card.Value == justDiscardedCard.Value)
                {
                    weHaveThatValueInHand = true;
                    highestValueThatWeCanDoDouble = justDiscardedCard.Value;
                }
            }
        }

        if (weHaveThatValueInHand)
        {
            if (highestValueThatWeCanDoDouble == firstDiscardedCard.Value)
                _playerObject.AfterDiscardAction = AfterDiscardAction.TakeFromDiscardedDeck;
            else
                _playerObject.AfterDiscardAction = AfterDiscardAction.TakeFromJustDiscarded;
        }
        else
        {
            _playerObject.AfterDiscardAction = AfterDiscardAction.TakeFromBackDeck;
        }

        return highestValueThatWeCanDoDouble;
    }
}
