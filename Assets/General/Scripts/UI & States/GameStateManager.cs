using System.Collections.Generic;
using UnityEngine;

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
                    Debug.LogError("[GameStateManager] Singleton not found in scene!");
            }
            return _instance;
        }
    }

    public enum GameStates
    {
        MainMenu,
        Options,
        Credits,
        Gameplay,
        Pause,
        GameOver,
        Win
    }

    private Dictionary<GameStates, IGameState> registeredGameStates = new Dictionary<GameStates, IGameState>();
    public IGameState currentGameState { get; private set; } = null;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterState(GameStates gstate, IGameState state)
    {
        if (registeredGameStates.ContainsKey(gstate))
        {
            Debug.LogWarning($"[GameStateManager] State {gstate} already registered. Overwriting.");
            registeredGameStates[gstate] = state;
            return;
        }
        registeredGameStates.Add(gstate, state);
    }

    public void SetCurrentGameState(GameStates gstate)
    {
        if (!registeredGameStates.ContainsKey(gstate))
        {
            Debug.LogError($"[GameStateManager] State {gstate} not registered!");
            return;
        }

        currentGameState?.OnStateExit();

        IGameState newState = registeredGameStates[gstate];
        newState.OnStateEnter();
        currentGameState = newState;
    }

    public bool IsInState(GameStates gstate)
    {
        if (!registeredGameStates.ContainsKey(gstate)) return false;
        return currentGameState == registeredGameStates[gstate];
    }

    private void Update()
    {
        currentGameState?.OnStateUpdate();
    }
}
