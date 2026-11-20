using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainView : ModuleView<MainModule>
{
    [SerializeField] private Button _PlayGameButton;
    [SerializeField] private Button _TestDragButton;
    [SerializeField] private TMP_Text _TipText;
    private bool _opening;

    protected override void OnInitialize()
    {
        if (_PlayGameButton == null || _TipText == null)
            throw new InvalidOperationException("MainView button and text must be assigned.");
        // 复用原演示按钮的序列化引用。
        var label = _PlayGameButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = "Enter match";
    }

    protected override void OnOpen()
    {
        _TipText.text = "GRIDFORGE";
        _PlayGameButton.interactable = !_opening;
        _TestDragButton.gameObject.SetActive(false);
        _PlayGameButton.onClick.AddListener(OnClicked);
    }

    protected override void OnClose() => _PlayGameButton.onClick.RemoveListener(OnClicked);

    private async void OnClicked()
    {
        if (_opening) return;
        _opening = true;
        _PlayGameButton.interactable = false;
        try { await Module.Controller.EnterMatchAsync(); }
        catch (OperationCanceledException) when (IsDisposed) { }
        catch (Exception error)
        {
            Debug.LogException(error);
            if (!IsDisposed) _TipText.text = "Open failed. Please retry.";
        }
        finally
        {
            _opening = false;
            if (!IsDisposed) _PlayGameButton.interactable = true;
        }
    }

}
