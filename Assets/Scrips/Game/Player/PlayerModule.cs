using System;

public sealed class PlayerModule : IModule
{
    private PlayerModel _model;
    private bool _disposed;

    public PlayerModel Model => _model
        ?? throw new InvalidOperationException(
            "PlayerModule 尚未初始化或已释放。");

    public void Initialize()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PlayerModule));

        if (_model != null)
            return;

        var model = new PlayerModel();
        model.Initialize();
        _model = model;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _model?.Dispose();
        _model = null;
    }

    public PlayerModel GetModel()
    {
        return Model;
    }
}