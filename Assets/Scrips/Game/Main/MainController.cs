using System;
using System.Threading.Tasks;

public sealed class MainController : IController
{
    private readonly MainModel _model;
    private readonly IGameSceneManager _gameSceneMgr;
    private readonly UIManager _uiManager;
    private readonly Action<int> _applyGold;
    private bool _disposed;

    internal MainController(MainModel model, Action<int> applyGold, IGameSceneManager gameSceneMgr, UIManager uiManager)
    {
        _gameSceneMgr = gameSceneMgr ?? throw new ArgumentNullException(nameof(gameSceneMgr));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _applyGold = applyGold ?? throw new ArgumentNullException(nameof(applyGold));
        _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
    }

    public int Gold
    {
        get { ThrowIfDisposed(); return _model.Gold; }
    }

    public void Initialize() { }
    public void Dispose() => _disposed = true;

    public void AddGold(int amount)
    {
        ThrowIfDisposed();
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        // 原型在本地计算结果；接服务器时改为请求并应用返回状态。
        _applyGold(checked(_model.Gold + amount));
    }

    public async Task EnterMatchAsync()
    {
        ThrowIfDisposed();
        await _gameSceneMgr.EnterMatchAsync();
    }


    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MainController));
    }
}

