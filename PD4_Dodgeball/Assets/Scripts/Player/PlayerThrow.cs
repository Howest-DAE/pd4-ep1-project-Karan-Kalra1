using Dodgeball.Model;
using MVP.Presenter;
using UnityEngine;

namespace Dodgeball.Presenter
{
	public class PlayerThrow : PresenterMonobehaviour<PlayerModel>
	{
		[SerializeField]
		private Transform _aimTransform;

		[SerializeField]
		private Transform _grabPivot;

		[SerializeField]
		private float _throwSpeed = 10f;


        public Transform AimTransform => _aimTransform;
        public Transform GrabPivot => _grabPivot;
        public float ThrowSpeed => _throwSpeed;

        private PlayerPresenter _playerPresenter;
		private IBallThrowingStrategy _throwingStrategy;

		public IBallThrowingStrategy ThrowingStrategy
		{
			get => _throwingStrategy;
			set
			{
				if (_throwingStrategy == value) return;

				if (_throwingStrategy != null)
				{
					_throwingStrategy.ThrowBallRequested -= _throwingStrategy_ThrowBallRequested;
					_throwingStrategy.GrabBallRequested -= _throwingStrategy_GrabBallRequested;
				}

				_throwingStrategy = value;
				if (_throwingStrategy == null) return;

				_throwingStrategy.ThrowBallRequested += _throwingStrategy_ThrowBallRequested;
				_throwingStrategy.GrabBallRequested += _throwingStrategy_GrabBallRequested;
			}
		}

		private void OnDestroy()
		{
			ThrowingStrategy = null;
		}

		private void _throwingStrategy_GrabBallRequested(object sender, System.EventArgs e)
		{
			Model.TryGrabBall();
		}

		private void _throwingStrategy_ThrowBallRequested(object sender, System.EventArgs e)
		{

			Model.TryThrowBall(CalculateAimVelocity().ToNumericsVector());
		}


		public bool HasBall => Model?.GrabbedBall != null;

		protected override void Awake()
		{
			base.Awake();
			_playerPresenter = GetComponent<PlayerPresenter>();

		}
		protected override void Start()
		{
			base.Start();
			Model = _playerPresenter.Model;
		}

		protected override void Update()
		{
			ThrowingStrategy?.Update();
			base.Update();
		}


		//Model changes

		protected override void OnModelUpdated(PlayerModel previousModel)
		{
			Model.BallThrown += Model_BallThrown;
		}

		protected override void OnModelPropertyChanged(string propertyName)
		{
			switch (propertyName)
			{
				case nameof(Model.GrabbedBall):
					SetGrabbedBall();
					break;
			}
		}

		private void SetGrabbedBall()
		{
			if (Model.GrabbedBall == null) return;
			BallPresenter presenter = _playerPresenter.ArenaPresenter.GetBallPresenter(Model.GrabbedBall);
			presenter.transform.SetParent(_grabPivot);
			presenter.transform.localPosition = Vector3.zero;

		}

		//Gets invoked when the ball needs to be thrown
		private void Model_BallThrown(object sender, ThrowBallEventArgs e)
		{
			var ballPresenter = _playerPresenter.ArenaPresenter.GetBallPresenter(e.Ball);
			ballPresenter.transform.parent = null;
			ballPresenter.Throw(e.Velocity.ToUnityVector());
		}
		public Vector3 CalculateAimVelocity()
		{
			return _aimTransform.forward * _throwSpeed;
		}
		public Vector3 GetGrabPosition()
		{
			return _grabPivot.transform.position;
		}

        // Triggers
        void OnTriggerEnter(Collider other)
        {
            if (!isActiveAndEnabled) return;
            if (Model == null) return;
            if (!other.CompareTag("Ball")) return;

            BallPresenter ball = other.GetComponent<BallPresenter>();
            if (ball == null || ball.Model == null) return;

            Model.SetBallInGrabRange(ball.Model, true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isActiveAndEnabled) return;
            if (Model == null) return;
            if (!other.CompareTag("Ball")) return;

            BallPresenter ball = other.GetComponent<BallPresenter>();
            if (ball == null || ball.Model == null) return;

            Model.SetBallInGrabRange(ball.Model, false);
        }
    }
}
