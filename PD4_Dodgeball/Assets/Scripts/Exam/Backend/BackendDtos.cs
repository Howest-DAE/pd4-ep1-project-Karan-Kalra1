using System;
using System.Collections.Generic;

[Serializable]
public class PlayerCreateDto
{
    public string playFabId;
    public string displayName;
}

[Serializable]
public class PlayerDto
{
    public string playFabId;
    public string displayName;
}

[Serializable]
public class CreateMatchDto
{
    public string hostPlayFabId;
    public string clientPlayFabId;
}

[Serializable]
public class MatchCreatedDto
{
    public int matchId;
}

[Serializable]
public class PlayerHitCreateDto
{
    public string attackerPlayFabId;
    public string victimPlayFabId;
}

[Serializable]
public class MatchPlayerInfoDto
{
    public string playFabId;
    public string displayName;
    public bool isHost;
    public int pointsScored;
    public int timesHit;
    public int crownsRemaining;
}

[Serializable]
public class MatchSummaryDto
{
    public int matchId;
    public List<MatchPlayerInfoDto> players;
}