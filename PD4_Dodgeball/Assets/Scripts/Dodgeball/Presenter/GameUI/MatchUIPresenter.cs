using Dodgeball.Model;
using MVP.Presenter;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dodgeball.Presenter
{
	public class MatchUIPresenter : PresenterMonobehaviour<MatchModel>
	{
		[SerializeField]
		private GameUIPresenter _gamePresenter;

		private Label _redScoreLabel, _blueScoreLabel, _timerLabel;

		private void OnEnable()
		{
            if (_gamePresenter == null)
            {
                Debug.LogWarning("MatchUIPresenter: GamePresenter is not assigned.");
                return;
            }

            if (_gamePresenter.Model == null)
            {
                Debug.LogWarning("MatchUIPresenter: GamePresenter.Model is null.");
                return;
            }

            if (_gamePresenter.Model.CurrentMatch == null)
            {
                Debug.LogWarning("MatchUIPresenter enabled before CurrentMatch exists. Disable MatchPanel at scene start.");
                return;
            }

            Model = _gamePresenter.Model.CurrentMatch;


            UIDocument document = GetComponent<UIDocument>();
			_redScoreLabel = document.rootVisualElement.Q<Label>("RedScore");
			_blueScoreLabel = document.rootVisualElement.Q<Label>("BlueScore");
			_timerLabel = document.rootVisualElement.Q<Label>("Timer");

			UpdateBlueScore();
			UpdateRedScore();
			UpdateTimerText();
		}

		protected override void OnModelPropertyChanged(string propertyName)
		{
			base.OnModelPropertyChanged(propertyName);
			switch (propertyName)
			{
				case nameof(Model.ScoreRed):
					UpdateRedScore();
					break;
				case nameof(Model.ScoreBlue):
					UpdateBlueScore();
					break;
				case nameof(Model.SecondsLeft):
				case nameof(Model.MinutesLeft):
					UpdateTimerText();
					break;
			}
		}

		void UpdateRedScore()
		{
			_redScoreLabel.text = Model.ScoreRed.ToString();
		}

		void UpdateBlueScore()
		{
			_blueScoreLabel.text = Model.ScoreBlue.ToString();
		}

		void UpdateTimerText()
		{
			_timerLabel.text = $"{Model.MinutesLeft:00}:{Model.SecondsLeft:00}";
		}

	}
}
