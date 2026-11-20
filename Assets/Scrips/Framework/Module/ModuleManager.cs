using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ModuleManager
{
    private readonly Dictionary<Type, Func<IModule>> _factories = new();
    private readonly Dictionary<Type, IModule> _modules = new();
    private readonly List<IModule> _order = new();
    private readonly HashSet<Type> _creating = new();
    private bool _disposed;

    public void Register<T>(Func<T> factory) where T : class, IModule
    {
        ThrowIfDisposed();
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        if (_factories.ContainsKey(typeof(T)))
            throw new InvalidOperationException($"{typeof(T).Name} is already registered.");
        _factories.Add(typeof(T), () => factory());
    }

    // 初始化成功后才发布实例，View 只能获取已就绪模块。
    public T GetOrCreate<T>() where T : class, IModule
    {
        ThrowIfDisposed();
        var type = typeof(T);
        if (_modules.TryGetValue(type, out var existing)) return (T)existing;
        if (!_factories.TryGetValue(type, out var factory))
            throw new InvalidOperationException($"{type.Name} is not registered.");
        if (!_creating.Add(type))
            throw new InvalidOperationException($"Circular module dependency: {type.Name}.");
        IModule module = null;
        try
        {
            module = factory() ?? throw new InvalidOperationException($"{type.Name} factory returned null.");
            module.Initialize();
            _modules.Add(type, module);
            _order.Add(module);
            return (T)module;
        }
        catch
        {
            try { module?.Dispose(); }
            catch (Exception error) { Debug.LogException(error); }
            throw;
        }
        finally { _creating.Remove(type); }
    }

    public T GetModule<T>() where T : class, IModule
    {
        ThrowIfDisposed();
        return _modules.TryGetValue(typeof(T), out var module) ? (T)module : null;
    }

    public void DisposeAllModules()
    {
        if (_disposed) return;
        _disposed = true;
        for (int i = _order.Count - 1; i >= 0; i--)
        {
            try { _order[i].Dispose(); }
            catch (Exception error) { Debug.LogException(error); }
        }
        _order.Clear();
        _modules.Clear();
        _factories.Clear();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ModuleManager));
    }
}
