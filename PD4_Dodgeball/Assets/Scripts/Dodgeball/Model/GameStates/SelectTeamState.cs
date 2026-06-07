namespace Dodgeball.Model.GameStates
{
	public class SelectTeamState : BaseGameState
	{

		public SelectTeamState(GameStatesFSM fsm) : base(fsm)
		{

		}

		public override void OnEnter()
		{
			base.OnEnter();
            GameModel.TeamSelection = GameModel.TeamSelectionFactory();
            GameModel.TeamSelection.ReadinessChanged += TeamSelection_ReadinessChanged;
		}

		private void TeamSelection_ReadinessChanged(object sender, System.EventArgs e)
		{
			if (GameModel.TeamSelection.ReadyToPlay)
			{
				FSM.TransitionTo(FSM.CountdownState);
			}
		}

        public override void OnExit()
        {
            if (GameModel.TeamSelection != null)
            {
                GameModel.TeamSelection.ReadinessChanged -= TeamSelection_ReadinessChanged;
            }

            base.OnExit();
        }
    }
}
