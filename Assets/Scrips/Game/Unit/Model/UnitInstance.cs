using System;

public sealed class UnitInstance
{
    public int InstanceId { get; }
    public int ConfigId { get; }

    public UnitInstance(int instanceId, int configId)
    {
        if (instanceId <= 0)
            throw new ArgumentOutOfRangeException(nameof(instanceId));

        if (configId <= 0)
            throw new ArgumentOutOfRangeException(nameof(configId));

        InstanceId = instanceId;
        ConfigId = configId;
    }
}