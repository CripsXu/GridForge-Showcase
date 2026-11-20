using System;

public sealed class BoardModule : IModule
{
    private readonly BenchModel _benchModel;
    private readonly EventBus _eventBus;

    private BoardModel _model;
    private BoardController _controller;
    private bool _disposed;

    public BoardModel Model => _model
        ?? throw new InvalidOperationException(
            "BoardModule 尚未初始化或已释放。");

    public BoardController Controller => _controller
        ?? throw new InvalidOperationException(
            "BoardModule 尚未初始化或已释放。");

    public BoardModule(BenchModel benchModel, EventBus eventBus)
    {
        _benchModel = benchModel
            ?? throw new ArgumentNullException(nameof(benchModel));

        _eventBus = eventBus
            ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public void Initialize()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BoardModule));

        if (_controller != null)
            return;

        var model = new BoardModel();
        var controller = new BoardController(
            model,
            _benchModel,
            _eventBus);

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