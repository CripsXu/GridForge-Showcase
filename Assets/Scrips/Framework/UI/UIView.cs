using System;
using UnityEngine;

public abstract class UIView : MonoBehaviour
{
    public bool IsInitialized { get; private set; }
    public bool IsOpen { get; private set; }
    public bool IsDisposed { get; private set; }
    protected EventBus Events { get; private set; }

    // 框架统一连接依赖，业务 View 无需持有整个 GameContext。
    public void Initialize(ModuleManager modules, EventBus events)
    {
        ThrowIfDisposed();
        if (IsInitialized) return;
        if (modules == null) throw new ArgumentNullException(nameof(modules));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        ResolveModule(modules);
        OnInitialize();
        IsInitialized = true;
    }

    public void Open()
    {
        ThrowIfDisposed();
        if (!IsInitialized)
            throw new InvalidOperationException($"{GetType().Name} must be initialized before opening.");
        if (IsOpen) return;
        IsOpen = true;
        try
        {
            OnOpen();
            if (IsOpen && !IsDisposed) gameObject.SetActive(true);
        }
        catch
        {
            Close();
            throw;
        }
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        try { OnClose(); }
        finally { gameObject.SetActive(false); }
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        try { Close(); }
        finally { OnDispose(); }
    }

    // 场景销毁或外部 Destroy 也必须退订。
    protected virtual void OnDestroy() => Dispose();

    private void ThrowIfDisposed()
    {
        if (IsDisposed) throw new ObjectDisposedException(GetType().Name);
    }

    protected virtual void ResolveModule(ModuleManager modules) { }
    protected virtual void OnInitialize() { }
    protected virtual void OnOpen() { }
    protected virtual void OnClose() { }
    protected virtual void OnDispose() { }
}

public abstract class ModuleView<TModule> : UIView where TModule : class, IModule
{
    protected TModule Module { get; private set; }

    protected sealed override void ResolveModule(ModuleManager modules)
    {
        Module = modules.GetModule<TModule>()
            ?? throw new InvalidOperationException(
                $"Register and initialize {typeof(TModule).Name} before opening {GetType().Name}.");
    }
}

