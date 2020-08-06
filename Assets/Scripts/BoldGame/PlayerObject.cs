using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerObject : MonoBehaviour
{
    public List<Transform> CardPositions;
    public List<Transform> ShowCardPositions;
    public Dictionary<int, bool> CardPositionsOccupied;

    public int PlayerPosition;
    public int PlayerIndex;
    public int PlayerTurn;
    public bool IsPlayingTurn;
    public List<int> CardsInHand;

    public int PointsInHand;

    [Header("AI Options")]
    public bool IsAI;
    [SerializeField]
    public AIStrategy AIStrategy;

    public List<int> CardsYouWillDiscard;

    public AfterDiscardAction AfterDiscardAction;

    public bool AllreadyTookCard;

    public int ShowCardCurrentIndex;
    public int DiscardedCardCurrentIndex;

    // 
    private AIPlayer _aIPlayer;

    private float _timeBetweenActions = 0.3f;
    private float _doDiscardCardsWaitTime = 0.4f;
    internal int TotalPoints;

    public void Init(int?[] cardsInHand)
    {
        CardsInHand = new List<int>();

        CardPositionsOccupied = new Dictionary<int, bool>();
        int cpi = 0;
        foreach (Transform t in CardPositions)
        {
            CardPositionsOccupied.Add(cpi, false);
            cpi++;
        }
        cpi = 0;

        foreach (int cardIndex in cardsInHand)
        {
            cpi = GetAvailableHandCardPosition();
            var pos = CardPositions[cpi].position;
            var rot = CardPositions[cpi].eulerAngles;

            var card = Game.Instance.CardManager.CardPool[cardIndex];
            card.GoInHand(pos, rot, instant: true, afterTakeCard: () =>
            {
                card.CardIndexInHand = cpi;
                CardPositionsOccupied[cpi] = true;
            });

            CardsInHand.Add(cardIndex);
        }

        CalculatePoints();

        if (IsAI)
        {
            gameObject.AddComponent<AIPlayer>();
            _aIPlayer = GetComponent<AIPlayer>();
            _aIPlayer.Init(this);
        }
    }

    public void PlayTurn()
    {
        IsPlayingTurn = true;

        foreach (int cardIndex in CardsInHand)
        {
            var card = Game.Instance.CardManager.CardPool[cardIndex];
            card.GetComponent<BoxCollider>().enabled = true;
        }

        Game.Instance.MyTurnPanelController.gameObject.SetActive(true);
    }

    public void AiPlayTurn()
    {
        IsPlayingTurn = true;
        _aIPlayer.PlayTurn();   // after calculations this will call DiscardCard.
    }

    private void EndTurn()
    {
        //Debug.Log("I ended my turn. P[" + PlayerIndex + "]");

        CardsYouWillDiscard = null;
        IsPlayingTurn = false;
        AllreadyTookCard = false;

        if (IsAI == false)
            Game.Instance.MyTurnPanelController.HideActions();

        Game.Instance.NextPlayerTurn();
    }

    public void CardWasSelectedByUser(int cardValue, bool selected)
    {
        if (selected == false)
        {
            Game.Instance.MyTurnPanelController.SetPendingAction((int)TurnAction.None);
            SetCardsBackToNormal();
            CardsYouWillDiscard = new List<int>();
            return;
        }

        Game.Instance.MyTurnPanelController.SetPendingAction((int)TurnAction.DiscardCards);

        if (CardsYouWillDiscard == null)
            CardsYouWillDiscard = new List<int>();

        foreach (int cardIndex in CardsInHand)
        {
            var card = Game.Instance.CardManager.CardPool[cardIndex];
            if (card.Value == cardValue)
            {
                card.CardSelected(true);
                CardsYouWillDiscard.Add(cardIndex);
            }
            else
            {
                card.ShrinkCard();
                CardsYouWillDiscard.Remove(cardIndex);
            }
        }
    }

    public void ConfirmedAction(TurnAction turnAction)
    {
        Card card = null;
        bool complexAction = false;

        Game.Instance.CanBold = false;

        switch (turnAction)
        {
            case TurnAction.DiscardCards:

                foreach (int cardIndex in CardsInHand)
                {
                    card = Game.Instance.CardManager.CardPool[cardIndex];
                    card.GetComponent<BoxCollider>().enabled = false;
                }

                Game.Instance.MyTurnPanelController.HideActions();

                StartCoroutine(DoDiscardCards());
                break;
            case TurnAction.TakeFromJustDiscarded:

                complexAction = true;
                Game.Instance.CardManager.IsAtLeastOneCardDiscarded = false;
                break;
            case TurnAction.TakeFromDiscardedDeck:

                complexAction = true;
                break;
            default:
                break;
        }

        if (complexAction)
        {
            AllreadyTookCard = true;
            int takeCardIndex = 0;
            switch (turnAction)
            {
                case TurnAction.TakeFromDiscardedDeck:
                    takeCardIndex = Game.Instance.CardManager.GetFirstFromDiscardedDeck();
                    break;
                case TurnAction.TakeFromJustDiscarded:
                    takeCardIndex = Game.Instance.CardManager.GetFirstFromJustDiscarded();
                    break;
            }
            card = Game.Instance.CardManager.CardPool[takeCardIndex];

            var cpi = GetAvailableHandCardPosition();
            var pos = CardPositions[cpi].position;
            var rot = CardPositions[cpi].eulerAngles;

            card.GoInHand(pos, rot, afterTakeCard: () =>
            {
                card.CardIndexInHand = cpi;
                CardPositionsOccupied[cpi] = true;

                CardsInHand.Add(takeCardIndex);

                foreach (int ci in CardsInHand)
                {
                    if (Game.Instance.CardManager.CardPool[ci].Value == card.Value)
                        Game.Instance.CardManager.CardPool[ci].SetBlocked();
                }

                CalculatePoints();
            });
        }

        SetCardsBackToNormal();
    }

    IEnumerator DoDiscardCards()
    {

        yield return new WaitForSeconds(Game.Instance.InGameOptions.ByGameSpeed(_doDiscardCardsWaitTime));

        DiscardedCardCurrentIndex = 0;
        StartCoroutine(DiscardCard());
    }

    public void SetCardsBackToNormal(bool force = false)
    {
        foreach (int cardIndex in CardsInHand)
        {
            var card = Game.Instance.CardManager.CardPool[cardIndex];
            if (force)
                card.CardSelected(false);
            else
            {
                if (card.IsBlocked == false)
                    card.CardSelected(false);
            }
        }
    }

    public IEnumerator DiscardCard()
    {
        int cardIndex = CardsYouWillDiscard[DiscardedCardCurrentIndex];
        var card = Game.Instance.CardManager.CardPool[cardIndex];

        Debug.Log("Discarding Card[" + card.Value + "]_(" + card.Index + ") -| " + DateTime.Now.ToString("HH:mm:ss.fff"));

        if (DiscardedCardCurrentIndex == CardsYouWillDiscard.Count - 1)
        {
            card.GoToDiscarded(() =>
            {
                if (AllreadyTookCard)
                {
                    if (IsAI == false)
                        SetCardsBackToNormal(true);

                    EndTurn();
                    return;
                }

                int takeCardIndex = Game.Instance.CardManager.GetFirstFromBackDeck();
                card = Game.Instance.CardManager.CardPool[takeCardIndex];

                Debug.Log(" -  Taking Card[" + card.Value + "]_(" + card.Index + ") -| " + DateTime.Now.ToString("HH:mm:ss.fff"));
                //Debug.Log("         -  takeCardIndex: [" + takeCardIndex + "]");

                var cpi = GetAvailableHandCardPosition();
                var pos = CardPositions[cpi].position;
                var rot = CardPositions[cpi].eulerAngles;

                card.GoInHand(pos, rot, afterTakeCard: () =>
                {
                    card.CardIndexInHand = cpi;
                    CardPositionsOccupied[cpi] = true;

                    CardsInHand.Add(takeCardIndex);

                    CalculatePoints();

                    EndTurn();
                });
            });
        }
        else
        {
            card.GoToDiscarded();
        }
        CardPositionsOccupied[card.CardIndexInHand] = false;
        CardsInHand.Remove(cardIndex);

        CalculatePoints();

        yield return new WaitForSeconds(Game.Instance.InGameOptions.ByGameSpeed(_timeBetweenActions));

        DiscardedCardCurrentIndex++;
        if (DiscardedCardCurrentIndex < CardsYouWillDiscard.Count)
        {
            StartCoroutine(DiscardCard());
        }
    }

    IEnumerator ShowCard()
    {
        int cardIndex = CardsInHand[ShowCardCurrentIndex];
        var card = Game.Instance.CardManager.CardPool[cardIndex];

        var pos = ShowCardPositions[ShowCardCurrentIndex].position;
        var rot = ShowCardPositions[ShowCardCurrentIndex].eulerAngles;

        card.ShowCard(pos, rot);

        yield return new WaitForSeconds(Game.Instance.InGameOptions.ByGameSpeed(_timeBetweenActions));

        ShowCardCurrentIndex++;
        if (ShowCardCurrentIndex < CardsInHand.Count)
        {
            StartCoroutine(ShowCard());
        }
    }

    public void ShowCards()
    {
        ShowCardCurrentIndex = 0;
        StartCoroutine(ShowCard());
    }

    public int GetAvailableHandCardPosition()
    {
        return CardPositionsOccupied.Where(cpo => cpo.Value == false).FirstOrDefault().Key;
    }

    void CalculatePoints()
    {
        PointsInHand = 0;
        foreach (int ci in CardsInHand)
        {
            var card = Game.Instance.CardManager.CardPool[ci];
            PointsInHand += card.Value;
        }
        if (IsAI == false)
            Game.Instance.MyTurnPanelController.ShowPoints(PointsInHand);
    }
}
