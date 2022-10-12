using System;
using UnityEngine;

public abstract class AnimStateBehaviour<T> : StateMachineBehaviour
{
    [SerializeField] protected T m_Tag;
    public static event Action<AnimState<T>> OnStateEntered;
    public static event Action<AnimState<T>> OnStateExited;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnStateEntered?.Invoke(new AnimState<T>()
        {
            Tag = m_Tag,
            Animator = animator,
            AnimatorStateInfo = stateInfo,
            LayerIndex = layerIndex
        });

#if DEBUG
        Debug.Log($"AnimStateBehaviour<{typeof(T).Name}> -> OnStateEnter() :: Tag<{typeof(T).Name}> = {m_Tag}");
#endif
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnStateExited?.Invoke(new AnimState<T>()
        {
            Tag = m_Tag,
            Animator = animator,
            AnimatorStateInfo = stateInfo,
            LayerIndex = layerIndex
        });

#if DEBUG
        Debug.Log($"AnimStateBehaviour<{typeof(T).Name}> -> OnStateExit() :: Tag<{typeof(T).Name}> = {m_Tag}");
#endif
    }
}

public struct AnimState<T>
{
    public T Tag;
    public Animator Animator;
    public AnimatorStateInfo AnimatorStateInfo;
    public int LayerIndex;
}