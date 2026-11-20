using System;
using UnityEngine;

public sealed class UnitAnimationPlayer
{
    private static readonly int IdleState = 
    Animator.StringToHash("Base Layer.Idle");

    private static readonly int MoveState =
    Animator.StringToHash("Base Layer.Move");

    private static readonly int AttackState =
    Animator.StringToHash("Base Layer.Attack");

    private static readonly int HitState =
        Animator.StringToHash("Base Layer.Hit");

    private static readonly int DeadState =
        Animator.StringToHash("Base Layer.Death");

    private bool _moving;
    private bool _dead;

    private bool _playingShortAction;
    private int _shortActionState;
    private int _actionStartedFrame = -1;

    public bool IsDead => _dead;

    private readonly Animator _animator;

    public UnitAnimationPlayer(Animator animator)
    {
        _animator = animator
            ?? throw new ArgumentNullException(nameof(animator));
    }

    public void SetMoving(bool moving)
    {
        //攻击期间也记录最新的移动意图
        _moving = moving;

        if (_dead || _playingShortAction)
        {
            return;
        }

        PlayLocomotion();
    }

    public void PlayAttack()
    {
        if (_dead)
            return;

        // 新攻击可以打断 Hit，或重新开始 Attack。
        PlayShortAction(AttackState);
    }

    public void PlayHit()
    {
        // 普通 Hit 不打断 Attack，也不重启正在播放的 Hit。
        if (_dead || _playingShortAction)
            return;

        PlayShortAction(HitState);
    }


    public void PlayDead()
    {
        if (_dead)
        {
            return;
        }

        _dead = true;
        _moving = false;
        _playingShortAction = false;
        _animator.Play(DeadState, 0, 0f);
    }

    public void Tick()
    {
        if (_dead || !_playingShortAction)
            return;

        //Play后，Animator的状态信息可能未更新
        if (_actionStartedFrame == Time.frameCount)
        {
            return;
        }

        if (_animator.IsInTransition(0))
        {
            return;
        }

        var current = _animator.GetCurrentAnimatorStateInfo(0);

        if (current.fullPathHash == _shortActionState &&
            current.normalizedTime < 1f)
        {
            return;
        }

        //Attack已结束，或者被其他动画打断
        _playingShortAction = false;
        PlayLocomotion();
    }

    private void PlayLocomotion()
    {
        if (_dead || _playingShortAction)
        {
            return;
        }

        if (_animator.IsInTransition(0))
        {
            return;
        }

        int stateHash = _moving ? MoveState : IdleState;

        var current = _animator.GetCurrentAnimatorStateInfo(0);

        if (current.fullPathHash == stateHash)
            return;

        _animator.Play(stateHash, 0, 0f);
    }

    private void PlayShortAction(int stateHash)
    {
        _playingShortAction = true;
        _shortActionState = stateHash;
        _actionStartedFrame = Time.frameCount;

        _animator.Play(stateHash, 0, 0f);
    }

}