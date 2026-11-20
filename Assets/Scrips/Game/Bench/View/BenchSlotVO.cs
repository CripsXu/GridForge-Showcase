public sealed class BenchSlotVO
{
    public int InstanceId { get; }
    public string Name { get; }
    public int ConfigId {get;}

    public BenchSlotVO(int instanceId, string name, int configId)
    {
        InstanceId = instanceId;
        Name = name;
        ConfigId = configId;
    }
}