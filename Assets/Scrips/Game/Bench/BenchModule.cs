using System;

public sealed class BenchModule : IModule
{
    private readonly ConfigManager _configs;
    private BenchModel _model;
    private BenchController _controller;
    private readonly EventBus _eventBus;
    private bool _disposed;

    public BenchModel Model => _model
        ?? throw new InvalidOperationException(
            "BenchModule 尚未初始化或已释放。");

    public BenchController Controller => _controller
        ?? throw new InvalidOperationException(
            "BenchModule 尚未初始化或已释放。");

    public BenchModule(ConfigManager configs, EventBus eventBus)
    {
        _configs = configs
            ?? throw new ArgumentNullException(nameof(configs));

        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public void Initialize()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BenchModule));

        if (_controller != null)
            return;

        var unitConfigs = _configs.Get<UnitConfigTable>();
        var model = new BenchModel();
        var controller = new BenchController(model, unitConfigs, _eventBus);

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

        _model = model;
        _controller = controller;
    }

    public BenchModel GetModel() => Model;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _controller?.Dispose();
        }
        finally
        {
            _controller = null;
            _model?.Dispose();
            _model = null;
        }
    }
}