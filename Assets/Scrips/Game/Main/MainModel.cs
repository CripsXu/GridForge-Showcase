using System;

public sealed class MainModel : IModel
{
    public int Gold { get; private set; }
    public void Initialize() => Gold = 100;
    public void Dispose() { }

    // 模块内部应用已确认的状态，界面只能经 Controller 读取。
    internal bool ApplyGold(int gold)
    {
        if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
        if (Gold == gold) return false;
        Gold = gold;
        return true;
    }
}

