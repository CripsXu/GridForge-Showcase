using System;
using System.Collections.Generic;

// 同步、主线程分发。新增监听从下一次 Publish 开始生效。
public sealed class EventBus
{
    private readonly Dictionary<object, List<Delegate>> _events = new();

    public void Subscribe<T>(EventKey<T> key, Action<T> callback)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (callback == null) throw new ArgumentNullException(nameof(callback));
        if (!_events.TryGetValue(key, out var listeners))
        {
            listeners = new List<Delegate>();
            _events.Add(key, listeners);
        }
        if (!listeners.Contains(callback)) listeners.Add(callback);
    }

    public void UnSubscribe<T>(EventKey<T> key, Action<T> callback)
    {
        if (!_events.TryGetValue(key, out var listeners)) return;
        listeners.Remove(callback);
        if (listeners.Count == 0) _events.Remove(key);
    }

    public void Publish<T>(EventKey<T> key, T data)
    {
        if (!_events.TryGetValue(key, out var listeners)) return;
        // 回调里关闭窗口、退订或注册事件，都不会破坏当前遍历。
        var snapshot = listeners.ToArray();
        List<Exception> errors = null;
        foreach (var listener in snapshot)
        {
            if (!listeners.Contains(listener)) continue;
            try { ((Action<T>)listener)(data); }
            catch (Exception error)
            {
                (errors ??= new List<Exception>()).Add(error);
            }
        }

        // 一个监听者出错仍继续分发，最后将错误交给调用者。
        if (errors != null)
            throw new AggregateException("Event listeners failed after state was applied.", errors);
    }
}

public sealed class EventKey<T>
{

}

