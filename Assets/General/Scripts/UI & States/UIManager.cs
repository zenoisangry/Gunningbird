using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null) _instance = FindAnyObjectByType<UIManager>();
            if (_instance == null) Debug.LogError("[UIManager] Singleton not found!");
            return _instance;
        }
    }

    public enum UIType
    {
        None, MainMenu, Options, Credits, Pause, Gameplay, GameOver, Win
    }

    [Header("UI Container (solo pannelli menu — NON GameplayHUD)")]
    [SerializeField] private Transform uiContainer;

    [Header("Gameplay HUD (separato — non figlio di UIContainer)")]
    [SerializeField] private UIGameplay gameplayHUD;

    private Dictionary<UIType, IGameUI> registeredUIs = new Dictionary<UIType, IGameUI>();
    private UIType currentUIType = UIType.None;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (uiContainer != null)
            AutoRegisterUIs();
        else
            Debug.LogWarning("[UIManager] UI Container not assigned.");

        // Registra GameplayHUD separatamente se assegnato
        if (gameplayHUD != null)
            RegisterUI(UIType.Gameplay, gameplayHUD);
    }

    private void AutoRegisterUIs()
    {
        IGameUI[] found = uiContainer.GetComponentsInChildren<IGameUI>(true);
        foreach (IGameUI ui in found)
            RegisterUI(ui.GetUIType(), ui);

        // Nascondi tutti i pannelli menu
        foreach (var kvp in registeredUIs)
            kvp.Value.SetActive(false);
    }

    public void RegisterUI(UIType uiType, IGameUI uiImpl)
    {
        if (uiType == UIType.None) return;
        if (registeredUIs.ContainsKey(uiType)) return;
        registeredUIs.Add(uiType, uiImpl);
        uiImpl.Init();
    }

    public void ShowUI(UIType uiType)
    {
        // Nascondi tutti i pannelli menu (non tocca GameplayHUD — lo gestisce UIGameplay.SetActive)
        foreach (var kvp in registeredUIs)
            kvp.Value.SetActive(false);

        currentUIType = uiType;
        if (uiType == UIType.None) return;

        if (registeredUIs.TryGetValue(uiType, out IGameUI next))
            next.SetActive(true);
        else
            Debug.LogWarning($"[UIManager] UI '{uiType}' not registered. Registrati: {string.Join(", ", registeredUIs.Keys)}");
    }

    public void HideAllUI()
    {
        foreach (var kvp in registeredUIs)
            kvp.Value.SetActive(false);
        currentUIType = UIType.None;
    }

    /// <summary>
    /// Chiamato da GameManager dopo il reload della scena.
    /// Trova il nuovo uiContainer nella scena caricata e ri-registra tutte le UI.
    /// </summary>
    public void ReRegisterUIs()
    {
        registeredUIs.Clear();
        currentUIType = UIType.None;

        // Cerca il container nella nuova scena
        if (uiContainer == null)
        {
            var found = FindAnyObjectByType<UIManager>();
            if (found != null && found != this)
                uiContainer = found.uiContainer;
        }

        if (uiContainer != null)
            AutoRegisterUIs();

        // Re-registra GameplayHUD
        if (gameplayHUD != null)
            RegisterUI(UIType.Gameplay, gameplayHUD);
        else
        {
            gameplayHUD = FindAnyObjectByType<UIGameplay>();
            if (gameplayHUD != null)
                RegisterUI(UIType.Gameplay, gameplayHUD);
        }
    }

    public void RegisterPlayer(PlayerInput player)
    {
        if (player == null) return;
        if (gameplayHUD != null)
            gameplayHUD.BindPlayer(player);
    }

    public UIType  GetCurrentUIType()        => currentUIType;
    public bool    IsUIActive(UIType uiType) => currentUIType == uiType;
    public IGameUI GetUI(UIType uiType)
    {
        registeredUIs.TryGetValue(uiType, out IGameUI ui);
        return ui;
    }
}
