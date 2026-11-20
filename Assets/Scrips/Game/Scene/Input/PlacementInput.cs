using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
using UnityEngine.UI;

public class PlacementInput : MonoBehaviour
{
    [SerializeField] private Camera _sceneCamera;

    private UnitActor _draggedActor; //拖着谁
    private Vector3 _startPosition;  //开始位置
    private Vector3 _dragOffset;    //点击位置与根节点的偏移
    private Plane _dragPlane;       //鼠标投影
    private BoardController _boardController;
    private BoardPresenter _boardPresenter;
    private bool _initialized;

    private BenchController _benchController;
    private BenchPresenter _benchPresenter;
    private ShopController _shopController;
    private UIManager _ui;
    private Func<bool> _canEditPreparation;
    private readonly List<RaycastResult> _uiHits = new();


    private void Awake()
    {
        if(_sceneCamera == null)
        {
            Debug.LogError("PlacementInput未绑定场景相机", this);
            enabled = false;
        }
    }

    public void Initialize(
        BoardController boardController,
        BoardPresenter boardPresenter,
        BenchController benchController,
        BenchPresenter benchPresenter,
        ShopController shopController,
        UIManager ui,
        Func<bool> canEditPreparation)
    {
        if (_initialized) return;
        _boardController = boardController ?? throw new ArgumentNullException(nameof(boardController));
        _boardPresenter = boardPresenter ?? throw new ArgumentNullException(nameof(boardPresenter));
        _benchController = benchController ?? throw new ArgumentNullException(nameof(benchController));
        _benchPresenter = benchPresenter ?? throw new ArgumentNullException(nameof(benchPresenter));
        _shopController = shopController ?? throw new ArgumentNullException(nameof(shopController));
        _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        _canEditPreparation = canEditPreparation ?? throw new ArgumentNullException(nameof(canEditPreparation));
        _initialized = true;
    }

    private void TryBeginDrag(Vector2 screenPosition)
    {
        // 点击UI时，不开始场景拖动
        if (!CanEditPreparation() || IsPointerOverUI(screenPosition))
            return;

        //只允许从场景相机显示的区域开始拖拽
        if (!_sceneCamera.pixelRect.Contains(screenPosition))
            return;

        Ray ray = _sceneCamera.ScreenPointToRay(screenPosition);

        if(!Physics.Raycast(ray, out RaycastHit hit, 100f))
            return;
        
        UnitActor actor = hit.collider.GetComponentInParent<UnitActor>();

        if(actor == null || actor.InstanceId <= 0)
            return;
        
        Plane plane = new Plane(Vector3.up, actor.transform.position);

        if(!plane.Raycast(ray, out float distance))
            return;
        
        _draggedActor = actor;
        _startPosition = actor.transform.position;
        _dragPlane = plane;

        Vector3 hitPoint = ray.GetPoint(distance);
        _dragOffset = _startPosition - hitPoint;
    }

    private bool MoveDrag(Vector2 screenPosition)
    {
        if (_draggedActor == null)
            return false;

        Ray ray = _sceneCamera.ScreenPointToRay(screenPosition);

        if (!_dragPlane.Raycast(ray, out float distance))
            return false;

        Vector3 hitPoint = ray.GetPoint(distance);
        _draggedActor.transform.position = hitPoint + _dragOffset;
        return true;
    }
    private void CancelDrag()
    {
        if (_draggedActor != null)
            _draggedActor.transform.position = _startPosition;

        _draggedActor = null;
    }

    private void OnDisable()
    {
        CancelDrag();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            CancelDrag();
    }

    private void FinishDrag(Vector2 screenPosition)
    {
        if (_draggedActor == null) return;
        UnitActor actor = _draggedActor;
        int instanceId = actor.InstanceId;
        Vector3 startPosition = _startPosition;
        bool succeeded = false;

        try
        {
            if (!CanEditPreparation()) return;

            var shop = GetShopViewIfCreated();
            if (shop != null && shop.ContainsSellArea(screenPosition))
            {
                _draggedActor = null;
                var result = _shopController.TrySellUnit(instanceId);
                succeeded = result == UnitSaleResult.Success;
                if (!succeeded) Debug.Log($"出售失败：{result}", this);
                return;
            }

            if (IsPointerOverUI(screenPosition) ||
                !_sceneCamera.pixelRect.Contains(screenPosition) ||
                !MoveDrag(screenPosition))
                return;

            Vector3 worldPosition = actor.transform.position;
            if (_benchPresenter.TryGetSlot(worldPosition, out int benchIndex))
            {
                _draggedActor = null;
                var result = _benchController.TryMoveUnit(instanceId, benchIndex);
                if (result == BenchPlacementResult.UnitNotFound)
                    result = _boardController.TryReturnToBench(instanceId, benchIndex);
                succeeded = result == BenchPlacementResult.Success;
                if (!succeeded && result != BenchPlacementResult.NoChange)
                    Debug.Log($"备战席放置失败：{result}", this);
                return;
            }

            if (_boardPresenter.TryGetCell(worldPosition, out int col, out int row))
            {
                _draggedActor = null;
                var result = _boardController.TryPlaceUnit(instanceId, col, row);
                succeeded = result == BoardPlacementResult.Success;
                if (!succeeded) Debug.Log($"棋盘放置失败：{result}", this);
            }
        }
        catch (Exception error)
        {
            Debug.LogException(error, this);
        }
        finally
        {
            _draggedActor = null;
            if (!succeeded && actor != null)
                actor.transform.position = startPosition;
        }
    }

    private bool CanEditPreparation()
    {
        return _initialized && isActiveAndEnabled &&
        _canEditPreparation != null && _canEditPreparation();
    }

    private ShopView GetShopViewIfCreated()
    {
        try { return _ui.Get<ShopView>(); }
        catch (InvalidOperationException error)
            when (!(error is ObjectDisposedException))
        {
            return null;
        }
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }
        _uiHits.Clear();
        eventSystem.RaycastAll(new PointerEventData(eventSystem)
        {
            position = screenPosition
        }, _uiHits);
        foreach (var hit in _uiHits)
            if (hit.module is GraphicRaycaster) return true;
        return false;
    }

    private void LateUpdate()
    {
        Mouse mouse = Mouse.current;
        if (!CanEditPreparation() || mouse == null || _sceneCamera == null ||
            _boardPresenter == null || _benchPresenter == null)
        {
            CancelDrag();
            return;
        }

        Keyboard keyboard = Keyboard.current;

        bool cancelPressed = keyboard != null &&
            keyboard.escapeKey.wasPressedThisFrame;

        if (cancelPressed)
        {
            CancelDrag();
            return;
        }

        Vector2 screenPosition = mouse.position.ReadValue();

        // 必须先处理松手，否则会被下面的 !isPressed 当成取消。
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            FinishDrag(screenPosition);
            return;
        }

        if (!mouse.leftButton.isPressed)
        {
            CancelDrag();
            return;
        }

        if (_draggedActor == null &&
            mouse.leftButton.wasPressedThisFrame)
        {
            TryBeginDrag(screenPosition);
        }

        if (_draggedActor != null)
            MoveDrag(screenPosition);
    }
}
