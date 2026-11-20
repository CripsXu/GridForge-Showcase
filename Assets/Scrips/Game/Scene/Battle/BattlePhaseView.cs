using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The scene owns the HUD. The runner owns all phase transitions.
public sealed class BattlePhaseView : MonoBehaviour
{
    private Image _background;
    private RectTransform _rect;
    private Image _resultBand;
    private TMP_FontAsset _phaseFont;
    private TMP_FontAsset _resultFont;
    private Button _button;
    private TextMeshProUGUI _label;
    private BattleSceneRunner _runner;

    public static BattlePhaseView Create(RectTransform parent, BattleSceneRunner runner, TMP_FontAsset resultFont)
    {
        var panel = new GameObject("Battle phase", typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = (RectTransform)panel.transform;
        rect.SetParent(parent, false);
        var band = new GameObject("Result banner", typeof(RectTransform), typeof(Image));
        var bandRect = (RectTransform)band.transform;
        bandRect.SetParent(rect, false);
        bandRect.anchorMin = new Vector2(0f, .5f);
        bandRect.anchorMax = new Vector2(1f, .5f);
        bandRect.sizeDelta = new Vector2(0f, 228f);

        var text = new GameObject("Status and action", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRect = (RectTransform)text.transform;
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

        var view = panel.AddComponent<BattlePhaseView>();
        view._runner = runner;
        view._rect = rect;
        view._background = panel.GetComponent<Image>();
        view._resultBand = band.GetComponent<Image>();
        view._resultBand.color = new Color(.055f, .095f, .13f, .94f);
        view._resultBand.raycastTarget = false;
        view._resultFont = resultFont;
        view._label = text.GetComponent<TextMeshProUGUI>();
        view._phaseFont = view._label.font;
        view._label.alignment = TextAlignmentOptions.Center;
        view._label.raycastTarget = false;
        view._button = panel.GetComponent<Button>();
        view._button.targetGraphic = panel.GetComponent<Image>();
        view._button.onClick.AddListener(view.Click);
        view.Show("PREPARATION   |   Start battle [B]", true);
        return view;
    }

    private void Click() => _runner.ActivatePhaseAction();

    public void Show(string message, bool interactive)
    {
        _rect.anchorMin = _rect.anchorMax = _rect.pivot = Vector2.one;
        _rect.anchoredPosition = new Vector2(-20f, -16f);
        _rect.sizeDelta = new Vector2(362f, 70f);
        _resultBand.gameObject.SetActive(false);
        _background.color = new Color(.09f, .18f, .24f, .97f);
        _label.font = _phaseFont;
        _label.fontSize = 22;
        _label.text = message;
        _button.interactable = interactive;
        transform.SetAsLastSibling();
    }

    public void ShowResult(BattleOutcome outcome)
    {
        string title = outcome switch
        {
            BattleOutcome.PlayerWin => "<color=#F6D27C>胜利</color>",
            BattleOutcome.EnemyWin => "<color=#FFAD9F>失败</color>",
            BattleOutcome.Draw => "<color=#C8DDF1>平局</color>",
            _ => throw new System.ArgumentOutOfRangeException(nameof(outcome))
        };
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        _rect.pivot = new Vector2(.5f, .5f);
        _rect.offsetMin = _rect.offsetMax = Vector2.zero;
        _background.color = new Color(.025f, .045f, .06f, .58f);
        _resultBand.gameObject.SetActive(true);
        _label.font = _resultFont;
        _label.fontSize = 72;
        _label.text = $"<b>{title}</b>\n<size=26><color=#D8E3E8>按任意键返回准备阶段</color></size>";
        _button.interactable = true;
        transform.SetAsLastSibling();
    }

    private void OnDestroy() => _button?.onClick.RemoveListener(Click);
}
