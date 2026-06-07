using System.Collections.Generic;
using UnityEngine;

public class ExtraBallSpawnerTrigger : MonoBehaviour
{
    [SerializeField]
    private int _spawnIndex;

    [SerializeField]
    private ExtraBallPurchasePanel _purchasePanel;

    private readonly HashSet<Collider> _localPlayerCollidersInside = new();

    private void OnTriggerEnter(Collider other)
    {
        NetworkPlayerAvatar avatar = other.GetComponentInParent<NetworkPlayerAvatar>();

        if (avatar == null || !avatar.IsOwner)
        {
            return;
        }

        _localPlayerCollidersInside.Add(other);

        if (_purchasePanel == null)
        {
            Debug.LogWarning("ExtraBallPurchasePanel is not assigned.");
            return;
        }

        Debug.Log($"Local player entered extra ball spawner. Colliders inside: {_localPlayerCollidersInside.Count}");

        _purchasePanel.Show(_spawnIndex);
    }

    private void OnTriggerExit(Collider other)
    {
        NetworkPlayerAvatar avatar = other.GetComponentInParent<NetworkPlayerAvatar>();

        if (avatar == null || !avatar.IsOwner)
        {
            return;
        }

        _localPlayerCollidersInside.Remove(other);

        Debug.Log($"Local player exited extra ball spawner. Colliders inside: {_localPlayerCollidersInside.Count}");

        if (_localPlayerCollidersInside.Count == 0 && _purchasePanel != null)
        {
            _purchasePanel.Hide();
        }
    }
}