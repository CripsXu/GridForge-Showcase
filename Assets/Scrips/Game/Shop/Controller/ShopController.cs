using System;
using System.Collections.Generic;

public sealed class ShopController : IController
{
    private const int OfferCount = 5;

    private readonly ShopModel _model;
    private readonly UnitConfigTable _unitConfigs;
    private readonly Random _random = new Random();
    private readonly PlayerModel _playerModel;
    private readonly BenchModel _benchModel;
    private readonly BoardModel _boardModel;
    private readonly EventBus _eventBus;

    private bool _initialized;
    private bool _disposed;

    public int Gold
    {
        get
        {
            ThrowIfNotReady();
            return _playerModel.Gold;
        }
    }

    internal ShopController(
        ShopModel model, 
        UnitConfigTable unitConfigs, 
        EventBus eventBus,
        PlayerModel playerModel,
        BenchModel benchModel,
        BoardModel boardModel
        )
    {
        _model = model ?? throw new ArgumentNullException(nameof(model), "Model cannot be null.");
        _unitConfigs = unitConfigs ?? throw new ArgumentNullException(nameof(unitConfigs), "UnitConfigs cannot be null.");
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus), "EventBus cannot be null.");
        _playerModel = playerModel ?? throw new ArgumentNullException(nameof(playerModel), "PlayerModel cannot be null.");
        _benchModel = benchModel ?? throw new ArgumentNullException(nameof(benchModel), "BenchModel cannot be null.");
        _boardModel = boardModel ?? throw new ArgumentNullException(nameof(boardModel), "BenchModel cannot be null.");
    }

    public void Initialize()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ShopController), "The shop controller has been disposed.");

        }

        if (_initialized)
        {
            return;
        }

        GenerateOffers(featureModel: true);
        _initialized = true;
    }

    private void GenerateOffers(bool featureModel = false)
    {
        var candidates = new List<UnitConfig>(_unitConfigs.GetAll());

        if(candidates.Count == 0)
        {
            throw new InvalidOperationException("No unit candidates available for shop offers.");
        }

        var configIds = new int[OfferCount];

        for(int i = 0; i < OfferCount; i++)
        {
            int randomIndex = _random.Next(candidates.Count);
            configIds[i] = candidates[randomIndex].id;
        }

        // Put the configured animated unit in reach on the first visit.
        if (featureModel)
        {
            var featured = candidates.Find(unit => !string.IsNullOrWhiteSpace(unit.prefabAddress));
            if (featured != null) configIds[0] = featured.id;
        }
        _model.ReplaceOffers(configIds);
    }

    public int SlotCount
    {
        get
        {
            ThrowIfNotReady();
            return _model.SlotCount;
        }
    }

    private void ThrowIfNotReady()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ShopController), "The shop controller has been disposed.");
        }

        if (!_initialized)
        {
            throw new InvalidOperationException("The shop controller has not been initialized.");
        }
    }

    // 当前 UI 接入免费刷新，付费规则随购买闭环实现。
    public void RefreshOffers()
    {
        ThrowIfNotReady();
        GenerateOffers();
    }

    public ShopOfferVO GetOfferVo(int index)
    {
        ThrowIfNotReady();
        var slot = _model.GetSlot(index);

        var config = _unitConfigs.Get(slot.ConfigId);

        return new ShopOfferVO(
            config.id,
            config.name,
            config.cost,
            slot.IsPurchased,
            config.portraitAssetId,
            config.rarityFrameAssetId,
            string.Join(" · ", config.traits ?? Array.Empty<string>())
        );
    }

    public void Dispose()
    {
        _disposed = true;
    }

    public ShopPurchaseResult BuyOffer(int index)
    {
        ThrowIfNotReady();

        var slot = _model.GetSlot(index);

        if(slot.IsPurchased)
        {
            return ShopPurchaseResult.AlreadyPurchased;
        }

        var config = _unitConfigs.Get(slot.ConfigId);

        if(_playerModel.Gold < config.cost)
            return ShopPurchaseResult.NotEnoughGold;

        if (!_benchModel.TryGetEmptySlot(out int benchIndex))
            return ShopPurchaseResult.BenchFull;

        //创建可能失败的对象放在扣款前
        var unit = _playerModel.CreateUnit(config.id);

        if(!_playerModel.TrySpendGold(config.cost))
            return ShopPurchaseResult.NotEnoughGold;

        _benchModel.Place(benchIndex, unit);
        _model.MarkPurchased(index);

        try
        {
            _eventBus.Publish(GameEvents.ShopOfferBought, index);
        }
        catch(AggregateException error)
        {
            // 当前 EventBus 会汇总监听异常。
            // 此时购买已经完成，不能当成购买失败重试。
            UnityEngine.Debug.LogException(error);
        }

        try
        {
            _eventBus.Publish(GameEvents.BenchChanged, benchIndex);
        }
        catch (AggregateException error)
        {
            UnityEngine.Debug.LogException(error);
        }

        return ShopPurchaseResult.Success;
    }

    public UnitSaleResult TrySellUnit(int instanceId)
    {
        ThrowIfNotReady();
        if (instanceId <= 0)
            return UnitSaleResult.UnitNotFound;

        bool inBench = _benchModel.TryFindSlot(instanceId, out int benchIndex);
        bool inBoard = _boardModel.TryFindCell(instanceId, out int col, out int row);

        if (!inBench && !inBoard)
            return UnitSaleResult.UnitNotFound;
        if (inBench && inBoard)
            return UnitSaleResult.InvalidState;
        if (inBoard && !_boardModel.IsPlayerCell(col, row))
            return UnitSaleResult.InvalidState;

        var unit = inBench ? _benchModel.GetSlot(benchIndex) : _boardModel.GetCell(col, row);

        if (!_unitConfigs.TryGet(unit.ConfigId, out var config) ||
        config == null || config.cost < 0)
        {
            return UnitSaleResult.InvalidConfig;
        }

        int refund = config.cost;

        try
        {
            checked
            {
                _ = _playerModel.Gold + refund;
            }
        }
        catch (OverflowException)
        {
            
            return UnitSaleResult.GoldOverflow;
        }

        //先验证金额再删除
        if (inBench)
        {
            _benchModel.Remove(benchIndex);
        }
        else
        {
          _boardModel.Remove(col, row);  
        }
        _playerModel.AddGold(refund);

        if (inBench)
        {
            PublishSafely(GameEvents.BenchChanged, benchIndex);
        }
        else
        {
            PublishSafely(GameEvents.BoardChanged, new BoardCellChanged(col, row));
        }
        PublishSafely(GameEvents.GoldChanged, _playerModel.Gold);
        PublishSafely(GameEvents.UnitSold, instanceId);
        return UnitSaleResult.Success;
    }

    private void PublishSafely<T>(EventKey<T> key, T value)
    {
        try { _eventBus.Publish(key, value); }
        catch (AggregateException error) { UnityEngine.Debug.LogException(error); }
    }
}
