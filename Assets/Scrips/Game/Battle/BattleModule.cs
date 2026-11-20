using System;

public sealed class BattleModule : IModule
{
    private readonly ConfigManager _configManager;

    private BattleModel _model;
    private BattleController _controller;
    private bool _disposed;

    public BattleModel Model => _model
        ?? throw new InvalidOperationException(
            "BattleModule 尚未初始化或已释放。");

    public BattleController Controller => _controller
        ?? throw new InvalidOperationException(
            "BattleModule 尚未初始化或已释放。");

    public BattleModule(ConfigManager configManager)
    {
        _configManager = configManager
            ?? throw new ArgumentNullException(
                nameof(configManager));
    }

    public void Initialize()
    {
        if (_disposed)
            throw new ObjectDisposedException(
                nameof(BattleModule));

        if (_controller != null)
            return;

        var unitConfigs =
            _configManager.Get<UnitConfigTable>();

        var model = new BattleModel();
        var controller = new BattleController(
            model,
            unitConfigs);

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

        // 全部初始化成功后，才对外提供对象。
        _model = model;
        _controller = controller;
    }

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