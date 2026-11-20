using System;

public sealed class ShopModel : IModel
{
    private ShopSlot[] _slots = Array.Empty<ShopSlot>();

    private readonly UnitConfigTable _unitConfigs;

    public ShopModel(UnitConfigTable unitConfigs)
    {
        _unitConfigs = unitConfigs ?? throw new ArgumentNullException(nameof(unitConfigs));
    }

    public int SlotCount => _slots.Length;

    public void Initialize()
    {
    }

    public ShopSlot GetSlot(int index)
    {
        if(index < 0 || index >= _slots.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        }
        return _slots[index];
    }

    internal void ReplaceOffers(int[] configIds)
    {
        if(configIds == null)
        {
            throw new ArgumentNullException(nameof(configIds), "ConfigIds cannot be null.");
        }

        if(configIds.Length == 0)
        {
            throw new ArgumentException("ConfigIds cannot be empty.", nameof(configIds));
        }

        var newSlots = new ShopSlot[configIds.Length];

        for(int i = 0; i < configIds.Length; i++)
        {
            // 确认配置存在；不存在就抛异常。
            _unitConfigs.Get(configIds[i]);

            newSlots[i] = new ShopSlot(configIds[i]);

        }

        _slots = newSlots;
    }

    internal void MarkPurchased(int index)
    {
        GetSlot(index).MarkPurchased();
    }


    public void Dispose()
    {
        _slots = Array.Empty<ShopSlot>();
    }

    internal bool BuyOffer(int i)
    {
        if (i < 0 || i >= _slots.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(i));
        }

        var slot = _slots[i];

        if (slot.IsPurchased)
            return false;

        slot.MarkPurchased();

        return true;
    }
}