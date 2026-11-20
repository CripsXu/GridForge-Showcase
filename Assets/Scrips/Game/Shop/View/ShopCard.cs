// 手写表现逻辑；控件引用由 ShopCard.Bindings.g.cs 生成。
using System;
using UnityEngine;

public partial class ShopCard : global::UnityEngine.MonoBehaviour
{
    [Serializable]
    private sealed class CardSprite
    {
        public string assetId;
        public Sprite sprite;
    }

    // 小规模卡面素材随 Prefab 引用；配置和 VO 只保存资源标识。
    [SerializeField] private CardSprite[] _cardSprites = Array.Empty<CardSprite>();
    private ShopOfferVO _vo;

    public event Action OnClick;
    
    private void Awake()
    {
        ValidateGeneratedBindings();

        _btnRoot.onClick.AddListener(HandleClick);
    }

    private void HandleClick() => OnClick?.Invoke();

    private void OnDestroy()
    {
        if (_btnRoot != null)
            _btnRoot.onClick.RemoveListener(HandleClick);
    }

    public void SetOffer(ShopOfferVO vo)
    {
        _vo = vo ?? throw new ArgumentNullException(nameof(vo));
    }

    public void Render()
    {
        ValidateGeneratedBindings();

        if (_vo == null)
            throw new InvalidOperationException("ShopCard 尚未设置商品数据。");

        // 每次渲染都重新填写内容。
        _tmpUnitName.text = _vo.Name;
        _tmpCostText.text = _vo.Cost.ToString();
        _tmpTraitsText.text = _vo.TraitsText;
        _imagePortraitPlaceholder.sprite = GetSprite(_vo.PortraitAssetId);
        _imageRarityFrame.sprite = GetSprite(_vo.RarityFrameAssetId);
        _tmpStateHint.text = "Click to Buy";
        _tmpSoldLabel.text = "Sold";

        RenderState(_vo.IsPurchased);
    }

    private void RenderState(bool sold)
    {
        // 商品内容统一归到 ContentRoot，不再逐个开关。
        _goContentRoot.SetActive(!sold);
        _goSelected.SetActive(false);
        _goInsufficient.SetActive(false);
        _goPending.SetActive(false);
        _goSold.SetActive(sold);
        _goStateLayer.SetActive(sold);
        _btnRoot.interactable = !sold;
    }

    private Sprite GetSprite(string assetId)
    {
        foreach (var entry in _cardSprites)
            if (entry != null && entry.assetId == assetId && entry.sprite != null)
                return entry.sprite;

        throw new InvalidOperationException($"ShopCard 未配置卡面素材：{assetId}");
    }

    public void RenderHeader(int slotIndex)
    {
        _tmpUnitTier.text =
            $"SLOT {slotIndex + 1:00} / ID {_vo.ConfigId}";
        _tmpQualityText.text = "";
    }

}
