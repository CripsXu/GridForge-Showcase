using System;

public sealed class BenchController : IController
{
    private readonly BenchModel _benchModel;
    private readonly UnitConfigTable _configs;
    private readonly EventBus _eventBus;
    private bool _disposed;

    public BenchController(BenchModel benchModel, UnitConfigTable configs, EventBus eventBus)
    {
        _benchModel = benchModel ?? throw new ArgumentNullException(nameof(benchModel));
        _configs = configs ?? throw new ArgumentNullException(nameof(configs));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _disposed = false;
    }

    public void Initialize() {}

    public BenchSlotVO GetSlotVO(int index)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BenchController));

        var unit = _benchModel.GetSlot(index);

        if (unit == null)
            return null;

        var config = _configs.Get(unit.ConfigId);

        return new BenchSlotVO(
            unit.InstanceId,
            config.name,
            unit.ConfigId);
    }

    public BenchPlacementResult TryMoveUnit(int instanceId, int toIndex)
    {
        if (!_benchModel.TryFindSlot(instanceId, out int fromIndex))
        {
            return BenchPlacementResult.UnitNotFound;
        }

        if (fromIndex == toIndex)
        {
            return BenchPlacementResult.NoChange;
        }

        _benchModel.MoveOrSwap(fromIndex, toIndex);

        PublishBencnChanged(fromIndex);
        PublishBencnChanged(toIndex);

        return BenchPlacementResult.Success;
    }

    private void PublishBencnChanged(int index)
    {
        try
        {
            _eventBus.Publish(GameEvents.BenchChanged, index);
        }
        catch (AggregateException error) 
        {
             UnityEngine.Debug.LogException(error); 
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }
}