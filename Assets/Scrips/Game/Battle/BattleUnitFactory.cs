using System;
using UnityEngine;

public sealed class BattleUnitFactory
{
    private readonly UnitConfigTable _unitConfigs;
    private readonly float _cellSpacing;
    private readonly float _moveSpeed;

    //玩家和敌人共用这一套战斗编号
    private int _nextBattleId = 1;

    public BattleUnitFactory(
        UnitConfigTable unitConfigs,
        float cellSpacing,
        float moveSpeed)
    {
        _unitConfigs = unitConfigs ??
    throw new ArgumentNullException(nameof(unitConfigs));
        RequirePositiveFinite(cellSpacing, nameof(cellSpacing));
        RequirePositiveFinite(moveSpeed, nameof(moveSpeed));

        _cellSpacing = cellSpacing;
        _moveSpeed = moveSpeed;
    }

    public BattleUnit CreatePlayer(
        UnitInstance source,
        Vector3 initialPosition)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        
        return Create(
            source.ConfigId,
            BattleTeam.Player,
            initialPosition,
            source.InstanceId);
    }

    public BattleUnit CreateEnemy(
        int configId,
        Vector3 initialPosition)
    {
        return Create(
            configId,
            BattleTeam.Enemy,
            initialPosition,
            null);
    }

    public BattleUnit Create(
        int configId,
        BattleTeam team,
        Vector3 initialPosition,
        int? sourceInstanceId = null)
    {
        var config = _unitConfigs.Get(configId);
        int battleId = _nextBattleId;
        int nextBattleId = checked(battleId + 1);

        var unit = new BattleUnit(
            battleId: battleId,
            configId: config.id,
            team: team,
            maxHp: config.maxHp,
            attack: config.attack,
            attackIntervalSeconds: config.attackIntervalSeconds,
            attackRange: config.attackRangeCells * _cellSpacing,
            moveSpeed: _moveSpeed,
            initialPosition: initialPosition,
            sourceInstanceId: sourceInstanceId);

        //配置查询和单位构建都成功后，才更新下一个战斗编号
        _nextBattleId = nextBattleId;
        return unit;
    }

    private static void RequirePositiveFinite(
    float value,
    string paramName)
    {
        if (value <= 0f ||
            float.IsNaN(value) ||
            float.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(
                paramName, value, "必须是正的有限数值。");
        }
    }
}