using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class UnitActor : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset _nameFont;
    private TextMeshPro _label;
    public int InstanceId { get; private set; }

    public void Bind(int instanceId)
    {
        if (instanceId <= 0) throw new ArgumentOutOfRangeException(nameof(instanceId));
        InstanceId = instanceId;
        gameObject.name = $"Unit_{InstanceId}";
    }

    // Called once, after UnitVisual selects the capsule or installs a model.
    public void InitializeName(string displayName, bool enemy)
    {
        if (_nameFont == null || _label != null)
            throw new InvalidOperationException("Check the unit name font and actor initialization.");

        var bounds = new Bounds(transform.position, Vector3.zero);
        foreach (var body in GetComponentsInChildren<Renderer>())
            if (body.enabled) bounds.Encapsulate(body.bounds);

        var child = new GameObject("Unit name", typeof(RectTransform));
        child.transform.SetParent(transform, false);
        child.transform.position = new Vector3(transform.position.x, bounds.max.y + 0.58f, transform.position.z);
        _label = child.AddComponent<TextMeshPro>();
        _label.font = _nameFont;
        _label.fontSize = 3.8f;
        _label.alignment = TextAlignmentOptions.Center;
        _label.enableWordWrapping = false;
        _label.raycastTarget = false;
        _label.rectTransform.sizeDelta = new Vector2(3f, .6f);
        _label.color = enemy ? new Color32(255, 184, 170, 255) : new Color32(202, 240, 255, 255);
        _label.text = displayName;
        _label.renderer.shadowCastingMode = ShadowCastingMode.Off;
        _label.renderer.receiveShadows = false;
        FaceCamera();
    }

    private void LateUpdate() => FaceCamera();

    private void FaceCamera()
    {
        var camera = Camera.main;
        if (_label != null && camera != null) _label.transform.rotation = camera.transform.rotation;
    }
}
