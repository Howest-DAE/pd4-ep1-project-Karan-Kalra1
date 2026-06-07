using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkThrowTrajectoryPreview : NetworkBehaviour
{
    [SerializeField]
    private NetworkPlayerThrow _networkThrow;

    [SerializeField]
    private LineRenderer _lineRenderer;

    [SerializeField]
    private int _steps = 30;

    [SerializeField]
    private float _timeStep = 0.06f;

    [SerializeField]
    private LayerMask _collisionMask = ~0;

    private void Awake()
    {
        if (_networkThrow == null)
        {
            _networkThrow = GetComponent<NetworkPlayerThrow>();
        }

        if (_lineRenderer == null)
        {
            _lineRenderer = GetComponentInChildren<LineRenderer>();
        }

        Hide();
    }

    private void Update()
    {
        if (!IsOwner)
        {
            Hide();
            return;
        }

        if (_networkThrow == null || !_networkThrow.HasHeldBall)
        {
            Hide();
            return;
        }

        DrawTrajectory();
    }

    private void DrawTrajectory()
    {
        Transform holdPoint = _networkThrow.HoldPoint;

        if (holdPoint == null || _lineRenderer == null)
        {
            Hide();
            return;
        }

        Vector3 startPosition = holdPoint.position;
        Vector3 startVelocity = _networkThrow.GetCurrentAimVelocity();

        _lineRenderer.enabled = true;
        _lineRenderer.positionCount = _steps;

        Vector3 previousPoint = startPosition;

        for (int i = 0; i < _steps; i++)
        {
            float time = i * _timeStep;

            Vector3 point =
                startPosition +
                startVelocity * time +
                0.5f * Physics.gravity * time * time;

            if (i > 0)
            {
                Vector3 direction = point - previousPoint;
                float distance = direction.magnitude;

                if (distance > 0.001f &&
                    Physics.Raycast(previousPoint, direction.normalized, out RaycastHit hit, distance, _collisionMask))
                {
                    _lineRenderer.positionCount = i + 1;
                    _lineRenderer.SetPosition(i, hit.point);
                    return;
                }
            }

            _lineRenderer.SetPosition(i, point);
            previousPoint = point;
        }
    }

    private void Hide()
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
        }
    }
}