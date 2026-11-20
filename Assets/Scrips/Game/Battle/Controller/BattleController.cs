using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class BattleController : IController
{
    private readonly BattleModel _model;
    private readonly UnitConfigTable _unitConfigs;

    private bool _initialized;
    private bool _disposed;

    public bool IsRunning { get; private set; }

    private const float RangeEpsilon = 0.0001f;
    private const float MaxBattleSeconds = 60f;

    private float _elapsedSeconds;

    private readonly List<(BattleUnit Target, int Damage)>
        _pendingAttacks = new();

    private readonly List<(BattleUnit Unit, Vector3 Position)>
        _pendingMoves = new();

    public BattleController(BattleModel model, UnitConfigTable unitConfigs)
    {
        _model = model
            ?? throw new ArgumentNullException(nameof(model));

        _unitConfigs = unitConfigs
            ?? throw new ArgumentNullException(nameof(unitConfigs));
    }

    public void Initialize()
    {
        ThrowIfDisposed();
        _initialized = true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        IsRunning = false;
        ResetProgress();
        _initialized = false;
        _disposed = true;

    }

    private void ThrowIfNotReady()
    {
        ThrowIfDisposed();

        if (!_initialized)
        {
            throw new InvalidOperationException(
                "BattleController 尚未初始化。");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(
                nameof(BattleController));
        }
    }

    public void StartBattle(
        IReadOnlyList<BattleSpawnData> spawns,
        float cellSpacing,
        float moveSpeed)
    {
        ThrowIfNotReady();

        if (IsRunning)
        {
            throw new InvalidOperationException(
                "战斗正在运行，不能重复开始。");
        }

        if (spawns == null)
            throw new ArgumentNullException(nameof(spawns));

        if (spawns.Count == 0)
        {
            throw new ArgumentException(
                "出生列表不能为空。",
                nameof(spawns));
        }

        //每场使用一个新的工厂，双方共用战斗编号
        var factory = new BattleUnitFactory(
            _unitConfigs,
            cellSpacing,
            moveSpeed);

        var candidate = new BattleModel();
        candidate.Initialize();

        try
        {
            foreach(var spawn in spawns)
            {
                if(spawn == null)
                    throw new ArgumentException(
                        "出生列表中不能包含 null 元素。",
                        nameof(spawns));
                if (spawn.Team == BattleTeam.Player &&
                    !spawn.SourceInstanceId.HasValue)
                {
                    throw new ArgumentException(
                        "玩家参战单位必须关联实例",
                        nameof(spawns));
                }

                var unit = factory.Create(
                    spawn.ConfigId,
                    spawn.Team,
                    spawn.LocalPosition,
                    spawn.SourceInstanceId);
                
                candidate.AddUnit(unit);
            }

            if(candidate.GetAliveCount(BattleTeam.Player) == 0 ||
            candidate.GetAliveCount(BattleTeam.Enemy) == 0)
            {
                throw new InvalidOperationException(
                "当前版本要求双方至少各有一只参战单位。");
            }

            //常见输入与配置错误检查完了再提交阵容
            _model.Clear();

            foreach (var unit in candidate.Units)
            {
                _model.AddUnit(unit);
            }
        }
        finally
        {
            candidate.Dispose();
        }

        ResetProgress();
        IsRunning = true;
    }

    public void Tick(float deltaTime)
    {
        ThrowIfNotReady();

        if (deltaTime < 0f ||
            float.IsNaN(deltaTime) ||
            float.IsInfinity(deltaTime))
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        }

        if (!IsRunning || deltaTime == 0f)
            return;
        
        //每次执行tick都要清空
        _pendingMoves.Clear();
        _pendingAttacks.Clear();

        _elapsedSeconds += deltaTime;

        //第一阶段：决策，不修改位置和血量
        foreach (var unit in _model.Units)
        {
            if(unit.IsDead) continue;

            unit.AdvanceCooldown(deltaTime);

            var target = FindTarget(unit);
            unit.SetTarget(target?.BattleId);

            if (target == null)
            {
                continue;
            }

            float distance = Vector3.Distance(
                unit.Position,
                target.Position);

            if(distance <= unit.AttackRange + RangeEpsilon)
            {
                if(unit.AttackCooldownSeconds > 0f)
                    continue;

                unit.StartAttackCooldown();

                _pendingAttacks.Add(
                    (Target: target,
                    Damage: unit.Attack));
            }
            else
            {
                float step = Mathf.Min(
                    unit.MoveSpeed * deltaTime,
                    distance - unit.AttackRange);

                Vector3 nextPosition = Vector3.MoveTowards(
                    unit.Position,
                    target.Position,
                    step);

                _pendingMoves.Add((
                    Unit: unit,
                    Position: nextPosition));
            }
        }

        //第二阶段：应用所有移动
        foreach (var move in _pendingMoves)
        {
            move.Unit.MoveTo(move.Position);
        }

        //第三阶段：应用所有已经形成的攻击
        foreach (var attack in _pendingAttacks)
        {
            attack.Target.TakeDamage(attack.Damage);
        }

        CheckBattleEnd();
    }

    private BattleUnit FindTarget(BattleUnit unit)
    {
        //目标还有效就继续攻击当前目标
        if(unit.TargetBattleId.HasValue &&
        _model.TryGetUnit(
            unit.TargetBattleId.Value,
            out var currentTarget) &&
            !currentTarget.IsDead &&
            currentTarget.Team != unit.Team)
        {
            return currentTarget;
        }

        BattleUnit nearest = null;
        float nearsetDistanceSquared = float.PositiveInfinity;

        foreach (var candidate in _model.Units)
        {
            if (candidate.IsDead || candidate.Team == unit.Team)
            {
                continue;
            }

            float distanceSquared = 
            (candidate.Position - unit.Position).sqrMagnitude;

            if (nearest == null ||
            distanceSquared < nearsetDistanceSquared ||
            (distanceSquared == nearsetDistanceSquared &&
            candidate.BattleId < nearest.BattleId))
            {
                nearest = candidate;
                nearsetDistanceSquared = distanceSquared;
            }
        }
        return nearest;
    }


    private void CheckBattleEnd()
    {
        int playerAlive = _model.GetAliveCount(BattleTeam.Player);
        int enemyAlive = _model.GetAliveCount(BattleTeam.Enemy);

        if (playerAlive == 0 && enemyAlive == 0)
        {
            EndBattle(BattleOutcome.Draw);
        }
        else if (enemyAlive == 0)
        {
            EndBattle(BattleOutcome.PlayerWin);
        }
        else if (playerAlive == 0)
        {
            EndBattle(BattleOutcome.EnemyWin);
        }
        else if (_elapsedSeconds >= MaxBattleSeconds)
        {
            EndBattle(BattleOutcome.Draw);
        }
    }

    private void EndBattle(BattleOutcome outcome)
    {
        if (!IsRunning)
            return;

        _model.SetOutcome(outcome);
        IsRunning = false;
    }

    private void ResetProgress()
    {
        _elapsedSeconds = 0f;
        _pendingMoves.Clear();
        _pendingAttacks.Clear();
    }

    public void ClearBattle()
    {
        ThrowIfNotReady();

        IsRunning = false;
        ResetProgress();
        _model.Clear();

    }
}