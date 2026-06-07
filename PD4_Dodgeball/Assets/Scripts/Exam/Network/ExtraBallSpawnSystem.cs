using Unity.Netcode;
using UnityEngine;

public class ExtraBallSpawnSystem : NetworkBehaviour
{
    public static ExtraBallSpawnSystem Instance { get; private set; }

    [SerializeField]
    private NetworkObject _networkBallPrefab;

    [SerializeField]
    private Transform[] _extraBallSpawnPoints;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RequestSpawnExtraBall(int spawnIndex)
    {
        if (!IsClient)
        {
            return;
        }

        RequestSpawnExtraBallRpc(spawnIndex);
    }

    [Rpc(SendTo.Server)]
    private void RequestSpawnExtraBallRpc(int spawnIndex, RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (_networkBallPrefab == null)
        {
            Debug.LogError("Cannot spawn extra ball: network ball prefab is missing.");
            return;
        }

        if (_extraBallSpawnPoints == null ||
            spawnIndex < 0 ||
            spawnIndex >= _extraBallSpawnPoints.Length ||
            _extraBallSpawnPoints[spawnIndex] == null)
        {
            Debug.LogWarning($"Client {senderClientId} requested invalid extra ball spawn index {spawnIndex}.");
            return;
        }

        Transform spawnPoint = _extraBallSpawnPoints[spawnIndex];

        NetworkObject ball = Instantiate(
            _networkBallPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        ball.Spawn();

        Debug.Log($"Extra ball spawned by server for client {senderClientId} at spawn index {spawnIndex}.");
    }
}