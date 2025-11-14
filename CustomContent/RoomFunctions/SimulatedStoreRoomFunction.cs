namespace BBTimes.CustomContent.RoomFunctions;

public class SimulatedStoreRoomFunction : RoomFunction
{
    public event System.Action OnPlayerEnterStore;
    public event System.Action OnPlayerExitStore;

    public override void OnFirstPlayerEnter(PlayerManager player)
    {
        base.OnFirstPlayerEnter(player);
        OnPlayerEnterStore?.Invoke();
    }

    public override void OnLastPlayerExit(PlayerManager player)
    {
        base.OnLastPlayerExit(player);
        OnPlayerExitStore?.Invoke();
    }
}