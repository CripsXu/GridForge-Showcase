
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class UnitActorManager : IDisposable
{
    // 一个 InstanceId 对应一个 UnitActor。
    private readonly Dictionary<int, UnitActor> _actors = new();
    // 准备阶段：InstanceId → Actor，已有的 _actors 继续使用。

    // 本场战斗：BattleId → Actor，战后统一清理。
    private readonly Dictionary<int, UnitActor> _battleActors = new();

    private readonly UnitActor _unitPrefab;
    private readonly MatchResources _matchResources;
    private readonly Transform _root;
    private readonly UnitConfigTable _unitConfigs;

    private bool _disposed;

    public UnitActorManager(
        UnitActor unitPrefab,
        MatchResources matchResources,
        Transform root, UnitConfigTable unitConfigs)
    {
        _unitPrefab = unitPrefab
            ?? throw new ArgumentNullException(nameof(unitPrefab));

        _matchResources = matchResources
            ?? throw new ArgumentNullException(nameof(matchResources));

        _unitConfigs = unitConfigs ?? throw new ArgumentNullException(nameof(unitConfigs));
        _root = root
            ?? throw new ArgumentNullException(nameof(root));
    }
    
    

    public UnitActor GetOrCreate(int instanceId, int configId)
    {
        ThrowIfDisposed();

        if (instanceId <= 0)
            throw new ArgumentOutOfRangeException(nameof(instanceId));

        if (configId <= 0)
            throw new ArgumentOutOfRangeException(nameof(configId));

        // 1. 优先复用已有对象。
        if (_actors.TryGetValue(instanceId, out var actor))
        {
            if (actor != null)
                return actor;

            // Unity 对象已被外部销毁，清理失效记录。
            _actors.Remove(instanceId);
        }

        actor = CreateActor(configId);

        try
        {
            actor.Bind(instanceId);
            _actors.Add(instanceId, actor);
            return actor;
        }
        catch
        {
            DestroyActor(actor);
            throw;
        }
    }

    private UnitActor CreateActor(int configId, bool enemy = false)
    {
        bool hasModel = _matchResources.TryGetUnitPrefab(
            configId, out var modelPrefab);

        var actor = UnityEngine.Object.Instantiate(
            _unitPrefab, _root, false);

        try
        {
            actor.transform.localPosition = Vector3.zero;
            actor.transform.localRotation = Quaternion.identity;

            var visual = actor.GetComponent<UnitVisual>();

            if (visual == null)
                throw new InvalidOperationException(
                    "棋子模板根节点未挂 UnitVisual。");

            visual.Initialize(hasModel ? modelPrefab : null);
            actor.InitializeName(_unitConfigs.Get(configId).name, enemy);
            return actor;
        }
        catch
        {
            DestroyActor(actor);
            throw;
        }
    }

    private static void DestroyActor(UnitActor actor)
    {
        if (actor == null)
            return;

        actor.gameObject.SetActive(false);
        UnityEngine.Object.Destroy(actor.gameObject);
    }

    public UnitActor GetOrCreateBattle(int battleId, int configId, BattleTeam team)
    {
        ThrowIfDisposed();

        if (battleId <= 0)
            throw new ArgumentOutOfRangeException(nameof(battleId));

        if (configId <= 0)
            throw new ArgumentOutOfRangeException(nameof(configId));

        if (_battleActors.TryGetValue(battleId, out var actor) &&
            actor != null)
        {
            return actor;
        }

        actor = CreateActor(configId, team == BattleTeam.Enemy);
        _battleActors[battleId] = actor;

        return actor;
    }

    public void ClearBattleActors()
    {
        foreach (var actor in _battleActors.Values)
            DestroyActor(actor);

        _battleActors.Clear();
    }

    public bool TryGet(int instanceId, out UnitActor actor)
    {
        ThrowIfDisposed();

        if (instanceId <= 0)
            throw new ArgumentOutOfRangeException(nameof(instanceId));

        if (_actors.TryGetValue(instanceId, out actor))
        {
            if (actor != null)
                return true;

            // 清理已经被 Unity 销毁的对象记录。
            _actors.Remove(instanceId);
        }

        actor = null;
        return false;
    }

    public void Release(int instanceId)
    {
        ThrowIfDisposed();

        if (instanceId <= 0)
            throw new ArgumentOutOfRangeException(nameof(instanceId));

        RemoveActor(instanceId);
    }

    private void RemoveActor(int instanceId)
    {
        if (!_actors.TryGetValue(instanceId, out var actor))
            return;

        // 先解除管理器的所有权登记。
        _actors.Remove(instanceId);

        if (actor == null)
            return;

        actor.gameObject.SetActive(false);
        UnityEngine.Object.Destroy(actor.gameObject);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ClearBattleActors();

        // 清理本局仍由管理器持有的全部棋子。
        foreach (var actor in _actors.Values)
        {
            if (actor == null)
                continue;

            actor.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(actor.gameObject);
        }

        _actors.Clear();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UnitActorManager));
    }
}
