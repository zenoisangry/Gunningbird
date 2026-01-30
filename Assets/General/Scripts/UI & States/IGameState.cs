public interface IGameState
{
    public void OnStateEnter();

    public void OnStateUpdate();

    public void OnStateExit();
}