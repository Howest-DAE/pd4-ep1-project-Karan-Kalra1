using Dodgeball.Presenter;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _lookRotPivot;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _crouchSpeedModifier = 0.3f;
    [SerializeField] private float _jumpSpeed = 10f;

    private PlayerInputActions _inputActions;
    private PlayerRotationSettings _rotationSettings;

    private Vector3 _serverMoveDirection;
    private Vector3 _serverLookDirection = Vector3.forward;
    private bool _serverJumpRequested;
    private bool _serverCrouching;
    private float _verticalSpeed;

    private float _lookRotX;
    private float _lookRotY;

    private void Awake()
    {
        enabled = false;

        if (_characterController == null)
        {
            _characterController = GetComponent<CharacterController>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            GamePresenter gamePresenter = FindAnyObjectByType<GamePresenter>();

            if (gamePresenter != null)
            {
                _inputActions = gamePresenter.InputActionConfig.InputActions;
                _rotationSettings = gamePresenter.InputActionConfig.RotationSettings;

                _inputActions.MoveAction.Enable();
                _inputActions.LookAction.Enable();
                _inputActions.JumpAction.Enable();
                _inputActions.CrouchAction.Enable();
            }

            _lookRotY = transform.rotation.eulerAngles.y;
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            ReadOwnerInputAndSendToServer();
        }

        if (IsServer)
        {
            ApplyServerMovement();
        }
    }

    private void ReadOwnerInputAndSendToServer()
    {
        if (_inputActions.MoveAction == null)
        {
            return;
        }

        Vector2 moveInput = _inputActions.MoveAction.ReadValue<Vector2>();
        Vector2 lookInput = _inputActions.LookAction.ReadValue<Vector2>();

        Transform cam = Camera.main != null ? Camera.main.transform : transform;

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveWorld = right * moveInput.x + forward * moveInput.y;
        moveWorld = Vector3.ClampMagnitude(moveWorld, 1f);

        if (_rotationSettings != null)
        {
            _lookRotY += lookInput.x * Time.deltaTime * _rotationSettings.LookRotSpeedHor;
            _lookRotX += lookInput.y * Time.deltaTime * _rotationSettings.LookRotSpeedVert;
            _lookRotX = Mathf.Clamp(
                _lookRotX,
                _rotationSettings.MinLookAngleX,
                _rotationSettings.MaxLookAngleX
            );
        }

        Vector3 lookDirection = Quaternion.Euler(_lookRotX, _lookRotY, 0f) * Vector3.forward;

        bool jumpPressed = _inputActions.JumpAction.WasPressedThisFrame();
        bool crouching = _inputActions.CrouchAction.IsPressed();

        SubmitMovementInputRpc(moveWorld, lookDirection, jumpPressed, crouching);
    }

    [Rpc(SendTo.Server)]
    private void SubmitMovementInputRpc(
        Vector3 moveDirection,
        Vector3 lookDirection,
        bool jumpPressed,
        bool crouching)
    {
        _serverMoveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            _serverLookDirection = lookDirection.normalized;
        }

        if (jumpPressed)
        {
            _serverJumpRequested = true;
        }

        _serverCrouching = crouching;
    }

    private void ApplyServerMovement()
    {
        if (_characterController == null)
        {
            return;
        }

        Vector3 horizontalLook = _serverLookDirection;
        horizontalLook.y = 0f;

        if (horizontalLook.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(horizontalLook.normalized);
        }

        if (_lookRotPivot != null && _serverLookDirection.sqrMagnitude > 0.001f)
        {
            _lookRotPivot.rotation = Quaternion.LookRotation(_serverLookDirection.normalized);
        }

        Vector3 movement = _serverMoveDirection;

        if (_serverCrouching)
        {
            movement *= _crouchSpeedModifier;
        }

        _characterController.Move(movement * _moveSpeed * Time.deltaTime);

        ApplyServerGravityAndJump();
    }

    private void ApplyServerGravityAndJump()
    {
        _verticalSpeed += Time.deltaTime * Physics.gravity.y;

        if (_serverJumpRequested)
        {
            if (_characterController.isGrounded)
            {
                _verticalSpeed = _jumpSpeed;
            }

            _serverJumpRequested = false;
        }

        _characterController.Move(_verticalSpeed * Time.deltaTime * Vector3.up);

        if (_characterController.isGrounded && _verticalSpeed < 0f)
        {
            _verticalSpeed = 0f;
        }
    }
}