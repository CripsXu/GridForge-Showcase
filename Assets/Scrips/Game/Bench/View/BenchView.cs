// GridForge UI skeleton. User-owned; never overwritten by the generator.
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BenchView : global::ModuleView<global::BenchModule>
{
    private readonly List<BenchSlot> _slots = new();
    protected override void OnInitialize()
    {
        ValidateGeneratedBindings();
        InitSlots();
    }

    private void InitSlots()
    {
        //已经摆好的格子直接复用，包含未激活的
        _slots.AddRange(GetComponentsInChildren<BenchSlot>(true));

        if (_slots.Count == 0)
        {
            throw new InvalidOperationException(
                "备战席至少需要一个已绑定的 BenchSlot 模板。");
        }

        _slots.Sort((a, b) =>
            a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        var template = _slots[0];
        var parent = template.transform.parent;

        // 前提：所有格子都在同一个布局容器下。
        foreach (var slot in _slots)
        {
            if (slot.transform.parent != parent)
                throw new InvalidOperationException(
                    "BenchSlot 必须放在同一个容器下。");
        }
        
        while (_slots.Count < BenchModel.Capacity)
        {
            var slot = Instantiate(template, parent, false);

            slot.name = $"Slot{_slots.Count}";
            slot.transform.SetSiblingIndex(
                template.transform.GetSiblingIndex() + _slots.Count);

            _slots.Add(slot);
        }

        if (_slots.Count != BenchModel.Capacity)
            throw new InvalidOperationException("备战席应有七个格子。");
    }

    protected override void OnOpen()
    {
        Events.Subscribe(GameEvents.BenchChanged, OnBenchChanged);
        RenderSlots();
    }

    protected override void OnClose()
    {
        Events.UnSubscribe(GameEvents.BenchChanged, OnBenchChanged);
    }

    private void OnBenchChanged(int benchIndex)
    {
        RenderSlots();
    }

    private void RenderSlots()
    {
        int occupiedCount = 0;

        for (int i = 0; i < _slots.Count; i++)
        {
            var vo = Module.Controller.GetSlotVO(i);

            _slots[i].gameObject.SetActive(true);
            _slots[i].Render(vo);

            if (vo != null)
                occupiedCount++;
        }

        _tmpBenchTitle.text =
            $"BENCH {occupiedCount} / {BenchModel.Capacity}";
    }
}

