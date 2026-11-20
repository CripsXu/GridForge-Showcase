using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed class UIManager : IDisposable
{
    private readonly ResourceManager _resources;
    private readonly Transform _uiRoot;
    private readonly Transform _creationRoot;
    private readonly ModuleManager _modules;
    private readonly EventBus _events;
    private readonly Dictionary<Type, UIEntry> _views = new();
    private readonly Dictionary<Type, PendingOpen> _pending = new();
    private bool _disposed;

    private sealed class PendingOpen
    {
        public readonly string Address;
        public readonly TaskCompletionSource<UIView> Completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Cancelled;

        public PendingOpen(string address) => Address = address;
        public void Cancel()
        {
            Cancelled = true;
            Completion.TrySetCanceled();
        }
    }

    public UIManager(ResourceManager resources, Transform uiRoot,
        ModuleManager modules, EventBus events)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        if (uiRoot == null) throw new ArgumentNullException(nameof(uiRoot));
        _uiRoot = uiRoot;
        _modules = modules ?? throw new ArgumentNullException(nameof(modules));
        _events = events ?? throw new ArgumentNullException(nameof(events));

        // 在未激活的父节点下创建，避免 OnEnable 先于依赖初始化。
        var staging = new GameObject("UI Creation");
        staging.SetActive(false);
        _creationRoot = staging.transform;
        _creationRoot.SetParent(uiRoot, false);
    }

    public async Task<T> OpenAsync<T>(string address) where T : UIView
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("UI address is required.", nameof(address));
        var type = typeof(T);

        if (_views.TryGetValue(type, out var entry))
        {
            EnsureAddress(type, entry.Address, address);
            if (entry.View != null && !entry.View.IsDisposed)
            {
                entry.View.Open();
                return (T)entry.View;
            }
            _views.Remove(type);
            DestroyEntry(entry);
        }

        if (!_pending.TryGetValue(type, out var pending))
        {
            pending = new PendingOpen(address);
            _pending.Add(type, pending);
            // 在开始异步操作前登记，缓存命中导致同步完成时也安全。
            _ = CreateAsync<T>(pending);
        }
        else
        {
            EnsureAddress(type, pending.Address, address);
        }

        return (T)await pending.Completion.Task;
    }

    private async Task CreateAsync<T>(PendingOpen pending) where T : UIView
    {
        var type = typeof(T);
        GameObject instance = null;
        T view = null;
        bool acquired = false;
        try
        {
            var prefab = await _resources.LoadAsync<GameObject>(pending.Address);
            acquired = true;
            if (_disposed || pending.Cancelled) throw new OperationCanceledException();
            if (prefab == null || prefab.GetComponent<T>() == null)
                throw new InvalidOperationException(
                    $"Prefab '{pending.Address}' requires a {type.Name} component on its root.");

            instance = UnityEngine.Object.Instantiate(prefab, _creationRoot);
            instance.SetActive(false);
            view = instance.GetComponent<T>();
            view.Initialize(_modules, _events);
            instance.transform.SetParent(_uiRoot, false);
            if (_disposed || pending.Cancelled) throw new OperationCanceledException();

            _views.Add(type, new UIEntry(view, pending.Address));
            RemovePending(type, pending);
            view.Open();
            pending.Completion.TrySetResult(view);
            // 从此由 UIEntry 持有这次资源引用。
            acquired = false;
        }
        catch (Exception error)
        {
            if (view != null && _views.TryGetValue(type, out var entry) && entry.View == view)
                _views.Remove(type);
            try { if (view != null) view.Dispose(); }
            catch (Exception cleanupError) { Debug.LogException(cleanupError); }
            if (instance != null) UnityEngine.Object.Destroy(instance);
            if (acquired) _resources.Release(pending.Address);

            if (error is OperationCanceledException) pending.Completion.TrySetCanceled();
            else pending.Completion.TrySetException(error);
        }
        finally
        {
            RemovePending(type, pending);
        }
    }

    public T Get<T>() where T : UIView
    {
        ThrowIfDisposed();
        if (_views.TryGetValue(typeof(T), out var entry) &&
            entry.View != null && !entry.View.IsDisposed)
            return (T)entry.View;
        throw new InvalidOperationException($"UI {typeof(T).Name} has not been created.");
    }

    public void Close<T>() where T : UIView
    {
        if (_disposed) return;
        CancelPending(typeof(T));
        if (_views.TryGetValue(typeof(T), out var entry) && entry.View != null)
            entry.View.Close();
    }

    public void Destroy<T>() where T : UIView
    {
        if (_disposed) return;
        var type = typeof(T);
        CancelPending(type);
        if (!_views.TryGetValue(type, out var entry)) return;
        _views.Remove(type);
        DestroyEntry(entry);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var pending in _pending.Values) pending.Cancel();
        _pending.Clear();

        var entries = new List<UIEntry>(_views.Values);
        _views.Clear();
        foreach (var entry in entries)
        {
            try { DestroyEntry(entry); }
            catch (Exception error) { Debug.LogException(error); }
        }
        if (_creationRoot != null) UnityEngine.Object.Destroy(_creationRoot.gameObject);
    }

    private void DestroyEntry(UIEntry entry)
    {
        try
        {
            if (entry.View != null) entry.View.Dispose();
        }
        finally
        {
            if (entry.View != null) UnityEngine.Object.Destroy(entry.View.gameObject);
            _resources.Release(entry.Address);
        }
    }

    private void CancelPending(Type type)
    {
        if (!_pending.TryGetValue(type, out var pending)) return;
        _pending.Remove(type);
        pending.Cancel();
        // 底层加载完成后 CreateAsync 会释放它持有的资源引用。
    }

    private void RemovePending(Type type, PendingOpen pending)
    {
        if (_pending.TryGetValue(type, out var current) && ReferenceEquals(current, pending))
            _pending.Remove(type);
    }

    private static void EnsureAddress(Type type, string registered, string requested)
    {
        if (registered != requested)
            throw new InvalidOperationException(
                $"{type.Name} already uses '{registered}', not '{requested}'.");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UIManager));
    }
}

