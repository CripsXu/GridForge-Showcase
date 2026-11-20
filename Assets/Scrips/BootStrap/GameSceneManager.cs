using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public interface IGameSceneManager : IDisposable
{
    Task EnterMatchAsync();
    Task ExitMatchAsync();
    Task ShutdownAsync();
}

public sealed class GameSceneManager : IGameSceneManager
{
    private const string MatchScenePath =
        "Assets/Scenes/MatchScene.unity";

    // 借用的应用级服务。
    private readonly ResourceManager _resources;
    private readonly ConfigManager _configs;
    private readonly UIManager _appUI;
    private readonly Scene _appScene;
    private readonly Camera _appCamera;

    // 串行执行进入、退出流程。
    private readonly SemaphoreSlim _gate = new(1, 1);

    // 当前这一局。
    private Scene _matchScene;
    private MatchSceneRoot _matchRoot;
    private MatchResources _matchResources;

    // 导航状态。
    private bool _ready;
    private bool _exitRequested;
    private bool _disposed;
    private Task _exitTask;
    private Task _shutdownTask;

    public GameSceneManager(
        ResourceManager resources,
        ConfigManager configs,
        UIManager appUI,
        Scene appScene)
    {
        _resources = resources
            ?? throw new ArgumentNullException(nameof(resources));

        _configs = configs
            ?? throw new ArgumentNullException(nameof(configs));

        _appUI = appUI
            ?? throw new ArgumentNullException(nameof(appUI));

        if (!appScene.IsValid() || !appScene.isLoaded)
        {
            throw new ArgumentException(
                "应用场景尚未加载。",
                nameof(appScene));
        }

        _appScene = appScene;
        _appCamera = FindAppCamera(appScene);
    }

