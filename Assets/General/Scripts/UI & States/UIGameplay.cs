using UnityEngine;
using System.Collections;

public class UIGameplay : MonoBehaviour, IGameUI
{
    [Header("UI Elements")]
    [SerializeField] private WeaponUI weaponUI;
    [Tooltip("Figlio con tutto il contenuto visivo della HUD")]
    [SerializeField] private GameObject hudContent;

    private PlayerInput player;

    public void Init() { }

    public void BindPlayer(PlayerInput playerInput)
    {
        player = playerInput;
        if (player == null || weaponUI == null) return;

        weaponUI.Bind(
            player.GetComponent<WeaponManager>(),
            player.GetHealthSystem()
        );

        // Secondo bind dopo un frame — WeaponManager.Start() equipaggia
        // l'arma dopo Awake, quindi il primo bind potrebbe non trovare l'arma
        StartCoroutine(RefreshAfterFrame());
    }

    private IEnumerator RefreshAfterFrame()
    {
        yield return null;
        if (weaponUI != null && player != null)
            weaponUI.Bind(
                player.GetComponent<WeaponManager>(),
                player.GetHealthSystem()
            );
    }

    public void SetActive(bool active)
    {
        if (hudContent != null)
            hudContent.SetActive(active);
        else
            foreach (Transform child in transform)
                child.gameObject.SetActive(active);

        if (weaponUI != null) weaponUI.enabled = active;
    }

    public UIManager.UIType GetUIType() => UIManager.UIType.Gameplay;
}
