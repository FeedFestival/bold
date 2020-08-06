using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameSpeed
{
    Normal, Fast, VeryFast
}

public class InGameOptions : MonoBehaviour
{
    public Transform BackDeckTransform;
    public Transform DiscardedDeckTransform;
    public Transform DiscardedTransform;

    [HideInInspector]
    public readonly float DeckYAddition = 0.01f;

    public GameSpeed GameSpeed;

    public Color32 DisabledTextColor;
    public Color32 AvailableTextColor;

    public Color32 TotalTextColor;

    public float ByGameSpeed(float time)
    {
        switch (GameSpeed)
        {
            case GameSpeed.Normal:
                return time;
            case GameSpeed.Fast:
                return time / 2;
            case GameSpeed.VeryFast:
                return time / 3;
            default:
                return time;
        }
    }
}
