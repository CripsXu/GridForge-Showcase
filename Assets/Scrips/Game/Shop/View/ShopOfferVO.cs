public class ShopOfferVO
{
    public int ConfigId { get; }
    public string Name { get; }
    public int Cost { get; }
    public bool IsPurchased { get; }
    public string PortraitAssetId { get; }
    public string RarityFrameAssetId { get; }
    public string TraitsText { get; }

    public ShopOfferVO(
        int configId,
        string name,
        int cost,
        bool isPurchased,
        string portraitAssetId = "",
        string rarityFrameAssetId = "",
        string traitsText = "")
    {
        ConfigId = configId;
        Name = name;
        Cost = cost;
        IsPurchased = isPurchased;
        PortraitAssetId = portraitAssetId ?? "";
        RarityFrameAssetId = rarityFrameAssetId ?? "";
        TraitsText = traitsText ?? "";
    }
}
