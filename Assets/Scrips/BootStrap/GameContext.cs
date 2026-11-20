using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameContext : IDisposable
{
    public EventBus EventBus { get; } = new();
    public ResourceManager ResourceManager { get; } = new();
    public ModuleManager ModuleManager { get; } = new();
    public ConfigManager ConfigManager { get; }
    public IGameSceneManager gameSceneMgr { get; private set; }
    public UIManager UIManager { get; private set; }
    private bool _disposed;

    public GameContext()
    {
        ConfigManager = new ConfigManager(ResourceManager);
    }

    public void InitializeApplication(Transform uiRoot)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(GameContext));
        if (UIManager != null)
            throw new InvalidOperationException("应用已初始化。");
        if (uiRoot == null)
            throw new ArgumentNullException(nameof(uiRoot));

        var ui = new UIManager(ResourceManager, uiRoot, ModuleManager, EventBus);

        try
        {
            gameSceneMgr = new GameSceneManager(ResourceManager, ConfigManager, ui, uiRoot.gameObject.scene);
            UIManager = ui;
        }
        catch
        {
            ui.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try { gameSceneMgr?.Dispose(); }
        finally
        {
            try { UIManager?.Dispose(); }
            finally { ModuleManager.DisposeAllModules(); }
        }
    }


}

