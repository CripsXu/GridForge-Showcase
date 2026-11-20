public sealed class PlayerModel : IModel
{
    public int Gold { get; private set; }
    private int _nextUnitId = 1;

    public void Initialize()
    {
        Gold = 10; // 暂定本局初始金币
    }

    internal bool TrySpendGold(int amount)
    {
        if (amount < 0)
            throw new System.ArgumentOutOfRangeException(nameof(amount));

        if (Gold < amount)
            return false;

        Gold -= amount;
        return true;
    }

    internal UnitInstance CreateUnit(int configId)
    {
        var unit  = new UnitInstance(_nextUnitId, configId);
        _nextUnitId = checked(_nextUnitId + 1);
        return unit;
    }

    internal void AddGold(int amount)
    {
        if (amount < 0)
            throw new System.ArgumentOutOfRangeException(nameof(amount));

        Gold = checked(Gold + amount);
    }

    public void Dispose() { }
}