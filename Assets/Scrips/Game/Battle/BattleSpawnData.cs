using UnityEngine;

public sealed class BattleSpawnData
{
    public int ConfigId {get;}
    public BattleTeam Team {get;}

    //相对于棋盘的位置
    public Vector3 LocalPosition {get;}
    public int? SourceInstanceId {get;}

    public BattleSpawnData(
        int configId,
        BattleTeam team,
        Vector3 localPosition,
        int? sourceInstanceId = null)
    {
        ConfigId = configId;
        Team = team;
        LocalPosition = localPosition;
        SourceInstanceId = sourceInstanceId;
    }
}