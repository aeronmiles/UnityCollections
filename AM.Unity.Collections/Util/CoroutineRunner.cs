
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineRunner : MonoSingleton<CoroutineRunner>
{
    public static void Run(IEnumerator coroutine) => I.StartCoroutine(coroutine);

    public static void RunAll(IEnumerable<IEnumerator> coroutines)
    {
        foreach (var coro in coroutines)
            I.StartCoroutine(coro);
    }

    public static void Stop(IEnumerator coroutine) => I.StartCoroutine(coroutine);

    public static void StopAll(IEnumerable<IEnumerator> coroutines)
    {
        foreach (var coro in coroutines)
            I.StopCoroutine(coro);
    }

    public static void RunSequential(IEnumerable<IEnumerator> coroutines, Action onComplete)
    {
        I.StartCoroutine(RunRoutinesSequential(coroutines, onComplete));
    }

    static IEnumerator RunRoutinesSequential(IEnumerable<IEnumerator> coroutines, Action onComplete)
    {
        foreach (var coroutine in coroutines)
            yield return I.StartCoroutine(coroutine);

        onComplete?.Invoke();
    }

    public static void RunSequential(IEnumerable<IEnumerator> coroutines)
    {
        I.StartCoroutine(RunRoutinesSequential(coroutines));
    }

    static IEnumerator RunRoutinesSequential(IEnumerable<IEnumerator> coroutines)
    {
        foreach (var coroutine in coroutines)
            yield return I.StartCoroutine(coroutine);
    }
}


public class CoroutineQueue
{
    List<IEnumerator> m_Routines = new();

    private bool disposed;

    public CoroutineQueue Clear()
    {
        m_Routines.Clear();
        return this;
    }

    public CoroutineQueue Enqueue(IEnumerator coroutine)
    {
        m_Routines.Add(coroutine);
        return this;
    }

    public CoroutineQueue EnqueueRange(IEnumerable<IEnumerator> coroutines)
    {
        m_Routines.AddRange(coroutines);
        return this;
    }

    public void RunAll()
    {
#if DEBUG
        if (m_Routines.Count == 0) Debug.LogWarning($"CoroutineQueue -> RunAll() :: Queue empty");
#endif
        CoroutineRunner.RunAll(m_Routines);
        m_Routines.Clear();
    }

    public void RunSequential()
    {
#if DEBUG
        if (m_Routines.Count == 0) Debug.LogWarning($"CoroutineQueue -> RunSequential() :: Queue empty");
#endif
        CoroutineRunner.RunSequential(m_Routines, () => m_Routines.Clear());
    }

    public void RunSequential(Action onComplete)
    {
#if DEBUG
        if (m_Routines.Count == 0) Debug.LogWarning($"CoroutineQueue -> RunSequential(Action onComplete) :: Queue empty");
#endif
        CoroutineRunner.RunSequential(m_Routines, () =>
        {
            m_Routines.Clear();
            onComplete?.Invoke();
        });
    }
}


public static class CoroutineRunnerExt
{
    public static void Run(this IEnumerator coroutine) => CoroutineRunner.Run(coroutine);

    public static void RunAll(this IEnumerable<IEnumerator> coroutines)
    {
        foreach (var coroutine in coroutines)
            CoroutineRunner.Run(coroutine);
    }

    public static void RunSequential(this IEnumerable<IEnumerator> coroutines) => CoroutineRunner.RunSequential(coroutines);

    public static void RunSequential(this IEnumerable<IEnumerator> coroutines, Action onComplete) => CoroutineRunner.RunSequential(coroutines, onComplete);
}