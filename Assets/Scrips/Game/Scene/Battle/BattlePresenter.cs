using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattlePresenter : MonoBehaviour
{
    [SerializeField] private Transform _gridRoot;

    [SerializeField]
    private Color _playerColor =
        new Color(0.25f, 0.65f, 1f);

    [SerializeField]
    private Color _enemyColor =
        new Color(1f, 0.35f, 0.3f);

    private readonly Dictionary<int, GameObject> _actors = new();
    private readonly Dictionary<int, int> _lastAttackSequences = new();
    private readonly Dictionary<int, int> _lastHitSequences = new();
    private readonly HashSet<int> _shownDeaths = new();

    private BattleModel _model;
    private UnitActorManager _unitActorManager;

    public void Initialize(UnitActorManager unitActorManager)
    {
        _unitActorManager = unitActorManager
            ?? throw new ArgumentNullException(nameof(unitActorManager));
    }

    public void ShowBattle(BattleModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        if (_gridRoot == null)
            throw new InvalidOperationException(
                "BattlePresenter 未绑定 GridRoot。");

        if (_unitActorManager == null)
            throw new InvalidOperationException(
                "BattlePresenter 尚未初始化。");

        Clear();
        _model = model;

        try
        {
            RenderFrame();
        }
        catch
        {
            Clear();
            throw;
        }
    }

    public void RenderFrame()
    {
        if(_model == null)
            return;
        
        foreach (var unit in _model.Units)
        {
            if(!_actors.TryGetValue(
                unit.BattleId, out var actor) ||
                actor == null)
            {
                //已死亡且没有表现对象，不再创建
                if (unit.IsDead)
                {
                    continue;
                }
                actor = CreateActor(unit);

                //首次创建先放到出生位置
                actor.transform.localPosition = unit.Position;
                _actors[unit.BattleId] = actor;
                _lastAttackSequences[unit.BattleId] = unit.AttackSequence;
                _lastHitSequences[unit.BattleId] = unit.HitSequence;
            }

            Vector3 previousPosition = actor.transform.localPosition;

            actor.transform.localPosition = unit.Position;


            var visual = actor.GetComponent<UnitVisual>();

            if (unit.IsDead)
            {
                if (_shownDeaths.Add(unit.BattleId))
                {
                    if (visual != null && visual.animator != null)
                    {
                        actor.SetActive(true);
                        visual.PlayDead();
                    }
                    else
                    {
                        // 无动画的胶囊直接隐藏。
                        actor.SetActive(false);
                    }
                }

                continue;
            }

            actor.SetActive(true);

            Vector3 movement = unit.Position - previousPosition;
            movement.y = 0f;

            bool isMoving = movement.sqrMagnitude > 0f;

            visual?.SetMoving(isMoving);

            int lastAttack = _lastAttackSequences[unit.BattleId];

            if (unit.AttackSequence != lastAttack)
            {
                _lastAttackSequences[unit.BattleId] = unit.AttackSequence;

                visual?.PlayAttack();
            }

            int lastHit = _lastHitSequences[unit.BattleId];

            if (unit.HitSequence != lastHit)
            {
                // 即使播放器抑制本次 Hit，也消费这个序号。
                _lastHitSequences[unit.BattleId] = unit.HitSequence;
                visual?.PlayHit();
            }
        }
    }

    private GameObject CreateActor(BattleUnit unit)
    {
        var unitActor = _unitActorManager.GetOrCreateBattle(
            unit.BattleId, unit.ConfigId, unit.Team);

        var actor = unitActor.gameObject;

        actor.name = $"Battle_{unit.Team}_{unit.BattleId}";
        actor.transform.SetParent(_gridRoot, false);
        actor.transform.localRotation = unit.Team == BattleTeam.Enemy
            ? Quaternion.Euler(0f, 180f, 0f)
            : Quaternion.identity;

        // 管理器已经创建完整模型，再禁用全部碰撞体。
        foreach (var collider in
            actor.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        var visual = unitActor.GetComponent<UnitVisual>();

        // 按当前 UnitVisual 的约定：
        // 没有 Animator 表示使用占位胶囊，只给胶囊设置阵营色。
        if (visual.animator == null)
        {
            Color color = unit.Team == BattleTeam.Player
                ? _playerColor
                : _enemyColor;

            var properties = new MaterialPropertyBlock();

            foreach (var renderer in
                actor.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.GetComponent<TMPro.TMP_Text>() != null) continue;
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_Color", color);
                properties.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(properties);
            }
        }

        return actor;
    }

    public bool TryGetUnitVisual(
        int battleId,
        out UnitVisual visual)
    {
        visual = null;

        if (!_actors.TryGetValue(battleId, out var actor)
            || actor == null)
        {
            return false;
        }

        visual = actor.GetComponent<UnitVisual>();

        return visual != null;
    }

    public void Clear()
    {
        _model = null;
        _actors.Clear();

        _unitActorManager?.ClearBattleActors();
        _lastAttackSequences.Clear();

        _lastHitSequences.Clear();
        _shownDeaths.Clear();
    }

    private void OnDestroy()
    {
        Clear();
    }
}