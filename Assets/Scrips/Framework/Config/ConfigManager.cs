using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

public sealed class ConfigManager
{
    private readonly ResourceManager _resources;

    //根据具体config类型获取对象
    private readonly Dictionary<Type, IConfig> _configs = new();

    //明确记录注册顺序，用于初始化和逆序释放
    private readonly List<IConfig> _orderConfigs = new();

    private bool _initializing;
    private bool _initialized;
    private bool _disposed;

    public ConfigManager(ResourceManager resources)
    {
        _resources = resources ?? throw new ArgumentException(nameof(resources));
    }

    public void Regsiter<T>(T config) where T : class, IConfig
    {
        //检查状态，空参数和重复注册
        //同时加入字典和有序列表
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ConfigManager));
        }

        if(_initializing || _initialized)
        {
            throw new InvalidOperationException("配置初始化开始后，不能再注册新的配置。");
        }

        if(config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var type = typeof(T);
        if (_configs.ContainsKey(type))
        {
            throw new InvalidOperationException($"配置 {type.FullName} 已注册。");
        }

        _configs.Add(type, config);
        _orderConfigs.Add(config);
    }

    public async Task InitializeAsync()
    {
        // 按注册顺序，等待每个 Config 初始化完成。
        // 全部成功后标记为可用；失败时清理并向上抛异常。
        //1、检查是否允许初始化
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ConfigManager));   
        }

        if(_initialized) return;

        if (_initializing)
        {
            throw new InvalidOperationException("配置正在初始化中，不能重复初始化。");
        }

        //2、标记初始化开始
        _initializing = true;
        try
        {
            //3、按注册顺序，逐个等待初始化完成
            foreach (var config in _orderConfigs)
            {
                await config.InitializeAsync(_resources);
            }
            _initialized = true;

        }
        catch
        {
            _initializing = false;
            Dispose();

            //把原来的初始化异常抛给调用方
            throw;
        }
        finally
        {
            //成功、失败都会执行
            _initializing = false;
        }
    }

    public T Get<T>()
        where T : class, IConfig
    {
        // 检查管理器已完成初始化且未释放。
        // 根据 typeof(T) 获取对应对象。
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ConfigManager));
        }

        if (!_initialized)
        {
            throw new InvalidOperationException(
                "配置尚未全部初始化完成，不能获取。");
        }

        var type = typeof(T);
        if (!_configs.TryGetValue(type, out var config))
        {
            throw new KeyNotFoundException($"配置 {type.FullName} 未注册。");
        }

        return (T)config;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_initializing)
        {
            throw new InvalidOperationException(
                "请等待配置初始化结束后再释放。");
        }

        _disposed = true;
        _initialized = false;

        // 按注册顺序的逆序释放 Config，再清空容器。
        for(int i = _orderConfigs.Count - 1; i >= 0; i--)
        {
            try
            {
                _orderConfigs[i].Dispose();
            }
            catch(Exception ex)
            {
                //某张表清理失败，记录错误，继续清理
                Debug.LogException(ex);
            }
        }

        _orderConfigs.Clear();
        _configs.Clear();
    }
}