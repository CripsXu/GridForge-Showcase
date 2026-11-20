using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class ResourceManager
{
    private readonly Dictionary<string, ResourceEntry> _entries = new();

    public async Task InitializeAsync()
    {
        await Addressables.InitializeAsync().Task;
    }

    public async Task<T> LoadAsync<T>(string address, CancellationToken cancellationToken = default)
        where T : UnityEngine.Object
    {

        cancellationToken.ThrowIfCancellationRequested();
        // 已经存在这个资源的记录
        if (_entries.TryGetValue(address, out var entry))
        {
            entry.RefCount++;

            Debug.Log(
                $"[ResourceManager] Acquire {address}, RefCount = {entry.RefCount}"
            );

            try
            {
                // 资源还没加载完成
                if (entry.State == ResourceState.Loading)
                {
                    Debug.Log(
                        $"[ResourceManager] Join loading task: {address}"
                    );

                    // 多个请求等待同一个 Task
                    await AwaitWithCancellation(
                        entry.LoadTask,
                        cancellationToken
                    );
                }
                else if (entry.State == ResourceState.Loaded)
                {
                    Debug.Log(
                        $"[ResourceManager] Cache hit: {address}"
                    );
                }

                // LoadTask 如果失败，上面的 await 会直接把异常继续抛出去
                return entry.Handle.Result as T;
            }
            catch (OperationCanceledException)
            {
                Release(address);
                throw;
            }
        }

        Debug.Log($"[ResourceManager] Loading: {address}");

        AsyncOperationHandle<T> handle;

        try
        {
            // 这里本身就可能因为 InvalidKey 直接抛异常
            handle = Addressables.LoadAssetAsync<T>(address);
        }
        catch
        {
            Debug.LogError(
                $"[ResourceManager] Failed to create load operation: {address}"
            );

            throw;
        }

        // 立刻创建 Entry
        // 重点：不能等加载完成以后才放进 Dictionary
        entry = new ResourceEntry(handle);

        _entries.Add(address, entry);

        // 创建一份“真正负责完成加载状态管理”的 Task
        entry.LoadTask = CompleteLoadAsync(address, entry, handle);

        try
        {
            await AwaitWithCancellation(
                entry.LoadTask, 
                cancellationToken
                );

            return handle.Result;
        }
        catch (OperationCanceledException)
        {
            Release(address);
            throw;
        }
    }

    private async Task CompleteLoadAsync<T>(
        string address,
        ResourceEntry entry,
        AsyncOperationHandle<T> handle)
        where T : UnityEngine.Object
    {
        try
        {


            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                throw new Exception(
                    $"Load resource failed: {address}"
                );
            }

            entry.State = ResourceState.Loaded;

            if(entry.ReleaseWhenLoaded && entry.RefCount == 0)
            {
                Addressables.Release(entry.Handle);
                _entries.Remove(address);

                Debug.Log($"[ResourceManager] Released and removed from cache: {address}");

                return;
            }

            Debug.Log(
                $"[ResourceManager] Loaded and cached: {address}"
            );
        }
        catch
        {
            entry.State = ResourceState.Failed;

            // 失败的 Entry 不可以继续留在缓存里
            _entries.Remove(address);

            // 如果已经创建出了有效 Handle，要把引用还给 Addressables
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            Debug.LogError(
                $"[ResourceManager] Load failed: {address}"
            );

            // 继续把异常传给所有 await 这个 LoadTask 的调用者
            throw;
        }
    }

    public void Release(string address)
    {
        if (!_entries.TryGetValue(address, out var entry))
        {
            return;
        }

        entry.RefCount--;

        Debug.Log(
            $"[ResourceManager] Release {address}, RefCount = {entry.RefCount}"
        );

        if (entry.RefCount > 0)
        {
            return;
        }

        if(entry.State == ResourceState.Loading)
        {
            entry.ReleaseWhenLoaded = true;

            Debug.Log($"[ResourceManager] Mark release when loaded: {address}");

            return;
        }

        Addressables.Release(entry.Handle);

        _entries.Remove(address);

        Debug.Log(
            $"[ResourceManager] Released and removed from cache: {address}"
        );
    }

    private static async Task AwaitWithCancellation(
        Task task,
        CancellationToken cancellationToken
    )
    {
        if (!cancellationToken.CanBeCanceled)
        {
            await task;
            return;
        }

        var cancelTaskSource = new TaskCompletionSource<bool>();

        using(cancellationToken.Register(
            () => cancelTaskSource.TrySetCanceled()))
        {
            var completedTask = await Task.WhenAny(task, cancelTaskSource.Task);
            if(completedTask == cancelTaskSource.Task)
            {
                await cancelTaskSource.Task;
            }

            await task;
        }
    }
}