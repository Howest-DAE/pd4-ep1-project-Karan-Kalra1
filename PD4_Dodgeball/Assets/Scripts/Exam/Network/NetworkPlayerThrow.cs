using Dodgeball.Presenter;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerThrow : NetworkBehaviour
{
    [Header("Input")]
    [SerializeField]
    private InputActionReference _throwAction;

    [Header("Settings")]
    [SerializeField]
    private float _pickupDistance = 2.5f;

    private PlayerThrow _oldThrow;
    private NetworkBall _heldBall;

    public NetworkVariable<bool> IsHoldingBall = new(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);

    public Transform HoldPoint => _oldThrow != null ? _oldThrow.GrabPivot : transform;

    public bool HasHeldBall => IsHoldingBall.Value;

    public Vector3 GetCurrentAimVelocity()
    {
        return CalculateAimVelocity();
    }

    private void Awake()
    {
        enabled = false;
        _oldThrow = GetComponent<PlayerThrow>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner && _throwAction != null)
        {
            _throwAction.action.Enable();
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (_throwAction == null) return;

        if (_throwAction.action.WasPressedThisFrame())
        {
            Vector3 velocity = CalculateAimVelocity();
            RequestThrowRpc(velocity);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner)
        {
            return;
        }

        NetworkBall ball = other.GetComponent<NetworkBall>();

        if (ball == null)
        {
            ball = other.GetComponentInParent<NetworkBall>();
        }

        if (ball == null)
        {
            return;
        }

        NetworkObject ballNetworkObject = ball.NetworkObject;

        if (ballNetworkObject == null || !ballNetworkObject.IsSpawned)
        {
            Debug.LogWarning(
                $"Ignoring ball pickup trigger because ball is not spawned. " +
                $"Object={ball.gameObject.name}"
            );
            return;
        }

        RequestPickupRpc(ballNetworkObject);
    }

    [Rpc(SendTo.Server)]
    private void RequestPickupRpc(NetworkObjectReference ballReference)
    {
        if (_heldBall != null) return;

        if (!ballReference.TryGet(out NetworkObject ballObject))
        {
            return;
        }

        NetworkBall ball = ballObject.GetComponent<NetworkBall>();

        if (ball == null)
        {
            return;
        }

        if (!ball.CanBePickedUpBy(OwnerClientId))
        {
            Debug.Log("Pickup rejected: ball is held or pickup is temporarily blocked.");
            return;
        }

        float distance = Vector3.Distance(transform.position, ball.transform.position);

        if (distance > _pickupDistance)
        {
            Debug.LogWarning($"Pickup rejected. Distance={distance}");
            return;
        }

        ball.PickUpOnServer(OwnerClientId, this);
        _heldBall = ball;
        IsHoldingBall.Value = true;
    }

    [Rpc(SendTo.Server)]
    private void RequestThrowRpc(Vector3 velocity)
    {
        if (_heldBall == null)
        {
            return;
        }

        Vector3 direction = velocity.normalized;
        Vector3 origin = HoldPoint.position + direction * 1.5f;

        _heldBall.ThrowOnServer(origin, velocity);
        _heldBall = null;
        IsHoldingBall.Value = false;
    }

    private Vector3 CalculateAimVelocity()
    {
        if (_oldThrow != null && _oldThrow.AimTransform != null)
        {
            return _oldThrow.AimTransform.forward * _oldThrow.ThrowSpeed;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform.forward * 10f;
        }

        return transform.forward * 10f;
    }
}