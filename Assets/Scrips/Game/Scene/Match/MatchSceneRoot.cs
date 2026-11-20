using System;
using System.Threading.Tasks;
using UnityEngine;

public sealed class MatchSceneRoot : MonoBehaviour
{
    [SerializeField] private RectTransform _uiRoot;
    [SerializeField] private BenchPresenter _benchPresenter;
    [SerializeField] private BoardPresenter _boardPresenter;
    [SerializeField] private PlacementInput _placementInput;
    [SerializeField] private BattleSceneRunner _battleSceneRunner;
    [SerializeField] private BattlePresenter _battlePresenter;
    // 棋子表现根节点 Prefab，原来在各 Presenter 上配置
    [SerializeField] private UnitActor _unitActorPrefab;

    // 整局共用一个棋子管理器
    private UnitActorManager _unitActorManager;

    // 每个场景入口都有自己的一套局内对象。
    private readonly ModuleManager _modules = new();
    private readonly EventBus _events = new();
    private UIManager _ui;

    private bool _started;
    private bool _initialized;
    private bool _shutdown;

    private void Awake()
    {
        SetGameplayEnabled(false);
    }

    public async Task InitializeAsync(
        ResourceManager resources,
        ConfigManager configs,
        MatchResources matchResources)
    {
        ThrowIfShutdown();

        if (_started)
            throw new InvalidOperationException(
                "本局场景不能重复初始化。");

        if (resources == null)
            throw new ArgumentNullException(nameof(resources));

        if (configs == null)
            throw new ArgumentNullException(nameof(configs));

        if (matchResources == null)
            throw new ArgumentNullException(nameof(matchResources));

        ValidateSceneComponent(_uiRoot, nameof(_uiRoot));
        ValidateSceneComponent(
            _benchPresenter, nameof(_benchPresenter));
        ValidateSceneComponent(
            _boardPresenter, nameof(_boardPresenter));
        ValidateSceneComponent(
            _placementInput, nameof(_placementInput));
        ValidateSceneComponent(
            _battleSceneRunner, nameof(_battleSceneRunner));
        ValidateSceneComponent(
            _battlePresenter, nameof(_battlePresenter));

        _started = true;

        try
        {
            var unitConfigs = configs.Get<UnitConfigTable>();

            ModuleRegistry.RegisterMatch(
                _modules, configs, _events);

            _ui = new UIManager(
                resources, _uiRoot, _modules, _events);

            await matchResources.PreloadAsync(
                unitConfigs.GetAll());

            ThrowIfShutdown();

            InitializeGameplay(matchResources, unitConfigs);

            await _ui.OpenAsync<ShopView>("ui/shop");
            ThrowIfShutdown();

            await _ui.OpenAsync<BenchView>("ui/bench");
            ThrowIfShutdown();

            _initialized = true;
            Activate();
        }
        catch
        {
            Shutdown();
            throw;
        }
    }

    private void InitializeGameplay(MatchResources matchResources, UnitConfigTable unitConfigs)
    {
        var bench = _modules.GetOrCreate<BenchModule>();
        var board = _modules.GetOrCreate<BoardModule>();
        var shop = _modules.GetOrCreate<ShopModule>();

        _unitActorManager = new UnitActorManager(
            _unitActorPrefab, matchResources, transform, unitConfigs);
        _events.Subscribe(GameEvents.UnitSold, OnUnitSold);

        _benchPresenter.Initialize(bench.Controller, _events, _unitActorManager);
        _boardPresenter.Initialize(board.Controller, _events, _unitActorManager);
        _battlePresenter.Initialize(_unitActorManager);
        _battleSceneRunner.Initialize(_modules, _ui, _uiRoot);
        _placementInput.Initialize(
            board.Controller,
            _boardPresenter,
            bench.Controller,
            _benchPresenter,
            shop.Controller,
            _ui,
            () => _battleSceneRunner.CanEditPreparation);
    }

    private void OnUnitSold(int instanceId)
    {
        _unitActorManager?.Release(instanceId);
    }

    public void Activate()
    {
        ThrowIfShutdown();

        if (!_initialized)
            throw new InvalidOperationException(
                "对局场景尚未初始化。");

        SetGameplayEnabled(true);
    }

    public void Shutdown()
    {
        if (_shutdown)
            return;

        _shutdown = true;
        _initialized = false;

        try
        {
            SetGameplayEnabled(false);

            if (_battleSceneRunner != null)
                _battleSceneRunner.Shutdown();
        }
        finally
        {
            try
            {
                _events.UnSubscribe(GameEvents.UnitSold, OnUnitSold);
                _unitActorManager?.Dispose();
                _unitActorManager = null;
            }
            finally
            {
                try
                {
                    _ui?.Dispose();
                }
                finally
                {
                    _modules.DisposeAllModules();
                }
            }
        }

        // MatchResources 由导航持有，
        // 等场景卸载完成后再释放。
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (_placementInput != null)
            _placementInput.enabled = enabled;

        if (_battleSceneRunner != null)
            _battleSceneRunner.enabled = enabled;
    }

    private void ValidateSceneComponent(
        Component component,
        string fieldName)
    {
        if (component == null)
            throw new InvalidOperationException(
                $"对局场景未绑定：{fieldName}");

        if (component.gameObject.scene != gameObject.scene)
            throw new InvalidOperationException(
                $"{fieldName} 必须属于当前对局场景。");
    }

    private void ThrowIfShutdown()
    {
        if (this == null || _shutdown)
            throw new OperationCanceledException(
                "本局场景已经关闭。");
    }

    private void OnDestroy()
    {
        Shutdown();
    }
}