// GridForge UI skeleton. User-owned; never overwritten by the generator.
public partial class BenchSlot : global::UnityEngine.MonoBehaviour
{
    private void Awake()
    {
        ValidateGeneratedBindings();
    }

    public void Render(BenchSlotVO vo)
    {
        ValidateGeneratedBindings();

        bool occupied = vo != null;

        // 生成路径显示三个状态位于 StateLayer 下。
        // 子对象启用时，父对象也必须启用。
        _goStateLayer.SetActive(true);

        _goEmpty.SetActive(!occupied);
        _goOccupied.SetActive(occupied);
        _goSelected.SetActive(false);

        _tmpEmptyLabel.text = "Empty";
        _tmpOccupiedLabel.text = occupied
            ? $"{vo.Name} #{vo.InstanceId:00}"
            : "";
    }

    // Add rendering and interaction here. Keep game rules in the business layer.
}
