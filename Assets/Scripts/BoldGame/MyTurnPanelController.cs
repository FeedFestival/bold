using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MyTurnPanelController : MonoBehaviour
{
    //public Button TakeFromBackDeckButton;
    public Button TakeFromDiscardedDeckButton;
    public Button TakeFromJustDiscardedButton;
    public Image BlockingPanelImage;
    public Button ConfirmButton;

    public Transform BackDeckTransform;
    public Transform DiscardedDeckTransform;
    public Transform JustDiscardedTransform;

    public RectTransform CanvasRect;

    public Text PointsInHand;

    private Vector2 _viewportPosition;
    private Vector2 _worldObject_ScreenPosition;

    private TurnAction _turnAction;

    // Start is called before the first frame update
    void Start()
    {
        if (TakeFromDiscardedDeckButton.gameObject.activeSelf)
            PlaceObjectOnScreen(DiscardedDeckTransform.position, TakeFromDiscardedDeckButton.GetComponent<RectTransform>(), offset: new Vector3(0.15f, 2.1f));
        if (TakeFromJustDiscardedButton.gameObject.activeSelf)
            PlaceObjectOnScreen(JustDiscardedTransform.position, TakeFromJustDiscardedButton.GetComponent<RectTransform>(), offset: new Vector3(-0.05f, 1.4f));

        TakeFromDiscardedDeckButton.gameObject.SetActive(false);
        TakeFromJustDiscardedButton.gameObject.SetActive(false);

        BlockingPanelImage.gameObject.SetActive(true);
        BlockingPanelImage.raycastTarget = false;
        ConfirmButton.gameObject.SetActive(false);

        PointsInHand.gameObject.SetActive(true);
        ShowPoints(Game.Instance.Players[Game.Instance.UserPlayerIndex].PointsInHand);
    }

    public void StartTurn()
    {
        if (Game.Instance.CardManager.CardsInDescardedDeck > 0)
            TakeFromDiscardedDeckButton.gameObject.SetActive(true);
        else
            TakeFromDiscardedDeckButton.gameObject.SetActive(false);
        if (Game.Instance.CardManager.IsAtLeastOneCardDiscarded)
            TakeFromJustDiscardedButton.gameObject.SetActive(true);
        else
            TakeFromJustDiscardedButton.gameObject.SetActive(false);

        BlockingPanelImage.raycastTarget = false;
        ConfirmButton.gameObject.SetActive(false);
    }

    public void HideActions()
    {
        ConfirmButton.gameObject.SetActive(false);
        BlockingPanelImage.raycastTarget = false;

        TakeFromDiscardedDeckButton.gameObject.SetActive(false);
        TakeFromJustDiscardedButton.gameObject.SetActive(false);
    }

    public void SetPendingAction(int turnAction)
    {
        _turnAction = (TurnAction)turnAction;

        if (_turnAction == TurnAction.None)
        {
            ConfirmButton.gameObject.SetActive(false);
            BlockingPanelImage.raycastTarget = false;
            Game.Instance.Players[Game.Instance.UserPlayerIndex].SetCardsBackToNormal();
            return;
        }

        switch (_turnAction)
        {
            case TurnAction.DiscardCards:

                BlockingPanelImage.raycastTarget = true;
                ConfirmButton.gameObject.SetActive(true);
                break;
            case TurnAction.TakeFromJustDiscarded:

                HideActions();
                OnConfirmButtonClicked();
                break;
            case TurnAction.TakeFromDiscardedDeck:

                HideActions();
                OnConfirmButtonClicked();
                break;
            default:
                break;
        }
    }

    public void OnConfirmButtonClicked()
    {
        Game.Instance.Players[Game.Instance.UserPlayerIndex].ConfirmedAction(_turnAction);
    }

    void PlaceObjectOnScreen(Vector3 worldObject, RectTransform screenObject, Vector3 offset = default(Vector3))
    {
        //then you calculate the position of the UI element
        //0,0 for the canvas is at the center of the screen, whereas WorldToViewPortPoint treats the lower left corner as 0,0. Because of this, you need to subtract the height / width of the canvas * 0.5 to get the correct position.

        _viewportPosition = Camera.main.WorldToViewportPoint(worldObject + offset);
        var x = (_viewportPosition.x * CanvasRect.sizeDelta.x) - (CanvasRect.sizeDelta.x * 0.5f);
        //x = x + offset.x;
        var y = (_viewportPosition.y * CanvasRect.sizeDelta.y) - (CanvasRect.sizeDelta.y * 0.5f);
        //y = y + offset.y;
        var screenPos = new Vector2(x, y);

        //now you can set the position of the ui element
        screenObject.anchoredPosition = screenPos;
    }

    public void ShowPoints(int points)
    {
        if (PointsInHand.gameObject.activeSelf)
            PointsInHand.text = points.ToString();

        if (points > 7)
        {
            PointsInHand.color = Game.Instance.InGameOptions.DisabledTextColor;
        }
        else
        {
            PointsInHand.color = Game.Instance.InGameOptions.AvailableTextColor;
        }
    }

}
