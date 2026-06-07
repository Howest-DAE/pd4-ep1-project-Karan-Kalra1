namespace Dodgeball.Model.GameStates
{
    public class WaitingForOtherPlayerState : BaseGameState
    {
        public WaitingForOtherPlayerState(GameStatesFSM fsm) : base(fsm)
        {
        }

        public override void OnEnter()
        {
            base.OnEnter();
            // Real transition is now triggered by GameNetworkBridge
            // when NetworkPlayerRegistry reports 2 connected players.
        }
    }
}