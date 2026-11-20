using System;

public sealed class ShopModule : IModule
{
    
    private readonly ConfigManager _configManager;

    private ShopModel _shopModel;
    private ShopController _shopController;
    private bool _disposed;
    private readonly PlayerModel _playerModel;
    private readonly BenchModel _benchModel;
    private readonly BoardModel _boardModel;

    private readonly EventBus _eventBus;

    public ShopController Controller => _shopController ??
    throw new InvalidOperationException("ShopController is not initialized.");

    public ShopModel Model => _shopModel ??
throw new InvalidOperationException("ShopModel is not initialized.");

    public ShopModule(
        ConfigManager configManager, 
        EventBus eventBus,
        PlayerModel playerModel,
        BenchModel benchModel,
        BoardModel boardModel)
    {
        _configManager = configManager ?? 
        throw new ArgumentNullException(nameof(configManager));

        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _playerModel = playerModel ?? throw new ArgumentNullException(nameof(playerModel));
        _benchModel = benchModel ?? throw new ArgumentNullException(nameof(benchModel));
        _boardModel = boardModel ?? throw new ArgumentNullException(nameof(boardModel));
    }

    public void Initialize()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ShopModule));
        }

        if(_shopController != null)
        {
            return;
        }

        var unitConfigs = _configManager.Get<UnitConfigTable>();
        var model = new ShopModel(unitConfigs);
        var controller = new ShopController(model, unitConfigs,
         _eventBus, _playerModel, _benchModel, _boardModel);

        try
        {
            model.Initialize();
            controller.Initialize();
        }
        catch
        {
            controller.Dispose();
            model.Dispose();
            throw;
        }

        _shopModel = model;
        _shopController = controller;
    }

    public EventBus GetEventBus()
    {
        return _eventBus;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _shopController?.Dispose();
        _shopController = null;

        _shopModel?.Dispose();
        _shopModel = null;

    }

}