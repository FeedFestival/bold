using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CardPlace
{
    InBackDeck,
    InDiscardedDeck,
    IsJustDiscarded,
    InPlayerHand
}

public enum AfterDiscardAction
{
    TakeFromBackDeck,
    TakeFromDiscardedDeck,
    TakeFromJustDiscarded
}

public enum AIStrategy
{
    Simple,
    StackingStrategy,
    TwoMovesInAdvance
}

public enum TurnAction
{
    None,
    DiscardCards,
    TakeFromJustDiscarded,
    TakeFromDiscardedDeck
}

public enum TweenTransformType
{
    Move, Rotate, Scale
}

public class Game : MonoBehaviour
{
    //public GameObject Card1;

    private static Game _game;
    public static Game Instance { get { return _game; } }

    public CardManager CardManager;
    public InGameOptions InGameOptions;

    public MyTurnPanelController MyTurnPanelController;
    public ResultsTablePanel ResultsTablePanel;

    public List<PlayerObject> Players;
    public int PlayersPlaying;

    [HideInInspector]
    public int UserPlayerIndex;

    [HideInInspector]
    public int? PlayerThatCalledBold;

    public bool CanBold;

    private int _curentPlayerTurn;

    private int _currentGameLowestPoints = 999;

    private bool _playedAtLeastOneGame;

    void Awake()
    {
        _game = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        if (PlayersPlaying < Players.Count)
        {
            int length = Players.Count - PlayersPlaying;
            for (var i = 0; i < length; i++)
            {
                var indexToRemove = Players.FindIndex(p => p.PlayerPosition >= PlayersPlaying);
                //Debug.Log("i: " + i + ", indexToRemove: " + indexToRemove);
                Players.RemoveAt(indexToRemove);
            }
        }
        Players = Players.OrderBy(p => p.PlayerTurn).ToList();

        UserPlayerIndex = Players.FindIndex(p => p.IsAI == false);

        CardManager.Init(Players.Count);

        StartAnotherGame();
    }

    public void StartAnotherGame()
    {
        ResultsTablePanel.gameObject.SetActive(false);

        CardManager.Shuffle();

        SplitCardToPlayers();

        PlaceCardInBackDeck();

        if (_playedAtLeastOneGame == false)
            _curentPlayerTurn = 0;
        NextPlayerTurn();
    }

    public void SplitCardToPlayers()
    {
        int?[] thisPlayersCards;
        int playerIndex = 0;
        foreach (PlayerObject player in Players)
        {
            player.PlayerIndex = playerIndex;
            playerIndex++;

            thisPlayersCards = CardManager.GetInitialCardsForPlayer();
            player.Init(thisPlayersCards);
        }
    }

    private void PlaceCardInBackDeck()
    {
        CardManager.CardsInBackDeck = 0;
        CardManager.CardsInDescardedDeck = 0;
        CardManager.IsAtLeastOneCardDiscarded = false;

        var cardsNotInHands = CardManager.CardPool.Where(c => c.CardPlace == CardPlace.InBackDeck);
        int index = cardsNotInHands.Count() - 1;
        foreach (Card card in cardsNotInHands)
        {
            card.BackdeckIndex = index;
            index--;

            card.GoToBackDeck(instant: true);
        }
    }

    private IEnumerator _doTurn;

    public void NextPlayerTurn()
    {
        if (_doTurn != null)
            StopCoroutine(_doTurn);
        _doTurn = DoTurn();
        StartCoroutine(_doTurn);
    }

    IEnumerator DoTurn()
    {
        yield return new WaitForSeconds(InGameOptions.ByGameSpeed(0.5f));

        var playersPlayingTurn = Players.Where(p => p.IsPlayingTurn == true).Count();
        if (playersPlayingTurn == 0)
        {
            if (_curentPlayerTurn == Players.Count)
                _curentPlayerTurn = 0;

            CanBold = true;

            if (Players[_curentPlayerTurn].IsAI)
                Players[_curentPlayerTurn].AiPlayTurn();
            else
            {
                MyTurnPanelController.StartTurn();
                Players[_curentPlayerTurn].PlayTurn();
            }

            _curentPlayerTurn++;
        }
        _doTurn = null;
    }

    public void Bold()
    {
        if (CanBold == false)
            return;

        if (PlayerThatCalledBold.HasValue == false)
            PlayerThatCalledBold = UserPlayerIndex;

        if (Players[PlayerThatCalledBold.Value].IsPlayingTurn == false)
        {
            Debug.Log("Someone that is not his turn is trying to BOLD;");
            return;
        }

        if (Players[PlayerThatCalledBold.Value].PointsInHand > 7)
            return;

        foreach (var player in Players)
        {
            float timesOfSuspensions;
            if (player.PlayerIndex == PlayerThatCalledBold.Value)
                timesOfSuspensions = 0.35f;
            else
                timesOfSuspensions = UnityEngine.Random.Range(0.6f, 2.5f);
            StartCoroutine(ShowCards(timesOfSuspensions, player.PlayerIndex));

            if (player.PointsInHand < _currentGameLowestPoints)
                _currentGameLowestPoints = player.PointsInHand;
        }

        CanBold = false;
        Players[PlayerThatCalledBold.Value].IsPlayingTurn = false;
        _playedAtLeastOneGame = true;
        MyTurnPanelController.HideActions();

        StartCoroutine(EndGame());
    }

    IEnumerator ShowCards(float time, int index)
    {
        yield return new WaitForSeconds(InGameOptions.ByGameSpeed(time));

        Players[index].ShowCards();
    }

    IEnumerator EndGame()
    {
        yield return new WaitForSeconds(InGameOptions.ByGameSpeed(3f));

        int numberOfPlayersWithLowestPoints = Players.Where(p => p.PointsInHand == _currentGameLowestPoints).Count();
        bool playerThatCalledBoldLost = true;
        int totalPointsOfAllPlayers = 0;

        for (var i = 0; i < Players.Count; i++)
        {
            totalPointsOfAllPlayers += Players[i].PointsInHand;

            if (_currentGameLowestPoints == Players[i].PointsInHand)
            {
                if (i == PlayerThatCalledBold.Value)
                    playerThatCalledBoldLost = false;

                if (numberOfPlayersWithLowestPoints == 1)
                    Players[i].PointsInHand = 0;
            }
        }

        if (playerThatCalledBoldLost)
            Players[PlayerThatCalledBold.Value].PointsInHand = totalPointsOfAllPlayers;

        _currentGameLowestPoints = 999;
        PlayerThatCalledBold = null;

        yield return new WaitForSeconds(InGameOptions.ByGameSpeed(5f));

        ResultsTablePanel.gameObject.SetActive(true);
        ResultsTablePanel.AddGameScores();
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.A))
        {
            ResultsTablePanel.gameObject.SetActive(true);
            ResultsTablePanel.AddGameScores();
        }

        if (Input.GetKeyUp(KeyCode.S))
        {
            Bold();
        }
    }
}
