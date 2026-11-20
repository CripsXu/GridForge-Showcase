using System;
using System.Threading.Tasks;
using UnityEngine;

public class GameBootStrap : MonoBehaviour
{
    [SerializeField] private Transform _uiRoot;
    private GameContext _context;
    private bool _destroyed;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    private async void Start()
    {
        try { await StartGameAsync(); }
        catch (OperationCanceledException) when (_destroyed) { }
        catch (Exception error) { Debug.LogException(error); }
    }

    private async Task StartGameAsync()
    {
        _context = new GameContext();
        Debug.Log("Game Start");

        await _context.ResourceManager.InitializeAsync();
        if (_destroyed) return;

        // 注册并加载配置。
        _context.ConfigManager.Regsiter(new UnitConfigTable());
        await _context.ConfigManager.InitializeAsync();
        if (_destroyed) return;

        _context.InitializeApplication(_uiRoot);

        ModuleRegistry.RegisterApplication(
            _context.ModuleManager,
            _context.EventBus,
            _context.gameSceneMgr,
            _context.UIManager);

        _context.ModuleManager.GetOrCreate<MainModule>();

#if UNITY_EDITOR
        // // 开发检查统一在 DemoChecks 中选择，正式构建不调用。
        // await DemoChecks.RunAsync(_context);
        // if (_destroyed) return;
#endif

        await _context.UIManager.OpenAsync<MainView>("ui/main");

        if (_destroyed)
            return;
        Debug.Log("Game Start Complete");
    }

    private void OnDestroy()
    {
        _destroyed = true;
        _context?.Dispose();
    }
}
