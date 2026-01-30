public interface IGameUI
{
    void Init();
    void SetActive(bool active);
    UIManager.UIType GetUIType();
}