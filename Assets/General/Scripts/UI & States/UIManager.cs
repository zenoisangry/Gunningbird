using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────────────────
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<UIManager>();
            if (_instance == null)
                Debug.LogError("[UIManager] Singleton not found in scene!");
            return _instance;
        }
    }

    public enum UIType
    {
        None,
        MainMenu,
        Options,
        Credits,
        Pause,
        Gameplay,
        GameOver,
        Win
    }

    // ─── Inspector ───────────────────────────────────────────────────────────
    [Header("UI Container")]
    [Tooltip("Parent che contiene tutti i pannelli UI come figli.")]
    [SerializeField] private Transform uiContainer;

    // ─── State ───────────────────────────────────────────────────────────────
    private Dictionary<UIType, IGameUI> registeredUIs = new Dictionary<UIType, IGameUI>();
    private IGameUI currentUI = null;
    private UIType currentUIType = UIType.None;

    // ─── Lifecycle ───────────────────────────────────────────────────────────
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
            AutoRegisterUIs();
        else
            Debug.LogWarning("[UIManager] UI Container not assigned in Inspector.");
    }

    // ─── Registration ────────────────────────────────────────────────────────
    private void AutoRegisterUIs()
    {
        IGameUI[] found = uiContainer.GetComponentsInChildren<IGameUI>(true);
        foreach (IGameUI ui in found)
            RegisterUI(ui.GetUIType(), ui);

        // Nascondi tutto all'avvio
        ShowUI(UIType.None);
    }

    public void RegisterUI(UIType uiType, IGameUI uiImpl)
    {
        if (uiType == UIType.None) return;

        if (registeredUIs.ContainsKey(uiType))
        {
            Debug.LogWarning($"[UIManager] UI {uiType} already registered. Skipping.");
            return;
        }

        registeredUIs.Add(uiType, uiImpl);
        uiImpl.Init();
    }

    // ─── Show / Hide ─────────────────────────────────────────────────────────
    public void ShowUI(UIType uiType)
    {
        if (currentUIType == uiType && currentUI != null) return;

        currentUI?.SetActive(false);
        currentUIType = uiType;

        if (uiType == UIType.None)
        {
            currentUI = null;
            return;
        }

        if (registeredUIs.TryGetValue(uiType, out IGameUI next))
        {
            currentUI = next;
            currentUI.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[UIManager] UI {uiType} not registered.");
            currentUI = null;
        }
    }

    public void HideAllUI()
    {
        foreach (var kvp in registeredUIs)
            kvp.Value.SetActive(false);

        currentUI = null;
        currentUIType = UIType.None;
    }

    // ─── Player Binding ──────────────────────────────────────────────────────
    /// <summary>Rebinda la HUD al player (chiamato dopo ogni reset).</summary>
    public void RegisterPlayer(PlayerInput player)
    {
        if (player == null) return;

        if (registeredUIs.TryGetValue(UIType.Gameplay, out IGameUI ui))
        {
            if (ui is UIGameplay gameplayUI)
                gameplayUI.BindPlayer(player);
        }
    }

    // ─── Queries ─────────────────────────────────────────────────────────────
    public UIType GetCurrentUIType() => currentUIType;
    public bool IsUIActive(UIType uiType) => currentUIType == uiType;
    public IGameUI GetUI(UIType uiType)
    {
        registeredUIs.TryGetValue(uiType, out IGameUI ui);
        return ui;
    }
}
