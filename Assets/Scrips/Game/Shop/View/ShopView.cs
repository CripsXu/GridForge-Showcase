using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public partial class ShopView : ModuleView<ShopModule>
{
    [SerializeField] private RectTransform _sellAreaRect;
    private readonly List<RaycastResult> _sellHits = new();
    private readonly List<ShopCard> _cards = new();
    private EventBus EventBus => Module.GetEventBus();

    protected override void OnInitialize()
    {
        ValidateGeneratedBindings();
        // 复用已有卡片；数量不足时克隆已绑定模板，只在初始化时收集。
        _cards.AddRange(GetComponentsInChildren<ShopCard>(true));
        if (_cards.Count == 0)
            throw new InvalidOperationException("ShopView requires a bound ShopCard template.");
        var parent = _cards[0].transform.parent;
        foreach (var card in _cards)
            if (card.transform.parent != parent)
                throw new InvalidOperationException("ShopCards must share one row.");
        _cards.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        while (_cards.Count < Module.Model.SlotCount)
        {
            var card = Instantiate(_cards[0], parent, false);
            card.name = $"Slot{_cards.Count}";
            card.transform.SetSiblingIndex(_cards[0].transform.GetSiblingIndex() + _cards.Count);
            _cards.Add(card);
        }
        InitCards();
    }

    protected override void OnOpen()
    {
        _tmpShopTitle.text = "SHOP";
        _tmpRefreshSpec.text = "Free refresh (prototype)";
        _tmpRefreshLabel.text = "Refresh | Free";
        _tmpGoldSummary.text = $"Gold {Module.Controller.Gold}";
        _tmpShopStatusHint.text = "Choose a unit";
        _btnRefreshButton.interactable = true;
        _btnRefreshButton.onClick.AddListener(OnRefreshClicked);
        RenderOffers();
        EventBus.Subscribe(GameEvents.ShopOfferBought, RefreshOffers);
        EventBus.Subscribe(GameEvents.GoldChanged, OnGoldChanged);
        OnGoldChanged(Module.Controller.Gold);
    }

    private void RefreshOffers(int index)
    {
        var vo = Module.Controller.GetOfferVo(index);
        _cards[index].SetOffer(vo);
        _cards[index].Render();
        _cards[index].RenderHeader(index);
        Debug.Log($"Refreshed offer at index {index}");
    }



    private void InitCards()
    {
        var controller = Module.Controller;
        for (int i = 0; i < _cards.Count; i++)
        {
            bool visible = i < controller.SlotCount;
            _cards[i].gameObject.SetActive(visible);
            if (!visible) continue;
            int slotIndex = i;
            _cards[slotIndex].OnClick += () => OnCardClicked(slotIndex);
        }
    }

    private void OnCardClicked(int slotIndex)
    {
        if (!IsOpen) return;

        var result = Module.Controller.BuyOffer(slotIndex);

        _tmpShopStatusHint.text = result switch
        {
            ShopPurchaseResult.Success => "Purchased",
            ShopPurchaseResult.AlreadyPurchased => "Already sold",
            ShopPurchaseResult.NotEnoughGold => "Not enough gold",
            ShopPurchaseResult.BenchFull => "Bench is full",
            _ => "Unknown result"
        };

        OnGoldChanged(Module.Controller.Gold);
    }

    protected override void OnClose()
    {
        _btnRefreshButton.onClick.RemoveListener(OnRefreshClicked);
        EventBus.UnSubscribe(GameEvents.ShopOfferBought, RefreshOffers);
        EventBus.UnSubscribe(GameEvents.GoldChanged, OnGoldChanged);

    }

    private void OnRefreshClicked()
    {
        try
        {
            Module.Controller.RefreshOffers();
            RenderOffers();
            _tmpShopStatusHint.text = "Refreshed";
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            _tmpShopStatusHint.text = "Refresh failed";
        }
    }

    private void RenderOffers()
    {
        var controller = Module.Controller;
        for (int i = 0; i < _cards.Count; i++)
        {
            bool visible = i < controller.SlotCount;
            _cards[i].gameObject.SetActive(visible);
            if (!visible) continue;
            var vo = controller.GetOfferVo(i);
            _cards[i].SetOffer(vo);
            _cards[i].Render();
            _cards[i].RenderHeader(i);
        }
    }

    public bool ContainsSellArea(Vector2 screenPosition)
    {
        if (!IsOpen || !gameObject.activeInHierarchy ||
        _sellAreaRect == null || !_sellAreaRect.gameObject.activeInHierarchy)
        {
            return false;
        }

        Canvas canvas = _sellAreaRect.GetComponentInParent<Canvas>();
        if (canvas == null) return false;

        canvas = canvas.rootCanvas;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && eventCamera == null)
            return false;

        if (!RectTransformUtility.RectangleContainsScreenPoint(
            _sellAreaRect, screenPosition, eventCamera))
        {
            return false;
        }

        var eventsystem = EventSystem.current;
        if (eventsystem == null)
        {
            return false;
        }

        _sellHits.Clear();

        eventsystem.RaycastAll(new PointerEventData(eventsystem)
        {
            position = screenPosition,
        }, _sellHits);

        foreach (var hit in _sellHits)
        {
            if (!(hit.module is GraphicRaycaster))
            {
                continue;
            }
            Transform node = hit.gameObject.transform;
            return node == transform || node.IsChildOf(transform);
        }

        return false;
    }

    private void OnGoldChanged(int gold)
    {
        // 重新读取当前状态，也避免嵌套事件带来旧载荷显示。
        _tmpGoldSummary.text = $"Gold {Module.Controller.Gold}";
    }
}
