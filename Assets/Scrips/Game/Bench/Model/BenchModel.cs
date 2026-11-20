using System;

public sealed class BenchModel : IModel
{
    public const int Capacity = 7;

    private readonly UnitInstance[] _slots = new UnitInstance[Capacity];

    public void Initialize() {}

    public UnitInstance GetSlot(int index)
    {
        VaildateIndex(index);
        return _slots[index];
    }

    private void VaildateIndex(int index)
    {
        if (index < 0 || index >= Capacity)
            throw new ArgumentOutOfRangeException(nameof(index));
    }

    public bool TryGetEmptySlot(out int index)
    {
        for(int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    internal void Place(int index, UnitInstance unit)
    {
        VaildateIndex(index);

        if(unit == null)
            throw new ArgumentNullException(nameof(unit));

        if(_slots[index] != null)
            throw new InvalidOperationException("Slot is already occupied.");

        foreach (var existing in _slots)
        {
            if(existing != null && existing.InstanceId == unit.InstanceId)
            throw new InvalidOperationException("Unit is already placed in another slot.");
        }

        _slots[index] = unit;
    }

    public bool TryFindSlot(int instanceId, out int index)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            var unit = _slots[i];

            if(unit != null && unit.InstanceId == instanceId)
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    internal UnitInstance Remove(int index)
    {
        VaildateIndex(index);

        var unit = _slots[index];

        if (unit == null)
            throw new InvalidOperationException("备战槽位没有棋子。");

        _slots[index] = null;
        return unit;
    }

    internal void MoveOrSwap(int fromIndex, int toIndex)
    {
        VaildateIndex(fromIndex);
        VaildateIndex(toIndex);

        var source = _slots[fromIndex];

        if (source == null)
        {
            throw new InvalidOperationException("来源槽位没有棋子。");
        }

        if (fromIndex == toIndex)
        {
            return;
        }

        _slots[fromIndex] =  _slots[toIndex];
        _slots[toIndex] = source;
    }

    public void Dispose()
    {
        Array.Clear(_slots, 0, _slots.Length);
    }
}