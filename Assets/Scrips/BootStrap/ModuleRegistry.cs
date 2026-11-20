using System;

public static class ModuleRegistry
{
    public static void RegisterApplication(
        ModuleManager modules,
        EventBus events,
        IGameSceneManager gameSceneMgr,
        UIManager uiManager)
    {
        if (modules == null)
            throw new ArgumentNullException(nameof(modules));
        if (events == null)
            throw new ArgumentNullException(nameof(events));
        if (gameSceneMgr == null)
            throw new ArgumentNullException(nameof(gameSceneMgr));
        if (uiManager == null)
            throw new ArgumentNullException(nameof(uiManager));

        modules.Register(
            () => new MainModule(events, gameSceneMgr, uiManager));
    }

    public static void RegisterMatch(
        ModuleManager modules,
        ConfigManager configs,
        EventBus events)
    {
        if (modules == null)
            throw new ArgumentNullException(nameof(modules));
        if (configs == null)
            throw new ArgumentNullException(nameof(configs));
        if (events == null)
            throw new ArgumentNullException(nameof(events));

        modules.Register(
            () => new PlayerModule());

        modules.Register(
            () => new BenchModule(configs, events));

        modules.Register(
            () => new ShopModule(
                configs,
                events,
                modules.GetOrCreate<PlayerModule>().Model,
                modules.GetOrCreate<BenchModule>().Model,
                modules.GetOrCreate<BoardModule>().Model));

        modules.Register(
            () => new BoardModule(
                modules.GetOrCreate<BenchModule>().Model,
                events));

        modules.Register(
            () => new BattleModule(configs));
    }

    // 迁移期间兼容旧入口，最终接线完成后移除。
    public static void RegisterAll(GameContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        RegisterApplication(
            context.ModuleManager,
            context.EventBus,
            context.gameSceneMgr,
            context.UIManager);

        RegisterMatch(
            context.ModuleManager,
            context.ConfigManager,
            context.EventBus);
    }
}