    public async Task EnterMatchAsync()
    {
        ThrowIfClosing();

        await _gate.WaitAsync();

        try
        {
            ThrowIfClosing();

            if (_ready)
                return;

            if (_matchResources != null || _matchScene.IsValid())
            {
                throw new InvalidOperationException(
                    "上一局尚未清理，请先重试退出。");
            }

            if (SceneManager.GetSceneByPath(MatchScenePath).isLoaded)
            {
                throw new InvalidOperationException(
                    "请只从 AppScene 启动，由场景管理器加载 MatchScene。");
            }

            _matchResources = new MatchResources(_resources);

            try
            {
                await WaitForOperationAsync(
                    SceneManager.LoadSceneAsync(
                        MatchScenePath,
                        LoadSceneMode.Additive));

                _matchScene =
                    SceneManager.GetSceneByPath(MatchScenePath);

                if (!_matchScene.IsValid() || !_matchScene.isLoaded)
                {
                    throw new InvalidOperationException(
                        "MatchScene 加载未成功。");
                }

                ThrowIfClosing();

                if (!SceneManager.SetActiveScene(_matchScene))
                {
                    throw new InvalidOperationException(
                        "无法激活 MatchScene。");
                }

                _appCamera.enabled = false;
                _matchRoot = FindMatchRoot(_matchScene);

                await _matchRoot.InitializeAsync(
                    _resources,
                    _configs,
                    _matchResources);

                ThrowIfClosing();

                _appUI.Close<MainView>();
                _ready = true;
            }
            catch (Exception entryError)
            {
                try
                {
                    await ClearMatchAsync();
                }
                catch (Exception cleanupError)
                {
                    throw new AggregateException(
                        "进入本局失败，清理也未完成。",
                        entryError,
                        cleanupError);
                }

                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task ExitMatchAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(GameSceneManager));

        if (_exitTask != null && !_exitTask.IsCompleted)
            return _exitTask;

        _exitRequested = true;
        StopMatch();
        _exitTask = ExitMatchCoreAsync();
        return _exitTask;
    }

    public Task ShutdownAsync()
    {
        _disposed = true;
        _exitRequested = true;
        StopMatch();

        try
        {
            // 退局可能正在异步恢复主界面，关闭应用时取消这次打开。
            _appUI.Close<MainView>();
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }

        // 关闭失败时允许重试；仍在进行或已经成功的关闭共用结果。
        if (_shutdownTask == null ||
            _shutdownTask.IsFaulted || _shutdownTask.IsCanceled)
        {
            _shutdownTask = ShutdownCoreAsync();
        }

        return _shutdownTask;
    }

    public void Dispose()
    {
        // OnDestroy 不能等待Task：立即停业务，异步完成卸载并观察异常。
        _ = ObserveShutdownAsync(ShutdownAsync());
    }

    private async Task ExitMatchCoreAsync()
    {
        await _gate.WaitAsync();

        try
        {
            await ClearMatchAsync();

            if (!_disposed)
                await _appUI.OpenAsync<MainView>("ui/main");
        }
        finally
        {
            // 清理失败则保持禁止进入，保留句柄与资源供下一次退出重试。
            if (!_disposed &&
                _matchResources == null && !_matchScene.IsValid())
            {
                _exitRequested = false;
            }

            _gate.Release();
        }
    }

    private async Task ShutdownCoreAsync()
    {
        await _gate.WaitAsync();

        try
        {
            await ClearMatchAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task ObserveShutdownAsync(Task shutdown)
    {
        try
        {
            await shutdown;
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }
    }

    // 调用方已持有gate，此方法不能再次等待同一把锁。
    private async Task ClearMatchAsync()
    {
        StopMatch();

        if (_appScene.IsValid() && _appScene.isLoaded &&
            !SceneManager.SetActiveScene(_appScene))
        {
            throw new InvalidOperationException("无法恢复应用场景。");
        }

        if (_matchScene.IsValid() && _matchScene.isLoaded)
        {
            await WaitForOperationAsync(
                SceneManager.UnloadSceneAsync(_matchScene));

            if (_matchScene.IsValid() && _matchScene.isLoaded)
            {
                throw new InvalidOperationException(
                    "MatchScene 尚未卸载，保留本局资源以便重试退出。");
            }
        }

        // 对局相机已卸载，主界面重新获得背景相机。
        if (!_disposed && _appCamera != null)
            _appCamera.enabled = true;

        // 场景实例已经消失，才归还它们使用的资产引用。
        _matchResources?.Dispose();
        _matchResources = null;
        _matchRoot = null;
        _matchScene = default;
    }

    private void StopMatch()
    {
        _ready = false;

        try
        {
            if (_matchRoot != null)
                _matchRoot.Shutdown();
        }
        catch (Exception error)
        {
            // 停止异常不能阻止后续卸载；仍需记录实际清理错误。
            Debug.LogException(error);
        }
    }

    private static MatchSceneRoot FindMatchRoot(Scene matchScene)
    {
        MatchSceneRoot found = null;

        foreach (var rootObject in matchScene.GetRootGameObjects())
        {
            foreach (var candidate in rootObject.GetComponents<MatchSceneRoot>())
            {
                if (found != null)
                {
                    throw new InvalidOperationException(
                        "MatchScene 顶层只能有一个 MatchSceneRoot。");
                }

                found = candidate;
            }
        }

        if (found == null)
        {
            throw new InvalidOperationException(
                "MatchScene 顶层未找到 MatchSceneRoot。");
        }

        return found;
    }

    private static Camera FindAppCamera(Scene appScene)
    {
        Camera found = null;

        foreach (var rootObject in appScene.GetRootGameObjects())
        {
            var candidate = rootObject.GetComponent<Camera>();
            if (candidate == null || !rootObject.CompareTag("MainCamera"))
                continue;

            if (found != null)
                throw new InvalidOperationException("AppScene 只能有一台主界面相机。");

            found = candidate;
        }

        if (found == null)
            throw new InvalidOperationException("AppScene 顶层未找到主界面相机。");

        return found;
    }


    private void ThrowIfClosing()
    {
        if (_disposed || _exitRequested)
        {
            throw new OperationCanceledException(
                "应用已关闭或已请求退出本局。");
        }
    }

    private static Task WaitForOperationAsync(
        AsyncOperation operation)
    {
        if (operation == null)
        {
            throw new InvalidOperationException(
                "Unity 未创建场景加载或卸载操作。");
        }

        if (operation.isDone)
            return Task.CompletedTask;

        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        operation.completed += _ => completion.TrySetResult(true);

        return completion.Task;
    }

}
