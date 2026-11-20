using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BattleSceneRunner : MonoBehaviour
{
    [SerializeField] private BoardPresenter _boardPresenter;
    [SerializeField] private BattlePresenter _battlePresenter;
    [SerializeField] private PlacementInput _placementInput;

    [SerializeField] private TMPro.TMP_FontAsset _resultFont;
    [SerializeField] private float _moveSpeed = 2f;


    private enum Phase
    {
        Preparing,
        Fighting,
        Result,
        Returning
    }

    private ModuleManager _modules;
    private UIManager _ui;
    private BattleModule _battle;
    private Phase _phase = Phase.Preparing;

    private bool _initialized;
    private bool _shutdown;
    private bool _inputWasEnabled;

    private BattlePhaseView _phaseView;
    private int _resultFrame = -1;
    private int _returnFrame = -1;

    public void ActivatePhaseAction()
    {
        if (_phase == Phase.Result && Time.frameCount > _resultFrame) ReturnToPreparation();
        else if (CanEditPreparation) StartBattle();
    }
    public bool CanEditPreparation =>
    isActiveAndEnabled && _initialized && !_shutdown && _phase == Phase.Preparing;


    public void Initialize(ModuleManager modules, UIManager ui, RectTransform uiRoot)
    {
        if (_shutdown)
            throw new ObjectDisposedException(
                nameof(BattleSceneRunner));

        if (_initialized)
            return;

        if (_boardPresenter == null ||
            _battlePresenter == null ||
            _placementInput == null)
        {
            throw new InvalidOperationException(
                "BattleSceneRunner 的场景引用未绑定完整。");
        }

        _modules = modules
            ?? throw new ArgumentNullException(nameof(modules));

        _ui = ui
            ?? throw new ArgumentNullException(nameof(ui));

        if (_resultFont == null) throw new InvalidOperationException("Battle result font is missing.");
        _phaseView = BattlePhaseView.Create(uiRoot, this, _resultFont);
        _initialized = true;
    }

    public void StartBattle()
    {
        if (!_initialized ||
        _shutdown ||
        _phase != Phase.Preparing || Time.frameCount == _returnFrame)
        {
            return;
        }

        //只读取已有准备模块，不在这里自动进入商店
        var board = _modules.GetModule<BoardModule>();

        if (board == null)
        {
            Debug.LogWarning(
                "请先进入商店并上阵棋子。", this);
            return;
        }

        _phase = Phase.Fighting;

        _inputWasEnabled = _placementInput.enabled;

        //OnDisable 会取消尚未结束的拖拽
        _placementInput.enabled = false;

        bool started = false;

        try
        {
            var spawns = BuildSpawns(board.Model);

            if (spawns.Count == 0)
            {
                _phaseView.Show("Deploy a unit first   |   Start [B]", true);
                Debug.LogWarning(
                    "至少上阵一枚棋子才能开始战斗。", this);
                _placementInput.enabled = _inputWasEnabled;
                _phase = Phase.Preparing;
                return;
            }

            //真正开战才创建战斗模块
            var battle = _modules.GetOrCreate<BattleModule>();

            battle.Controller.StartBattle(
                spawns,
                _boardPresenter.CellSpacing,
                _moveSpeed
            );

            _battle = battle;
            started = true;

            _battlePresenter.ShowBattle(battle.Model);
            _boardPresenter.SetUnitsVisible(false);

            _ui.Close<ShopView>();
            _phaseView.Show("BATTLE   |   Auto combat", false);
        }
        catch (Exception error)
        {
            Debug.LogException(error, this);

            if (started)
            {
                // 本场已经创建成功，清理后返回准备阶段。
                ReturnToPreparation();
            }
            else
            {
                _placementInput.enabled = _inputWasEnabled;
                _phase = Phase.Preparing;
            }
        }
    }

    private List<BattleSpawnData> BuildSpawns(BoardModel board)
    {
        var spawns = new List<BattleSpawnData>();

        for (int col = 0; col < BoardModel.ColumnCount; col++)
        {
            for (int row = 0; row < BoardModel.PlayerRowCount; row++)
            {
                var unit = board.GetCell(col, row);

                if (unit == null)
                    continue;

                spawns.Add(new BattleSpawnData(
                    unit.ConfigId,
                    BattleTeam.Player,
                    _boardPresenter.GetCellLocalPosition(col, row),
                    unit.InstanceId));
            }
        }

        //没有上阵棋子时，不添加敌人
        if (spawns.Count == 0)
        {
            return spawns;
        }

        // 首版固定敌人，先验证战斗闭环。
        spawns.Add(new BattleSpawnData(
            91001,
            BattleTeam.Enemy,
            _boardPresenter.GetCellLocalPosition(3, 4)));

        spawns.Add(new BattleSpawnData(
            91002,
            BattleTeam.Enemy,
            _boardPresenter.GetCellLocalPosition(2, 6)));

        spawns.Add(new BattleSpawnData(
            91003,
            BattleTeam.Enemy,
            _boardPresenter.GetCellLocalPosition(4, 6)));

        spawns.Add(new BattleSpawnData(
            91004,
            BattleTeam.Enemy,
            _boardPresenter.GetCellLocalPosition(5, 5)));
        spawns.Add(new BattleSpawnData(
            91005,
            BattleTeam.Enemy,
            _boardPresenter.GetCellLocalPosition(6, 5)));

        return spawns;
    }

    private void Update()
    {
        if(!_initialized || _shutdown)
            return;

        var keyboard = Keyboard.current;

        if (_phase == Phase.Result)
        {
            if (keyboard != null && Time.frameCount > _resultFrame)
            {
                foreach (var key in keyboard.allKeys)
                {
                    if (!key.wasPressedThisFrame) continue;
                    ReturnToPreparation();
                    break;
                }
            }
            return;
        }

        if (_phase == Phase.Preparing)
        {
            if (keyboard != null &&
                keyboard.bKey.wasPressedThisFrame)
            {
                StartBattle();
            }

            return;
        }


        if (_phase == Phase.Fighting)
        {
            try
            {
                //先推进数据，再更新表现
                _battle.Controller.Tick(Time.deltaTime);
                _battlePresenter.RenderFrame();

                if (!_battle.Controller.IsRunning)
                {
                    _phase = Phase.Result;
                    _resultFrame = Time.frameCount;
                    _phaseView.ShowResult(_battle.Model.Outcome);
                    Debug.Log(
                        $"战斗结束：{_battle.Model.Outcome}", this);
                }
            }
            catch (System.Exception error)
            {
                Debug.LogException(error, this);
                ReturnToPreparation();
            }

            return;
        }


    }


    private async void ReturnToPreparation()
    {
        if (_shutdown || _phase == Phase.Returning)
            return;

        // await 期间仍保持 Returning，防止再次开战。
        _returnFrame = Time.frameCount;
        _phase = Phase.Returning;
        _phaseView.Show("Restoring preparation...", false);

        try
        {
            // 先解除表现对模型的引用，再清理战斗数据。
            _battlePresenter.Clear();

            if (_battle != null)
                _battle.Controller.ClearBattle();

            await _ui.OpenAsync<ShopView>("ui/shop");
        }
        catch (Exception error)
        {
            if (!_shutdown)
                Debug.LogException(error, this);
        }
        finally
        {
            // 等待期间可能已经退出游戏。
            if (!_shutdown)
            {
                _phaseView.Show("PREPARATION   |   Start battle [B]", true);
                _boardPresenter.SetUnitsVisible(true);
                _placementInput.enabled = _inputWasEnabled;

                _phase = Phase.Preparing;
            }
        }
    }



    public void Shutdown()
    {
        if (_shutdown)
            return;

        // 先停止 Update 和异步返回后的恢复操作。
        _shutdown = true;
        _initialized = false;


        if (_placementInput != null)
            _placementInput.enabled = false;

        if (_battlePresenter != null)
            _battlePresenter.Clear();

        if (_battle != null)
            _battle.Controller.ClearBattle();

        _battle = null;
        _modules = null;
        _ui = null;
    }


    private void OnDestroy()
    {
        Shutdown();
    }
}
