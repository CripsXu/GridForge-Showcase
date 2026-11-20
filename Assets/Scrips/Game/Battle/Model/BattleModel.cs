using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleModel : IModel
{
    private readonly Dictionary<int, BattleUnit> _unitsById = new();

    private bool _disposed;

    public IReadOnlyCollection<BattleUnit> Units
    {
        get
        {
            ThrowIfDisposed();
            return _unitsById.Values;
        }
    }

    //包括存活和阵亡单位
    public int Count => Units.Count;

    public BattleOutcome Outcome {get; private set;}

    public void Initialize()
    {
        ThrowIfDisposed();
    }

    internal void AddUnit(BattleUnit unit)
    {
        ThrowIfDisposed();

        if (unit == null)
            throw new ArgumentNullException(nameof(unit));

        if (_unitsById.ContainsKey(unit.BattleId))
        {
            throw new InvalidOperationException(
                $"战斗单位编号重复：{unit.BattleId}");
        }

        //同一个阵营的同一个棋子只能添加一次
        if(unit.SourceInstanceId.HasValue)
        {
            foreach(var existing in _unitsById.Values)
            {
                if(existing.Team == unit.Team &&
                existing.SourceInstanceId == unit.SourceInstanceId)
                {
                    throw new InvalidOperationException(
                        $"同一个阵营的同一个棋子只能添加一次：{unit.Team} {unit.SourceInstanceId}");
                }
            }
        }

        _unitsById.Add(unit.BattleId, unit);
    }

    public BattleUnit GetUnit(int battleId)
    {
        if(TryGetUnit(battleId, out var unit))
        {
            return unit;
        }
        else
        {
            throw new KeyNotFoundException(
                $"找不到战斗单位：{battleId}");
        }
    }

    public bool TryGetUnit(
    int battleId,
    out BattleUnit unit)
    {
        ThrowIfDisposed();

        return _unitsById.TryGetValue(battleId, out unit);
    }

    public int GetAliveCount(BattleTeam team)
    {
        ThrowIfDisposed();

        if(team != BattleTeam.Player && team != BattleTeam.Enemy)
            throw new ArgumentOutOfRangeException(nameof(team));
        
        int count = 0;

        foreach(var unit in _unitsById.Values)
        {
            if(unit.Team == team && !unit.IsDead)
                count++;
        }

        return count;
    }

    internal void SetOutcome(BattleOutcome outcome)
    {
        ThrowIfDisposed();

        if (outcome != BattleOutcome.PlayerWin &&
            outcome != BattleOutcome.EnemyWin &&
            outcome != BattleOutcome.Draw)
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        if (Outcome != BattleOutcome.None)
        {
            throw new InvalidOperationException(
                "本场战斗已有结果"
            );
        }

        Outcome = outcome;
    }

    internal void Clear()
    {
        ThrowIfDisposed();
        _unitsById.Clear();
        Outcome = BattleOutcome.None;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _unitsById.Clear();
        _disposed = true;
        Outcome = BattleOutcome.None;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BattleModel));
    }
}