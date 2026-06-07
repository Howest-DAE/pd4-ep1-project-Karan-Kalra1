using Dodgeball.Model;
using MVP.Presenter;
using UnityEngine;
using Unity.Netcode;

namespace Dodgeball.Presenter
{
	public class PlayerPresenter : PresenterMonobehaviour<PlayerModel>
	{
		//Inspector fields
		[SerializeField]
		private MeshRenderer[] _coloredParts;
		[SerializeField]
		private Material _redMaterial, _blueMaterial;
		[SerializeField]
		private LayerMask _jumpObstaclesMask;
		[SerializeField]
		private GameObject _hitParticlesPrefab;

		[SerializeField]
		private GameObject _aimVisual;


        private bool _isLocalNetworkPlayer;
        private NetworkPlayerThrow _networkThrow;

        //Properties
        public PlayerColor Color => Model.Color;
		public ArenaPresenter ArenaPresenter { get; set; }
		public IBallThrowingStrategy ThrowStrategy { get; set; }


		//TODO: replace with Networked PlayerId
		public ulong PlayerId { get; set; }

        private bool _isNetworkPlayerInitialized;

        protected override void Awake()
        {
            base.Awake();

            // If this prefab has a NetworkObject, do not enable local AI by default.
            // NetworkPlayerAvatar will initialize it after the NetworkVariables arrive.
            if (TryGetComponent<NetworkObject>(out _))
            {
                DisableLocalControlScripts();
                return;
            }

            SetControlledByPlayer(false);
        }

        protected override void Start()
        {
            if (_isNetworkPlayerInitialized)
            {
                base.Start();
                return;
            }

            // Non-network fallback / old local mode.
            if (!TryGetComponent<NetworkObject>(out _))
            {
                ArenaPresenter = FindAnyObjectByType<ArenaPresenter>();
                Model = ArenaPresenter.Model.GetPlayer(PlayerId);
                ArenaPresenter.AddPlayerPresenter(this);

                SetControlledByPlayer(PlayerId == 0);

                base.Start();
            }
        }

        protected override void OnModelUpdated(PlayerModel previousModel)
		{
			if (previousModel != null)
			{
				previousModel.HitByPlayerBall -= Model_HitByBall;
			}

			if (Model != null)
			{
				Model.HitByPlayerBall += Model_HitByBall;
				UpdatePlayerColor();
			}
		}

		private void Model_HitByBall(object sender, PlayerHitEventArgs e)
		{
			BallPresenter ball = ArenaPresenter.GetBallPresenter(e.Ball);
			if (ball == null) return;

			var particles = Instantiate(_hitParticlesPrefab).GetComponent<HitParticles>();
			particles.Spawn(e.SourcePlayerColor, ball.transform.position, this);
		}

		protected override void OnModelPropertyChanged(string propertyName)
		{
			if (propertyName == nameof(Model.GrabbedBall))
			{
				SetAimVisible(Model.GrabbedBall != null);
			}
			base.OnModelPropertyChanged(propertyName);
		}

		public void SetControlledByPlayer(bool controlled)
		{
			if (controlled)
			{
				GamePresenter presenter = FindAnyObjectByType<GamePresenter>();
				PlayerInputActions inputActions = presenter.InputActionConfig.InputActions;
				GetComponent<PlayerMovement>().MoveStrategy = new PlayerInputMoveStrategy(transform, Camera.main.transform, inputActions, presenter.InputActionConfig.RotationSettings);
				GetComponent<PlayerThrow>().ThrowingStrategy = new InputBallThrowStrategy(inputActions);
			}
			else
			{
				AITargetSelector targetSelector = new AITargetSelector(this);
				GetComponent<PlayerMovement>().MoveStrategy = new PlayerAIMoveStrategy(ArenaPresenter, this, targetSelector, _jumpObstaclesMask);
				GetComponent<PlayerThrow>().ThrowingStrategy = new AIBallThrowingStrategy(targetSelector, this);
			}
			GetComponent<PlayerCameraAttach>().enabled = controlled;

		}

		private void UpdatePlayerColor()
		{
			Material colorMat = Color == PlayerColor.Red ? _redMaterial : _blueMaterial;
			foreach (var renderer in _coloredParts)
			{
				renderer.sharedMaterial = colorMat;
			}
		}

        private void SetAimVisible(bool visible)
        {
            if (_aimVisual != null)
            {
                _aimVisual.SetActive(visible);
            }
        }
        // Update is called once per frame
        protected override void Update()
        {
            if (_isNetworkPlayerInitialized)
            {
                UpdateNetworkVisuals();
            }
            else
            {
                ThrowStrategy?.Update();
            }

            base.Update();
        }

        public void InitializeNetworkPlayer(ulong playerId, bool isLocalOwner)
        {
            if (_isNetworkPlayerInitialized)
            {
                return;
            }

            PlayerId = playerId;

            ArenaPresenter = FindAnyObjectByType<ArenaPresenter>();

            if (ArenaPresenter == null)
            {
                Debug.LogError("ArenaPresenter not found while initializing network player.");
                return;
            }

            Model = ArenaPresenter.Model.GetPlayer(PlayerId);

            if (Model == null)
            {
                Debug.LogError($"No PlayerModel found for network player {PlayerId}.");
                return;
            }

            ArenaPresenter.AddPlayerPresenter(this);

            DisableLocalControlScripts();

            _isLocalNetworkPlayer = isLocalOwner;
            _networkThrow = GetComponent<NetworkPlayerThrow>();

            SetAimVisible(false);

            PlayerCameraAttach cameraAttach = GetComponent<PlayerCameraAttach>();
            if (cameraAttach != null)
            {
                cameraAttach.enabled = isLocalOwner;
            }

            _isNetworkPlayerInitialized = true;

            Debug.Log($"Initialized network player. ClientId={PlayerId}, IsLocalOwner={isLocalOwner}");
        }

        private void DisableLocalControlScripts()
        {
            PlayerMovement movement = GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.enabled = false;
            }

            PlayerThrow thrower = GetComponent<PlayerThrow>();
            if (thrower != null)
            {
                thrower.enabled = false;
            }

            PlayerCameraAttach cameraAttach = GetComponent<PlayerCameraAttach>();
            if (cameraAttach != null)
            {
                cameraAttach.enabled = false;
            }
        }

        private void UpdateNetworkVisuals()
        {
            if (_networkThrow == null)
            {
                return;
            }

            // Only the local owner should see their own aim/trajectory preview.
            SetAimVisible(_isLocalNetworkPlayer && _networkThrow.HasHeldBall);
        }

        public void PlayNetworkHitEffect(PlayerColor sourcePlayerColor, Vector3 hitPosition)
        {
            if (_hitParticlesPrefab == null)
            {
                Debug.LogWarning("PlayerPresenter: Hit particles prefab is missing.");
                return;
            }

            HitParticles particles = Instantiate(_hitParticlesPrefab).GetComponent<HitParticles>();

            if (particles == null)
            {
                Debug.LogWarning("PlayerPresenter: Hit particles prefab does not have HitParticles component.");
                return;
            }

            particles.Spawn(sourcePlayerColor, hitPosition, this);
        }
    }
}
