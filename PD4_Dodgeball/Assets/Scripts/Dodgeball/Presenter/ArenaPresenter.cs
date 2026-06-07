using Dodgeball.Model;
using MVP.Presenter;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace Dodgeball.Presenter
{
	public class ArenaPresenter : PresenterMonobehaviour<ArenaModel>
	{
		[SerializeField]
		private SpawnArea[] _spawns;
		[SerializeField]
		private List<Transform> _ballSpawnLocations = new List<Transform>();

		[SerializeField]
		private GameObject _playerPrefab;
		[SerializeField]
		private GameObject _ballPrefab;

        [SerializeField]
        private NetworkObject _networkPlayerPrefab;

        [SerializeField]
        private NetworkObject _networkBallPrefab;

        private List<PlayerPresenter> _spawnedPlayers = new List<PlayerPresenter>();
		private List<BallPresenter> _spawnedBalls = new List<BallPresenter>();

		public ReadOnlyCollection<PlayerPresenter> SpawnedPlayers => _spawnedPlayers.AsReadOnly();
		public ReadOnlyCollection<BallPresenter> SpawnedBalls => _spawnedBalls.AsReadOnly();

		[SerializeField]
		private BallSpawner[] _ballSpawners;

		protected override void Awake()
		{
			base.Awake();
			Model = new ArenaModel();

			foreach (var spawner in _ballSpawners)
			{
				int index = RegisterSpawner(spawner.SpawnLocation);
				spawner.SpawnLocationIndex = index;
			}
		}

		//Adds a spawnlocation and stores the index
		public int RegisterSpawner(Transform transform)
		{
			_ballSpawnLocations.Add(transform);
			return _ballSpawnLocations.Count - 1;
		}

		public void SpawnBall(int spawnLocationIndex)
		{
			if (spawnLocationIndex == -1)
			{
				spawnLocationIndex = Random.Range(0, _ballSpawnLocations.Count);
			}
			Transform spawnLocation = _ballSpawnLocations[spawnLocationIndex];
			BallPresenter ballPresenter = Instantiate(_ballPrefab, spawnLocation.position, Quaternion.identity).GetComponent<BallPresenter>();
		}

		public void SpawnPlayer(ulong playerId)
		{
			PlayerModel model = Model.GetPlayer(playerId);
			var spawnLocation = GetRandomPlayerSpawn(model.Color);

			GameObject playerGo = Instantiate(_playerPrefab, spawnLocation.pos, spawnLocation.rot);
			var playerPresenter = playerGo.GetComponent<PlayerPresenter>();
			playerPresenter.PlayerId = playerId; //TODO: remove this line, will be replaced by networked playerId
		}

        public void SpawnNetworkBall(int spawnLocationIndex)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            if (spawnLocationIndex == -1)
            {
                spawnLocationIndex = Random.Range(0, _ballSpawnLocations.Count);
            }

            Transform spawnLocation = _ballSpawnLocations[spawnLocationIndex];

            NetworkObject ball = Instantiate(
                _networkBallPrefab,
                spawnLocation.position,
                Quaternion.identity
            );

            ball.Spawn();
        }

        public void SpawnNetworkPlayer(ulong playerId)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            PlayerModel model = Model.GetPlayer(playerId);

            if (model == null)
            {
                Debug.LogError($"Cannot spawn network player. No PlayerModel for {playerId}");
                return;
            }

            var spawnLocation = GetRandomPlayerSpawn(model.Color);

            NetworkObject playerObject = Instantiate(
                _networkPlayerPrefab,
                spawnLocation.pos,
                spawnLocation.rot
            );

            NetworkPlayerAvatar avatar = playerObject.GetComponent<NetworkPlayerAvatar>();

            if (avatar == null)
            {
                Debug.LogError("Network player prefab is missing NetworkPlayerAvatar.");
                Destroy(playerObject.gameObject);
                return;
            }

            avatar.SetPlayerDataOnServer(playerId, (int)model.Color);

            playerObject.SpawnWithOwnership(playerId);

            Debug.Log($"Spawned network player for ClientId={playerId}, Color={model.Color}");
        }

        public void AddPlayerPresenter(PlayerPresenter presenter)
		{
			//Don't add model to ArenaModel. Model already exists in Arena Model at this point (created by game)
			_spawnedPlayers.Add(presenter);
		}

		public void AddBall(BallPresenter presenter)
		{
			Model.AddBall(presenter.Model);
			_spawnedBalls.Add(presenter);
		}

		public BallPresenter GetBallPresenter(ulong ballId)
		{
			return _spawnedBalls.FirstOrDefault(b => b.Model.Id == ballId);
		}
		public BallPresenter GetBallPresenter(BallModel model)
		{
			return _spawnedBalls.FirstOrDefault(b => b.Model == model);
		}
		public PlayerPresenter GetPlayerPresenter(ulong playerId)
		{
			return _spawnedPlayers.FirstOrDefault(b => b.PlayerId == playerId);
		}
		public PlayerPresenter GetPlayerPresenter(PlayerModel model)
		{
			return _spawnedPlayers.FirstOrDefault(p => p.Model == model);
		}

		//Player spawn randomization
		public (Vector3 pos, Quaternion rot) GetRandomPlayerSpawn(PlayerColor color)
		{
			var spawns = _spawns.Where(s => s.Color == color).ToList();
			SpawnArea spawn = spawns.Any() ?
				(spawns.Count == 1 ? spawns[0] : spawns[Random.Range(0, spawns.Count)])
				: null;

			Quaternion rotation = Quaternion.identity;
			if (spawn != null)
			{
				rotation = spawn.transform.rotation;
			}
			return (spawn?.GetRandomVector() ?? Vector3.zero, rotation);
		}

	}
}
