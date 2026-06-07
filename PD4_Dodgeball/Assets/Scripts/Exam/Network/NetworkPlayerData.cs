using System;
using Unity.Collections;
using Unity.Netcode;

public struct NetworkPlayerData : INetworkSerializable, IEquatable<NetworkPlayerData>
{
    public ulong ClientId;
    public FixedString64Bytes PlayFabId;
    public FixedString64Bytes DisplayName;

    public bool IsHost;

    // -1 = no team selected yet
    // 0 = first team
    // 1 = second team
    public int TeamIndex;

    public bool HasSelectedTeam;
    public bool IsReady;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayFabId);
        serializer.SerializeValue(ref DisplayName);
        serializer.SerializeValue(ref IsHost);
        serializer.SerializeValue(ref TeamIndex);
        serializer.SerializeValue(ref HasSelectedTeam);
        serializer.SerializeValue(ref IsReady);
    }

    public bool Equals(NetworkPlayerData other)
    {
        return ClientId == other.ClientId &&
               PlayFabId.Equals(other.PlayFabId) &&
               DisplayName.Equals(other.DisplayName) &&
               IsHost == other.IsHost &&
               TeamIndex == other.TeamIndex &&
               HasSelectedTeam == other.HasSelectedTeam &&
               IsReady == other.IsReady;
    }

    public override string ToString()
    {
        return
            $"ClientId={ClientId}, " +
            $"PlayFabId={PlayFabId}, " +
            $"DisplayName={DisplayName}, " +
            $"IsHost={IsHost}, " +
            $"TeamIndex={TeamIndex}, " +
            $"HasSelectedTeam={HasSelectedTeam}, " +
            $"IsReady={IsReady}";
    }
}