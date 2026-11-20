using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

public class BenchPresenter : MonoBehaviour
{
    [SerializeField] private Transform _benchRoot;
    //槽位摆放位置
    private readonly Transform[] _anchors = new Transform[BenchModel.Capacity];
    // 槽位显示棋子
    private readonly UnitActor[] _actors = new UnitActor[BenchModel.Capacity];
    [SerializeField, Min(0.01f)] private float _slotHalfWidth = 0.95f;
    [SerializeField, Min(0.01f)] private float _slotHalfDepth = 0.75f;

    private BenchController _benchController;
    private EventBus _eventBus;
    private UnitActorManager _unitActorManager;
    private bool _initialized;

    public void Initialize(
        BenchController benchController,
        EventBus eventBus, UnitActorManager unitActorManager)
    {
        if (_initialized) return;
        if (_benchRoot == null)
            throw new InvalidOperationException("Bench root is not assigned.");

        _benchController = benchController ?? throw new ArgumentNullException(nameof(benchController));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _unitActorManager = unitActorManager ?? throw new ArgumentNullException(nameof(unitActorManager));

        InitializeAnchors();
        RenderAll();

        _eventBus.Subscribe(GameEvents.BenchChanged, RenderSlot);
        _initialized = true;
    }

    private void RenderSlot(int index)
    {
        if (index < 0 || index >= _actors.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        var vo = _benchController.GetSlotVO(index);
        var actor = _actors[index];


        // 数据为空，场景也应当为空。
        if (vo == null)
        {
            RemoveActor(index);
            return;
        }

        // 已经显示同一个实例，不重复创建。
        if (actor != null &&
            actor.InstanceId == vo.InstanceId &&
            actor.transform.parent == _anchors[index])
        {
            return;
        }

        RemoveActor(index);

        try
        {
            actor = _unitActorManager.GetOrCreate(vo.InstanceId, vo.ConfigId);

            if (actor)
            {
                actor.transform.SetParent(
                    _anchors[index], false);

                actor.transform.localPosition = Vector3.zero;
                actor.transform.localRotation = Quaternion.identity;
                actor.gameObject.SetActive(true);
                _actors[index] = actor;
                
            }
        }
        catch
        {
            RemoveActor(index);
            throw;
        }
    }

    private void RenderAll()
    {
        for (int i = 0; i < _actors.Length; i++)
        {
            RenderSlot(i);
        }
    }

    private void InitializeAnchors()
    {
        for (int i = 0; i < _actors.Length; i++)
        {
            string slotName = $"BenchSlot_{i}";
            var anchor = _benchRoot.Find(slotName);

            if (anchor == null)
                throw new InvalidOperationException(
                    $"BenchRoot 下找不到 {slotName}。");

            _anchors[i] = anchor;
        }
    }

    private void RemoveActor(int index)
    {
        _actors[index] = null;
    }

    public bool TryGetSlot(Vector3 worldPosition, out int index)
    {
        index = -1;

        if (!_initialized || _benchRoot == null)
            return false;

        Vector3 local = _benchRoot.InverseTransformPoint(worldPosition);
        float nearestDistance = float.PositiveInfinity;
        int nearestIndex = -1;
        for (int i = 0; i < _anchors.Length; i++)
        {
            Vector3 offset = local - _anchors[i].localPosition;
            float distance = offset.x * offset.x + offset.z * offset.z;
            if (distance >= nearestDistance)
            {
                continue;
            }
            nearestDistance = distance;
            nearestIndex = i;
        }

        if (nearestIndex < 0) return false;

        Vector3 difference = local - _anchors[nearestIndex].localPosition;
        if (Mathf.Abs(difference.x) > _slotHalfWidth ||
        Mathf.Abs(difference.z) > _slotHalfDepth)
        {
            return false;
        }

        index = nearestIndex;
        return true;
    }

    private void OnDestroy()
    {
        _eventBus?.UnSubscribe(GameEvents.BenchChanged, RenderSlot);

        for (int i = 0; i < _actors.Length; i++)
            RemoveActor(i);

        _initialized = false;
    }
}
