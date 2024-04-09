using System;
using UnityEngine;

[Serializable]
public class GameObjectActiveState
{
    public GameObject gameObject;
    public bool active;
    private bool _initialState;

    public void SetState()
    {
        if (gameObject != null)
        {
            _initialState = gameObject.activeSelf;
            gameObject.SetActive(active);
        }
    }

    public void ResetState()
    {
        if (gameObject != null)
        {
            gameObject.SetActive(_initialState);
        }
    }
}

public static class GameObjectActiveStateExt
{
    public static void SetStates(this GameObjectActiveState[] states)
    {
        foreach (var state in states)
        {
            state.SetState();
        }
    }

    public static void ResetStates(this GameObjectActiveState[] states)
    {
        foreach (var state in states)
        {
            state.ResetState();
        }
    }
}