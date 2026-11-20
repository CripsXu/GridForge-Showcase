using System.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class ResourceEntry
{
    public AsyncOperationHandle Handle { get; }

    // 所有等待同一资源的请求，共用这个 Task
    public Task LoadTask { get; set; }

    public int RefCount { get; set; }

    public ResourceState State { get; set; }

    public bool ReleaseWhenLoaded { get; set; }

    public ResourceEntry(AsyncOperationHandle handle)
    {
        Handle = handle;
        RefCount = 1;
        State = ResourceState.Loading;
    }
}