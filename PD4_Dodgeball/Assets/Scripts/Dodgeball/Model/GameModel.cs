using Dodgeball.Model.GameStates;
using MVP.Model;
using System;

namespace Dodgeball.Model
{
	/// <summary>
	/// Game Model covers the entire game flow starting with team selection up until the Game Over screen.
	/// States are controlled by the GameStatesFSM using the State Pattern.
	/// </summary>
	public class GameModel : ModelBase
	{
		public event EventHandler<EventArgs<MatchModel>> MatchStarted;
		public MatchModel CurrentMatch { get; private set; }
		public SelectTeamModel TeamSelection { get; set; }

		public Func<SelectTeamModel> TeamSelectionFactory { get; set; } =
	() => new SelectTeamModel();

		private float _countdownTimer;

		public float CountDownTime
		{
			get => _countdownTimer;
			set
			{
				if (_countdownTimer == value)
					return;
				_countdownTimer = value;
				OnPropertyChanged();
			}
		}

		public enum GameState
		{
			SelectTeam,
			WaitForOthers,
			CountDown,
			Playing,
			GameOver
		}


		private GameState _currentGameState;

		public GameState CurrentGameState
		{
			get => _currentGameState;
			set
			{
				//if(_currentGameState.Equals(value))
				if (_currentGameState == value)
					return;
				_currentGameState = value;
				OnPropertyChanged();
			}
		}

		private GameStatesFSM _statesFSM;

		public GameModel()
		{
			_statesFSM = new GameStatesFSM(this);
			SetCurrentGameState();
			_statesFSM.StateChanged += _statesFSM_StateChanged;

		}
		private void SetCurrentGameState()
		{
			CurrentGameState = _statesFSM.CurrentState switch
			{
				WaitingForOtherPlayerState => GameState.WaitForOthers,
				SelectTeamState => GameState.SelectTeam,
				CountdownState => GameState.CountDown,
				MatchPlayingState => GameState.Playing,
				_ => GameState.GameOver,
			};
		}
		private void _statesFSM_StateChanged(object sender, System.EventArgs e)
		{
			SetCurrentGameState();
		}

		public MatchModel StartNewMatch()
		{
			ulong redPlayerId = TeamSelection.GetPlayerIdForColor(PlayerColor.Red);
			ulong bluePlayerId = TeamSelection.GetPlayerIdForColor(PlayerColor.Blue);
			CurrentMatch = new MatchModel(redPlayerId, bluePlayerId);

			OnMatchStarted(CurrentMatch);
			return CurrentMatch;
		}

		protected virtual void OnMatchStarted(MatchModel matchModel)
		{
			MatchStarted?.Invoke(this, new EventArgs<MatchModel>(matchModel));
		}

		public void Update(float deltaTime)
		{
			_statesFSM.Update(deltaTime);
		}

        public void NotifyEnoughPlayersConnected()
        {
            if (_statesFSM.CurrentState == _statesFSM.WaitingForOtherPlayerState)
            {
                _statesFSM.TransitionTo(_statesFSM.SelectTeamState);
            }
        }

        public void NotifyNetworkGameOver()
        {
            if (_statesFSM.CurrentState == _statesFSM.GameOverState)
            {
                return;
            }

            _statesFSM.TransitionTo(_statesFSM.GameOverState);
        }

    }
}
