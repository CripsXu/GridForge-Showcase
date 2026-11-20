using System;

[Serializable]
public sealed class UnitConfig
{
    public int id;
    public string name;
    public int cost;
    public string prefabAddress;

    public string displayNameEn;
    public string portraitAssetId;
    public string rarityFrameAssetId;
    public string[] traits = Array.Empty<string>();

    public int maxHp;
    public int attack;
    public float attackIntervalSeconds;
    public int attackRangeCells;
}
