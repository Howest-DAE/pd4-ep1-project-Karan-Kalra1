
using Dodgeball.Model;
using MVP.Presenter;
using UnityEngine;
using Unity.Netcode;

namespace Dodgeball.Presenter
{
	public class MatchPresenter : PresenterMonobehaviour<MatchModel>
	{
		[SerializeField]
		private ArenaPresenter _arena;





        public void StartMatch(MatchModel model)
        {
            Model = model;
            Model.StartMatch(_arena.Model);

            if (NetworkManager.Singleton != null &&
    NetworkManager.Singleton.IsServer &&
    NetworkMatchState.Instance != null)
            {
                NetworkMatchState.Instance.ResetScoresOnServer();
            }

            if (NetworkManager.Singleton != null &&
    NetworkManager.Singleton.IsServer &&
    NetworkMatchReporter.Instance != null)
            {
                NetworkMatchReporter.Instance.CreateMatchOnServer();
            }

            if (NetworkManager.Singleton != null &&
    NetworkManager.Singleton.IsServer &&
    NetworkMatchTimer.Instance != null)
            {
                NetworkMatchTimer.Instance.StartTimerOnServer();
            }

            if (NetworkManager.Singleton == null)
            {
                // Local fallback.
                _arena.SpawnBall(0);
                _arena.SpawnPlayer(model.PlayerRed.PlayerId);
                _arena.SpawnPlayer(model.PlayerBlue.PlayerId);
                return;
            }

            // Every client creates the local model through Model.StartMatch above.
            // Only the server creates networked scene objects.
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            _arena.SpawnNetworkBall(0);

            _arena.SpawnNetworkPlayer(model.PlayerRed.PlayerId);
            _arena.SpawnNetworkPlayer(model.PlayerBlue.PlayerId);
        }

    }
}
