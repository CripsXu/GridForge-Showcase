using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopSlot
{

    public int ConfigId {get;}

    public bool IsPurchased {get; private set;}

    public ShopSlot(int configId)
    {
        if(configId <= 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(configId), "ConfigId must be greater than 0.");

        }
        ConfigId = configId;
    }

    internal void MarkPurchased()
    {
        if(IsPurchased)
        {
            throw new InvalidOperationException("该商品已经售出。");
        }
        IsPurchased = true;
    }

}
