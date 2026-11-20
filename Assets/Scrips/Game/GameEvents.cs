public static class GameEvents
{
    public static readonly EventKey<int> GoldChanged = new();
    public static readonly EventKey<int> ShopOfferBought = new();
    public static readonly EventKey<int> BenchChanged = new();
    public static readonly EventKey<BoardCellChanged> BoardChanged = new();

    public static readonly EventKey<int> UnitSold = new();
}