using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    public int Value;

    [Header("In Game Props")]
    public int Index;
    public int BackdeckIndex;
    public int CardIndexInHand;
    [SerializeField]
    public CardPlace CardPlace;

    public bool IsBlocked;

    private float _scaleAnimationTime = 0.44f;

    public delegate void AfterDiscardCallback();
    private AfterDiscardCallback AfterDiscard;

    public delegate void AfterTakeCardCallback();
    private AfterTakeCardCallback AfterTakeCard;

    private IEnumerator _killTween;
    private int? _moveTweenId;
    private int? _rotateTweenId;
    private int? _scaleTweenId;
    private float _animationTime = 1.5f;
    private float _goToDiscardedDeckWaitTime = 0.1f;
    private LeanTweenType _animationType = LeanTweenType.linear;

    private readonly Vector3 _scaledCard = new Vector3(1.03f, 1.03f, 1.03f);
    private readonly Vector3 _normalScaleCard = new Vector3(1, 1, 1);
    private readonly Vector3 _shrinkedScaleCard = new Vector3(0.97f, 0.97f, 0.97f);

    private int _timesMoved;

    private bool _selected = false;

    private BoxCollider _collider;

    void Start()
    {
        if (gameObject.GetComponent<BoxCollider>() == null)
            gameObject.AddComponent<BoxCollider>();
        _collider = gameObject.GetComponent<BoxCollider>();
        _collider.enabled = false;
    }

    private void OnMouseUp()
    {
        _selected = !_selected;
        Game.Instance.Players[Game.Instance.UserPlayerIndex].CardWasSelectedByUser(Value, _selected);
    }

    public void SetBlocked()
    {
        _collider.enabled = false;

        ShrinkCard();
        IsBlocked = true;
    }

    public void CardSelected(bool selected = false)
    {
        if (selected)
        {
            if (_scaleTweenId.HasValue)
            {
                LeanTween.cancel(_scaleTweenId.Value);
            }
            _scaleTweenId = LeanTween.scale(gameObject, _scaledCard, Game.Instance.InGameOptions.ByGameSpeed(_scaleAnimationTime)).id;
        }
        else
        {
            if (_scaleTweenId.HasValue)
            {
                LeanTween.cancel(_scaleTweenId.Value);
            }
            _scaleTweenId = LeanTween.scale(gameObject, _normalScaleCard, Game.Instance.InGameOptions.ByGameSpeed(_scaleAnimationTime)).id;
            IsBlocked = false;
        }
        //IsBlocked = false;
    }

    public void ShrinkCard()
    {
        if (_scaleTweenId.HasValue)
        {
            LeanTween.cancel(_scaleTweenId.Value);
        }
        _scaleTweenId = LeanTween.scale(gameObject, _shrinkedScaleCard, Game.Instance.InGameOptions.ByGameSpeed(_scaleAnimationTime)).id;
    }

    public void GoToBackDeck(bool instant = false)
    {
        var position = new Vector3(
            Game.Instance.InGameOptions.BackDeckTransform.position.x,
            Game.Instance.InGameOptions.BackDeckTransform.position.y + (Game.Instance.InGameOptions.DeckYAddition * Game.Instance.CardManager.CardsInBackDeck),
            Game.Instance.InGameOptions.BackDeckTransform.position.z
            );

        if (instant)
        {
            transform.position = position;
            transform.eulerAngles = Game.Instance.InGameOptions.BackDeckTransform.eulerAngles;
        }
        else
        {

        }

        CardPlace = CardPlace.InBackDeck;
        Game.Instance.CardManager.CardsInBackDeck++;
    }

    public void GoInHand(Vector3 position, Vector3 rotation, bool instant = false, AfterTakeCardCallback afterTakeCard = null)
    {
        AfterTakeCard = afterTakeCard;
        if (instant)
        {
            transform.position = position;
            transform.eulerAngles = rotation;

            AfterTakeCard?.Invoke();

            if (_collider != null)
                _collider.enabled = true;
        }
        else
        {
            if (_moveTweenId.HasValue)
            {
                LeanTween.cancel(_moveTweenId.Value);
                _moveTweenId = null;
            }
            if (_rotateTweenId.HasValue)
            {
                LeanTween.cancel(_rotateTweenId.Value);
                _rotateTweenId = null;
            }

            _moveTweenId = LeanTween.move(gameObject, position, Game.Instance.InGameOptions.ByGameSpeed(_animationTime)).setEase(_animationType).id;
            LeanTween.descr(_moveTweenId.Value).setOnComplete(() =>
            {
                LeanTween.cancel(_moveTweenId.Value);
                _moveTweenId = null;
                AfterTakeCard?.Invoke();
            });

            _rotateTweenId = LeanTween.rotate(gameObject, rotation, Game.Instance.InGameOptions.ByGameSpeed(_animationTime)).setEase(_animationType).id;
            LeanTween.descr(_rotateTweenId.Value).setOnComplete(() => {
                LeanTween.cancel(_rotateTweenId.Value);
                _rotateTweenId = null;
            });
        }

        CardPlace = CardPlace.InPlayerHand;
    }

    public void GoToDiscarded(AfterDiscardCallback afterDiscard = null)
    {
        var position = new Vector3(
            Game.Instance.InGameOptions.DiscardedTransform.position.x,
            Game.Instance.InGameOptions.DiscardedTransform.position.y,
            Game.Instance.InGameOptions.DiscardedTransform.position.z
            );

        if (_moveTweenId.HasValue)
        {
            LeanTween.cancel(_moveTweenId.Value);
            _moveTweenId = null;
        }
        if (_rotateTweenId.HasValue)
        {
            LeanTween.cancel(_rotateTweenId.Value);
            _rotateTweenId = null;
        }

        _moveTweenId = LeanTween.move(gameObject, position, Game.Instance.InGameOptions.ByGameSpeed(_animationTime)).id;
        LeanTween.descr(_moveTweenId.Value).setOnComplete(() =>
        {
            LeanTween.cancel(_moveTweenId.Value);
            _moveTweenId = null;
            AfterDiscard?.Invoke();
        });

        _rotateTweenId = LeanTween.rotate(gameObject, Game.Instance.InGameOptions.DiscardedTransform.eulerAngles, Game.Instance.InGameOptions.ByGameSpeed(_animationTime)).id;
        LeanTween.descr(_rotateTweenId.Value).setOnComplete(() => {
            LeanTween.cancel(_rotateTweenId.Value);
            _rotateTweenId = null;
        });

        if (Game.Instance.CardManager.IsAtLeastOneCardDiscarded)
        {
            List<int> cards = Game.Instance.CardManager.GetCardsCurrentlyDiscarded();
            foreach (int cardIndex in cards)
            {
                var card = Game.Instance.CardManager.CardPool[cardIndex];
                card.GoToDiscardedDeck();
            }
        }

        Game.Instance.CardManager.IsAtLeastOneCardDiscarded = true;

        CardPlace = CardPlace.IsJustDiscarded;
        AfterDiscard = afterDiscard;
    }

    public void GoToDiscardedDeck()
    {
        StartCoroutine(GoDiscardedDeck());
    }

    IEnumerator GoDiscardedDeck()
    {
        yield return new WaitForSeconds(Game.Instance.InGameOptions.ByGameSpeed(_goToDiscardedDeckWaitTime));

        var position = new Vector3(
            Game.Instance.InGameOptions.DiscardedDeckTransform.position.x,
            Game.Instance.InGameOptions.DiscardedDeckTransform.position.y + (Game.Instance.InGameOptions.DeckYAddition * Game.Instance.CardManager.CardsInDescardedDeck),
            Game.Instance.InGameOptions.DiscardedDeckTransform.position.z
            );

        if (_moveTweenId.HasValue)
        {
            LeanTween.cancel(_moveTweenId.Value);
            _moveTweenId = null;
        }
        if (_rotateTweenId.HasValue)
        {
            LeanTween.cancel(_rotateTweenId.Value);
            _rotateTweenId = null;
        }

        _moveTweenId = LeanTween.move(gameObject, position, Game.Instance.InGameOptions.ByGameSpeed(_animationTime)).id;
        LeanTween.descr(_moveTweenId.Value).setOnComplete(() =>
        {
            LeanTween.cancel(_moveTweenId.Value);
            _moveTweenId = null;
        });

        _rotateTweenId = LeanTween.rotate(gameObject, Game.Instance.InGameOptions.DiscardedDeckTransform.eulerAngles, Game.Instance.InGameOptions.ByGameSpeed(_animationTime)).setEase(_animationType).id;
        LeanTween.descr(_rotateTweenId.Value).setOnComplete(() => {
            LeanTween.cancel(_rotateTweenId.Value);
            _rotateTweenId = null;
        });

        Game.Instance.CardManager.CardsInDescardedDeck++;
        CardPlace = CardPlace.InDiscardedDeck;
    }

    internal void ShowCard(Vector3 pos, Vector3 rot)
    {
        if (_moveTweenId.HasValue)
        {
            LeanTween.cancel(_moveTweenId.Value);
            _moveTweenId = null;
        }
        _moveTweenId = LeanTween.move(gameObject, pos, Game.Instance.InGameOptions.ByGameSpeed(_animationTime)).setEase(_animationType).id;
        LeanTween.descr(_moveTweenId.Value).setOnComplete(() =>
        {
            LeanTween.cancel(_moveTweenId.Value);
            _moveTweenId = null;
        });
        if (_rotateTweenId.HasValue)
        {
            LeanTween.cancel(_rotateTweenId.Value);
            _rotateTweenId = null;
        }
        _rotateTweenId = LeanTween.rotate(gameObject, rot, Game.Instance.InGameOptions.ByGameSpeed(_animationTime)).setEase(_animationType).id;
        LeanTween.descr(_rotateTweenId.Value).setOnComplete(() => {
            LeanTween.cancel(_rotateTweenId.Value);
            _rotateTweenId = null;
        });

        Game.Instance.CardManager.CardsInDescardedDeck++;
        //CardPlace = CardPlace.InDiscardedDeck;
    }

    bool ContainsTweenType(TweenTransformType[] types, TweenTransformType compareType)
    {
        foreach (TweenTransformType type in types)
        {
            if (type == compareType)
                return true;
        }
        return false;
    }
}
