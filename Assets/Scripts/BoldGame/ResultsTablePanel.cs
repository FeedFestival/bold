using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultsTablePanel : MonoBehaviour
{
    public GridLayoutGroup GridLayoutGroup;

    public GameObject PlayerHeader;
    public GameObject PlayerScore;

    public bool IsInitialised;

    private Text[] _header;
    private Text[,] _scores;

    private int _gameIndex;

    private readonly int _totalLevelIndex = 3;

    public void Init()
    {
        IsInitialised = true;

        GridLayoutGroup.constraintCount = Game.Instance.PlayersPlaying;

        int levels = 5;
        int headersCount = Game.Instance.PlayersPlaying;
        int scoreCount = Game.Instance.PlayersPlaying * (levels - 1);

        _header = new Text[Game.Instance.PlayersPlaying];

        // we start from one so we can skip the example one !!!! IMPORTANT!
        for (int i = 0; i < headersCount; i++)
        {
            var go = Instantiate(PlayerHeader, GridLayoutGroup.transform);
            go.name = "playersHeader_[" + "players" + i + "]";
            go.transform.SetParent(GridLayoutGroup.transform);
            go.transform.localScale = new Vector3(1, 1, 1);

            _header[i] = go.transform.GetChild(0).GetComponent<Text>();
            _header[i].text = "Player " + i;
        }
        Destroy(PlayerHeader);

        _scores = new Text[levels - 1, Game.Instance.PlayersPlaying];

        int level = 0;
        int gridIndex = 0;

        for (int i = 0; i < scoreCount; i++)
        {
            var go = Instantiate(PlayerScore, GridLayoutGroup.transform);
            go.name = "score_[" + "players" + i + "]";
            go.transform.SetParent(GridLayoutGroup.transform);
            go.transform.localScale = new Vector3(1, 1, 1);

            _scores[level, gridIndex] = go.GetComponent<Text>();
            _scores[level, gridIndex].text = "";

            if (level == 3)
                _scores[level, gridIndex].color = Game.Instance.InGameOptions.TotalTextColor;

            if ((gridIndex + 1) == Game.Instance.PlayersPlaying)
            {
                level++;
                gridIndex = 0;
            }
            else
            {
                gridIndex++;
            }
        }
        Destroy(PlayerScore);

    }

    public void AddGameScores()
    {
        if (_scores == null)
            Init();

        if (_gameIndex < 3)
        {
            for (int i = 0; i < Game.Instance.PlayersPlaying; i++)
            {
                _scores[_gameIndex, i].text = Game.Instance.Players[i].PointsInHand.ToString();
            }
        }
        else
        {
            for (int i = 0; i < Game.Instance.PlayersPlaying; i++)
            {
                _scores[0, i].text = _scores[1, i].text;
                _scores[1, i].text = _scores[2, i].text;
                _scores[2, i].text = Game.Instance.Players[i].PointsInHand.ToString();
            }
        }
        _gameIndex++;

        for (int i = 0; i < Game.Instance.PlayersPlaying; i++)
        {
            int total = 0;
            System.Int32.TryParse(_scores[_totalLevelIndex, i].text, out total);

            total = total + Game.Instance.Players[i].PointsInHand;
            _scores[_totalLevelIndex, i].text = total.ToString();

            Game.Instance.Players[i].TotalPoints = total;
        }


    }
}
