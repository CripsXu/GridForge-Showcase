using System;
using UnityEngine;

public sealed class BattleUnit
{
    //身份
    public int BattleId {get;}
    public int? SourceInstanceId {get;}
    public int ConfigId{get;}
    public BattleTeam Team {get;}

    //本场战斗的基础属性
    public int MaxHp{get;}
    public int MaxMp{get;}
    public int Attack{get;}
    public float AttackIntervalSeconds{get;}
    public float AttackRange{get;}
    public float MoveSpeed{get;}

    //当前战斗状态
    public int Hp{get; private set;}
    public Vector3 Position{get; private set;}
    public int? TargetBattleId{get; private set;}
    public float AttackCooldownSeconds{get; private set;}
    public int AttackSequence { get; private set; }
    public int HitSequence { get; private set; }

    public bool IsDead => Hp <= 0;

    public BattleUnit(
        int battleId,
        int configId,
        BattleTeam team,
        int maxHp,
        int attack,
        float attackIntervalSeconds,
        float attackRange,
        float moveSpeed,
        Vector3 initialPosition,
        int? sourceInstanceId = null)
    {
        if (battleId <= 0)
            throw new ArgumentOutOfRangeException(nameof(battleId));

        if (configId <= 0)
            throw new ArgumentOutOfRangeException(nameof(configId));

        if (sourceInstanceId.HasValue && sourceInstanceId.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(sourceInstanceId));

        if (team != BattleTeam.Player && team != BattleTeam.Enemy)
            throw new ArgumentOutOfRangeException(nameof(team));

        if (maxHp <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHp));

        if (attack < 0)
            throw new ArgumentOutOfRangeException(nameof(attack));

        RequirePositiveFinite(
            attackIntervalSeconds, nameof(attackIntervalSeconds));
        RequirePositiveFinite(
            attackRange, nameof(attackRange));
        RequirePositiveFinite(
            moveSpeed, nameof(moveSpeed));

        if(float.IsNaN(initialPosition.x) ||
        float.IsInfinity(initialPosition.x) ||
        float.IsNaN(initialPosition.z) ||
        float.IsInfinity(initialPosition.z))
            throw new ArgumentException(
                "战斗位置的 X、Z 必须是有限数值。",
                nameof(initialPosition));

        BattleId = battleId;
        ConfigId= configId;
        Team = team;
        MaxHp = maxHp;
        SourceInstanceId = sourceInstanceId;
        Attack = attack;
        AttackIntervalSeconds = attackIntervalSeconds;
        AttackRange = attackRange;
        MoveSpeed = moveSpeed;
        Hp = MaxHp;
        Position = new Vector3(initialPosition.x, 0f, initialPosition.z);
        TargetBattleId = null;
        AttackCooldownSeconds = 0f;
    }

    //返回true表示本次受伤刚刚导致死亡
    public bool TakeDamage(int damage)
    {
        if(damage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage));
        }
        if(IsDead || damage == 0)
        {
            return false;
        }

        Hp = Mathf.Max(0, Hp - damage);
        HitSequence++;
        
        return IsDead;
    }

    private static void RequirePositiveFinite(float value, string paramName)
    {
        if(value <= 0f ||
        float.IsNaN(value) ||
        float.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(
                paramName, value, "必须是正的有限数值。");
        }
    }

    internal void SetTarget(int ? battleId)
    {
        if(battleId.HasValue && 
        (battleId.Value <= 0 || battleId.Value == BattleId))
        {
            throw new ArgumentOutOfRangeException(nameof(battleId));
        }

        TargetBattleId = IsDead ? null : battleId;
    }

    internal void AdvanceCooldown(float deltaTime)
    {
        if(deltaTime < 0f ||
        float.IsNaN(deltaTime) ||
        float.IsInfinity(deltaTime))
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        }

        if (IsDead) return;

        AttackCooldownSeconds = Mathf.Max(
            0f,
            AttackCooldownSeconds - deltaTime);
    }

    internal void MoveTo(Vector3 localPosition)
    {
        if (float.IsNaN(localPosition.x) ||
        float.IsInfinity(localPosition.x) ||
        float.IsNaN(localPosition.z) ||
        float.IsInfinity(localPosition.z))
        {
            throw new ArgumentException(
                "战斗位置的 X、Z 必须是有限数值。",
                nameof(localPosition));
        }

        if (IsDead)
        {
            return;
        }

        Position = new Vector3(
            localPosition.x,
            0f,
            localPosition.z
        );
    }

    internal void StartAttackCooldown()
    {
        if (IsDead || AttackCooldownSeconds > 0f)
        {
            throw new InvalidOperationException(
                "当前单位不能开始攻击冷却。");
        }

        AttackCooldownSeconds = AttackIntervalSeconds;
        AttackSequence++;
    }

}