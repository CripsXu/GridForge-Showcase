using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class MatchResources : IDisposable
{
    private readonly ResourceManager _resources;

    //配置ID到模型地址的映射
    private readonly Dictionary<int ,string> _unitAddresses = new();

    //模型地址 → 已加载prefab
    //同时记录本局持有了哪些资源引用
    private readonly Dictionary<string, GameObject> _prefabs = new();

    private bool _started;
    private bool _ready;
    private bool _disposed;

    public MatchResources(ResourceManager resources)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    }

    public async Task PreloadAsync(IEnumerable<UnitConfig> configs)
    {
        ThrowIfDisposed();

        if (_started)
            throw new InvalidOperationException("本局资源不能重复预加载。");

        if (configs == null)
            throw new ArgumentNullException(nameof(configs));

        _started = true;

        try
        {
            var addresses = new HashSet<string>();

            //先检查全部配置，再启动资源加载
            foreach (var config in configs)
            {
                if (config == null || config.id <= 0)
                    throw new InvalidOperationException("棋子配置或ID无效。");
                
                _unitAddresses.Add(config.id, config.prefabAddress);

                if (!string.IsNullOrWhiteSpace(config.prefabAddress))
                {
                    addresses.Add(config.prefabAddress);
                }
            }

            var tasks = new List<Task>();

            foreach (var address in addresses)
            {
                tasks.Add(LoadPrefabAsync(address));
            }

            await Task.WhenAll(tasks);

            ThrowIfDisposed();
            _ready = true;

        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private async Task LoadPrefabAsync(string address)
    {
        var prefab = await _resources.LoadAsync<GameObject>(address);

        // 成功返回后，本类才拥有需要归还的资源引用。
        if (_disposed || prefab == null)
        {
            _resources.Release(address);

            ThrowIfDisposed();

            throw new InvalidOperationException(
                $"角色Prefab无效：{address}");
        }

        // 每项成功立即登记，不能等全部成功后才登记。
        _prefabs.Add(address, prefab);
    }

    public bool TryGetUnitPrefab(int configId, out GameObject prefab)
    {
        ThrowIfDisposed();

        if (!_ready)
            throw new InvalidOperationException("本局资源尚未加载完成。");

        prefab = null;

        if (!_unitAddresses.TryGetValue(configId, out var address))
            throw new KeyNotFoundException(
                $"本局不包含棋子配置：{configId}");

        // 旧角色没有模型地址，允许继续显示胶囊。
        if (string.IsNullOrWhiteSpace(address))
            return false;

        if (!_prefabs.TryGetValue(address, out prefab) || prefab == null)
            throw new InvalidOperationException(
                $"本局角色资源不可用：{address}");

        return true;
    }


    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _ready = false;

        foreach (var address in _prefabs.Keys)
            _resources.Release(address);

        _prefabs.Clear();
        _unitAddresses.Clear();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MatchResources));
    }
}
