using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Singleton")]
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<UIManager>();
            if (_instance == null)
                Debug.LogError("Error can't instantiate singleton");
            return _instance;
        }
    }

    public enum UIType
    {
        None,
        MainMenu,
        Options,
        Pause,
        Gameplay,
        GameOver
    }

    [Header("UI References")]
    [SerializeField] private Transform uiContainer;

    private Dictionary<UIType, IGameUI> registeredUIs = new Dictionary<UIType, IGameUI>();
    
    private IGameUI currentUI = null;
    private UIType currentUIType = UIType.None;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (uiContainer != null)
        {
            AutoRegisterUIs();
        }
        else
        {
            Debug.LogWarning("[UIManager] UI Container not assigned.");
        }
    }

    private void AutoRegisterUIs()
    {
        IGameUI[] foundUIs = uiContainer.GetComponentsInChildren<IGameUI>(true);
        
        foreach (IGameUI ui in foundUIs)
        {
            RegisterUI(ui.GetUIType(), ui);
        }

        ShowUI(UIType.None);
    }

    public void RegisterUI(UIType uiType, IGameUI uiImplementation)
    {
        if (registeredUIs.ContainsKey(uiType))
        {
            return;
        }

        registeredUIs.Add(uiType, uiImplementation);
        uiImplementation.Init();
    }

    public void ShowUI(UIType uiType)
    {
        if (currentUIType == uiType && currentUI != null)
        {
            return;
        }

        if (currentUI != null)
        {
            currentUI.SetActive(false);
        }

        currentUIType = uiType;
        
        if (uiType == UIType.None)
        {
            currentUI = null;
            return;
        }

        if (registeredUIs.ContainsKey(uiType))
        {
            currentUI = registeredUIs[uiType];
            currentUI.SetActive(true);
        }
        else
        {
            currentUI = null;
        }
    }

    public void HideAllUI()
    {
        foreach (var kvp in registeredUIs)
        {
            kvp.Value.SetActive(false);
        }
        currentUI = null;
        currentUIType = UIType.None;
    }

    public UIType GetCurrentUIType()
    {
        return currentUIType;
    }

    public bool IsUIActive(UIType uiType)
    {
        return currentUIType == uiType;
    }

    public IGameUI GetUI(UIType uiType)
    {
        if (registeredUIs.ContainsKey(uiType))
        {
            return registeredUIs[uiType];
        }
        return null;
    }
    public void RegisterPlayer(PlayerInput player)
    {
        if (player == null) return;

        if (registeredUIs.TryGetValue(UIType.Gameplay, out IGameUI ui))
        {
            if (ui is UIGameplay gameplayUI)
            {
                gameplayUI.BindPlayer(player);
            }
        }
    }
}