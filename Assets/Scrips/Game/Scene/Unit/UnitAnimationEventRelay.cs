using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAnimationEventRelay : MonoBehaviour
{
    // Start is called before the first frame update
    private UnitVisual _owner;

    public void Bind(UnitVisual owner)
    {
        _owner = owner;
    }

    public void AnimationEvent_SkillImpact()
    {
        _owner?.NotifySkillImpact();
    }

    public void AnimationEvent_SkillFinished()
    {
        _owner?.NotifySkillAnimationFinished();
    }
}
