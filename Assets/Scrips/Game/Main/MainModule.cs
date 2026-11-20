using System;

public sealed class MainModule : IModule
{
    private readonly EventBus _events;
    private readonly IGameSceneManager _gameSceneMgr;
    private readonly UIManager _uiManager;
    private MainModel _model;
    private MainController _controller;

    public MainController Controller => _controller
        ?? throw new InvalidOperationException("MainModule has not been initialized or has been disposed.");

    public MainModule(EventBus events, IGameSceneManager gameSceneMgr, UIManager uiManager)
    {
        _gameSceneMgr = gameSceneMgr ?? throw new ArgumentNullException(nameof(gameSceneMgr));
        _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public void Initialize()
    {
        if (_controller != null) return;
        _model = new MainModel();
        _model.Initialize();
        _controller = new MainController(_model, ApplyGold, _gameSceneMgr, _uiManager);
        _controller.Initialize();
    }

    // 状态应用入口：先更新全部相关数据，再向外通知。
    private void ApplyGold(int gold)
    {
        if (_model == null) throw new ObjectDisposedException(nameof(MainModule));
        if (_model.ApplyGold(gold))
            _events.Publish(GameEvents.GoldChanged, _model.Gold);
    }


    public void Dispose()
    {
        _controller?.Dispose();
        _controller = null;
        _model?.Dispose();
        _model = null;
    } 
}

