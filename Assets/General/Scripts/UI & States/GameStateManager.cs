using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameStateManager;

public class GameStateManager : MonoBehaviour
{
    private static GameStateManager _instance;
    public static GameStateManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<GameStateManager>();
                if (_instance == null)
                {
                    Debug.LogError("Error can't instantiate singleton");
                }
            }
            return _instance;
        }
    }

    Dictionary<GameStates, IGameState> registeredGameStates = new Dictionary<GameStates, IGameState>();
    public enum GameStates
    {
        MainMenu,
        Options,
        Credits, //Cat was there for a bit! =^..^=
        Gameplay,
        Pause,
        GameOver,
    }

    public IGameState currentGameState = null;

    public void RegisterState(GameStates gstate, IGameState state)
    {
        registeredGameStates.Add(gstate, state);

    }
    public void SetCurrentGameState(GameStates gstate)
    {
        if (currentGameState != null)
        {
            currentGameState.OnStateExit();
        }
        IGameState newState = registeredGameStates[gstate];
        newState.OnStateEnter();
        currentGameState = newState;
    }

    void Update()
    {
        currentGameState?.OnStateUpdate();
    }
}