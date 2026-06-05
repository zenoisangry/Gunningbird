using UnityEngine;

public class UIGameplay : MonoBehaviour, IGameUI
{
    [Header("UI Elements")]
    [SerializeField] private WeaponUI weaponUI;
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
    }

    // GameplayHUD sta FUORI da UIContainer — il root non viene mai toccato da UIManager.
    // SetActive agisce solo su hudContent.
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
