using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class NetworkBall : NetworkBehaviour
{
    public NetworkVariable<ulong> HolderClientId = new(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsHeld = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );


    public NetworkVariable<ulong> LastThrowerClientId = new(
    ulong.MaxValue,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);

    public NetworkVariable<int> LastThrowerTeamIndex = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsActivePlayerBall = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Rigidbody _rigidbody;
    private Collider _collider;
    private NetworkPlayerThrow _holder;

    [SerializeField]
    private float _maxActiveSeconds = 3f;

    private float _activeUntilTime;

    [SerializeField]
    private float _pickupBlockAfterThrowSeconds = 0.5f;

    private float _pickupBlockedUntilTime;
    private ulong _pickupBlockedClientId = ulong.MaxValue;

    public bool Held => IsHeld.Value;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
    }

    private void LateUpdate()
    {
        if (!IsServer) return;
        if (!IsHeld.Value) return;
        if (_holder == null || _holder.HoldPoint == null) return;

        transform.position = _holder.HoldPoint.position;
        transform.rotation = _holder.HoldPoint.rotation;
    }

    public void PickUpOnServer(ulong holderClientId, NetworkPlayerThrow holder)
    {
        if (!IsServer) return;
        if (holder == null) return;

        _holder = holder;

        NetworkPlayerAvatar avatar = holder.GetComponent<NetworkPlayerAvatar>();

        int teamIndex = -1;

        if (avatar != null)
        {
            teamIndex = avatar.TeamIndex.Value;
        }

        HolderClientId.Value = holderClientId;
        LastThrowerClientId.Value = holderClientId;
        LastThrowerTeamIndex.Value = teamIndex;
        IsHeld.Value = true;
        IsActivePlayerBall.Value = false;

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;

        if (_collider != null)
        {
            _collider.enabled = false;
        }

        transform.SetParent(null, true);
        transform.position = holder.HoldPoint.position;
        transform.rotation = holder.HoldPoint.rotation;

        Debug.Log($"Ball picked up by client {holderClientId}, team {teamIndex}");
    }

    public bool CanBePickedUpBy(ulong clientId)
    {
        if (IsHeld.Value)
        {
            return false;
        }

        // A thrown ball is dangerous and should hit players,
        // not be picked up by them.
        if (IsActivePlayerBall.Value)
        {
            return false;
        }

        if (clientId == _pickupBlockedClientId &&
            Time.time < _pickupBlockedUntilTime)
        {
            return false;
        }

        return true;
    }

    public void ThrowOnServer(Vector3 origin, Vector3 velocity)
    {
        if (!IsServer) return;

        _holder = null;

        HolderClientId.Value = ulong.MaxValue;
        IsHeld.Value = false;
        IsActivePlayerBall.Value = true;
        _activeUntilTime = Time.time + _maxActiveSeconds;

        _pickupBlockedClientId = LastThrowerClientId.Value;
        _pickupBlockedUntilTime = Time.time + _pickupBlockAfterThrowSeconds;

        transform.SetParent(null, true);
        transform.position = origin;

        if (_collider != null)
        {
            _collider.enabled = true;
        }

        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        _rigidbody.WakeUp();

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.linearVelocity = velocity;

        Debug.Log(
            $"Ball thrown with velocity {velocity}. " +
            $"Thrower={LastThrowerClientId.Value}, " +
            $"Team={LastThrowerTeamIndex.Value}"
        );
    }

    private void FixedUpdate()
    {
        if (!IsServer)
        {
            return;
        }

        if (!IsActivePlayerBall.Value)
        {
            return;
        }

        if (IsHeld.Value)
        {
            return;
        }

        if (Time.time >= _activeUntilTime)
        {
            Debug.Log("Ball active timeout reached. Deactivating ball.");
            IsActivePlayerBall.Value = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer)
        {
            return;
        }

        Debug.Log($"Ball collision with {collision.collider.name}. Active={IsActivePlayerBall.Value}");

        if (!IsActivePlayerBall.Value)
        {
            return;
        }

        NetworkPlayerAvatar hitPlayer =
            collision.collider.GetComponentInParent<NetworkPlayerAvatar>();

        if (hitPlayer != null)
        {
            Debug.Log($"Ball collided with player client {hitPlayer.PlayerClientId.Value}");

            Vector3 hitPoint = collision.GetContact(0).point;
            TryRegisterPlayerHit(hitPlayer, hitPoint);

            // Whether it was valid, self-hit, or same-team hit,
            // after touching a player the ball is no longer an active scoring ball.
            IsActivePlayerBall.Value = false;
            return;
        }

        // If the ball hits the floor, wall, obstacle, etc.,
        // it missed the player and should become pickup-able again.
        Debug.Log($"Ball hit environment/object '{collision.collider.name}', deactivating active ball.");

        IsActivePlayerBall.Value = false;
    }

    private bool TryRegisterPlayerHit(NetworkPlayerAvatar hitPlayer, Vector3 hitPoint)
    {
        ulong victimClientId = hitPlayer.PlayerClientId.Value;
        int victimTeamIndex = hitPlayer.TeamIndex.Value;

        ulong attackerClientId = LastThrowerClientId.Value;
        int attackerTeamIndex = LastThrowerTeamIndex.Value;

        if (attackerClientId == ulong.MaxValue)
        {
            Debug.Log("Hit ignored: no attacker registered.");
            return false;
        }

        if (attackerClientId == victimClientId)
        {
            Debug.Log("Hit ignored: player hit themselves.");
            return false;
        }

        if (attackerTeamIndex == victimTeamIndex)
        {
            Debug.Log("Hit ignored: same team.");
            return false;
        }

        Debug.Log(
            $"Valid player hit! Attacker={attackerClientId}, Victim={victimClientId}, " +
            $"AttackerTeam={attackerTeamIndex}, VictimTeam={victimTeamIndex}"
        );

        if (NetworkMatchState.Instance == null)
        {
            Debug.LogError("Valid hit detected, but NetworkMatchState.Instance is null.");
            return true;
        }

        NetworkMatchState.Instance.RegisterHitOnServer(
            attackerClientId,
            victimClientId,
            attackerTeamIndex,
            hitPoint
        );

        return true;
    }
}