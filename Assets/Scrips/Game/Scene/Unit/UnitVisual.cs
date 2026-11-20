using System;
using System.Threading.Tasks;
using UnityEngine;

public sealed class UnitVisual : MonoBehaviour
{
    [SerializeField] private Renderer _placeholderRenderer;
    public Animator animator {get; private set;}

    private bool _initialized;
    private UnitAnimationPlayer _animationPlayer;
    [SerializeField] private string _skillTriggerName = "skill";

    private TaskCompletionSource<bool> _skillCompletion;
    public event Action SkillImpact;

    public void Initialize(GameObject prefab)
    {
        if (this == null)
            throw new ObjectDisposedException(nameof(UnitVisual));

        if (_initialized)
            throw new InvalidOperationException(
                "UnitVisual 不能重复初始化。");

        if (_placeholderRenderer == null)
            throw new InvalidOperationException(
                "未绑定占位 Renderer。");

        // 旧角色没有模型地址，继续显示胶囊。
        if (prefab == null)
        {
            _initialized = true;
            return;
        }

        //创建完整角色，保留其内部的骨骼和动画层级
        var model = Instantiate(prefab, transform, false);

        try
        {
            var anim = model.GetComponentInChildren<Animator>();

            if (anim == null || anim.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException(
    $"角色缺少 Animator 或 Controller：{prefab.name}");
            }

            animator = anim;
            _animationPlayer = new UnitAnimationPlayer(animator);

            // 将动画事件转发器绑定到当前 UnitVisual
            var relay = anim.GetComponent<UnitAnimationEventRelay>();

            if (relay == null)
            {
                relay = anim.gameObject.AddComponent<UnitAnimationEventRelay>();
            }

            relay.Bind(this);

            //只改变胶囊外观，保留拾取碰撞体
            _placeholderRenderer.enabled = false;

            _initialized = true;
        }
        catch (System.Exception)
        {
            model.SetActive(false);
            Destroy(model);
            throw;
        }
    }

    public void SetMoving(bool moving)
    {
        // 保留已有技能手动测试，测试期间不被待机打断。
        if (_skillCompletion != null)
            return;

        _animationPlayer?.SetMoving(moving);
    }

    public void PlayAttack()
    {
        if (_skillCompletion != null)
            return;

        _animationPlayer?.PlayAttack();
    }

    public void PlayHit()
    {
        if (_skillCompletion != null)
            return;

        _animationPlayer?.PlayHit();
    }

    public void PlayDead()
    {
        // 死亡优先，不能继续等待技能测试完成。
        CancelSkillAnimationWait();

        _animationPlayer?.PlayDead();
    }

    private void Update()
    {
        if (_skillCompletion != null)
            return;

        _animationPlayer?.Tick();
    }

    public Task PlaySkillAsync()
    {
        if (!_initialized || animator == null)
        {
            throw new InvalidOperationException(
                "UnitVisual 尚未初始化或没有 Animator。");
        }

        if (_skillCompletion != null)
            throw new InvalidOperationException(
                "当前技能动画尚未结束。");

        if (_animationPlayer?.IsDead == true)
        {
            throw new InvalidOperationException(
                "死亡表现已经开始，不能再播放技能。");
        }

        var completion = new TaskCompletionSource<bool>();

        _skillCompletion = completion;

        animator.ResetTrigger(_skillTriggerName);
        animator.SetTrigger(_skillTriggerName);

        return completion.Task;
    }

    public void NotifySkillImpact()
    {
        SkillImpact?.Invoke();
    }

    public void NotifySkillAnimationFinished()
    {
        var completion = _skillCompletion;
        if(completion == null)
        {
            return;
        }

        _skillCompletion = null;
        completion.TrySetResult(true);
    }

    public void CancelSkillAnimationWait()
    {
        var completion = _skillCompletion;
        _skillCompletion = null;

        completion?.TrySetCanceled();
    }
}