using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed class UnitConfigTable : IConfig
{
    private const string Address = "config/units";

    private Dictionary<int, UnitConfig> _units = new();

    private bool _initialized;
    private bool _disposed;

    public async Task InitializeAsync(ResourceManager resources)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UnitConfigTable));

        if (_initialized)
            return;

        if (resources == null)
            throw new ArgumentNullException(nameof(resources));

        //1.获得配置文件资源
        var asset = await resources.LoadAsync<TextAsset>(Address);

        //成功获得资源引用后，才进入需要归还资源的范围
        try
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitConfigTable));

            if (asset == null)
                throw new InvalidOperationException(
                    $"配置资源不是有效的 TextAsset：{Address}");

            //2、JSON文本解析成C#对象
            var file = JsonUtility.FromJson<UnitConfigFile>(asset.text);

            if(file == null || file.units == null || file.units.Length == 0)
                throw new InvalidOperationException(
                    $"配置资源内容无效：{Address}");

            //3、先建立临时字典，不直接修改正式数据
            var loadedUnits = new Dictionary<int, UnitConfig>();

            for(int i = 0; i < file.units.Length; i++)
            {
                var unit = file.units[i];


                if (unit == null || unit.id <= 0)
                    throw new InvalidOperationException(
                        $"{Address} 第 {i + 1} 条记录为空或 ID 无效。");

                if (loadedUnits.ContainsKey(unit.id))
                    throw new InvalidOperationException(
                        $"{Address} 存在重复 ID：{unit.id}");

                if (string.IsNullOrWhiteSpace(unit.name)
                    || unit.cost < 0
                    || unit.maxHp <= 0
                    || unit.attack < 0
                    || unit.attackIntervalSeconds <= 0
                    || float.IsNaN(unit.attackIntervalSeconds)
                    || float.IsInfinity(unit.attackIntervalSeconds)
                    || unit.attackRangeCells <= 0)
                {
                    throw new InvalidOperationException(
                        $"{Address} 中 ID={unit.id} 的名称或数值无效。");
                }

                loadedUnits.Add(unit.id, unit);
            }

            // 4. 全部成功，才让正式数据可用。
            _units = loadedUnits;
            _initialized = true;
        }
        finally
        {
            // 5. 解析成功或失败，都归还本次文件资源引用。
            resources.Release(Address);
        }
    }

    public UnitConfig Get(int id)
    {
        if (TryGet(id, out var config))
            return config;

        throw new KeyNotFoundException($"找不到棋子配置：{id}");
    }

    public bool TryGet(int id, out UnitConfig config)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UnitConfigTable));

        if (!_initialized)
            throw new InvalidOperationException("棋子配置尚未初始化完成。");

        return _units.TryGetValue(id, out config);
    }

    public IReadOnlyCollection<UnitConfig> GetAll()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UnitConfigTable));

        if (!_initialized)
            throw new InvalidOperationException("棋子配置尚未初始化完成。");

        return _units.Values;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _initialized = false;
        _units.Clear();
    }

}