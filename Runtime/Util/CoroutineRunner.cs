
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineRunner : MonoSingleton<CoroutineRunner>
{
  public static void Run(IEnumerator coroutine)
  {
    _ = I.StartCoroutine(coroutine);
  }

  public static void RunAllAsync(IEnumerable<IEnumerator> coroutines)
  {
    foreach (var coro in coroutines)
    {
      _ = I.StartCoroutine(coro);
    }
  }

  public static void Stop(IEnumerator coroutine) => I.StartCoroutine(coroutine);

  public static void StopAll(IEnumerable<IEnumerator> coroutines)
  {
    foreach (var coro in coroutines)
    {
      I.StopCoroutine(coro);
    }
  }

  public static void RunSequential(IEnumerator coroutine, Action onSuccess = null)
  {
    _ = I.StartCoroutine(RunRoutinesSequential(new IEnumerator[] { coroutine }, onSuccess));
  }

  public static void RunSequential(IEnumerable<IEnumerator> coroutines, Action onSuccess = null)
  {
    _ = I.StartCoroutine(RunRoutinesSequential(coroutines, onSuccess));
  }

  private static IEnumerator RunRoutinesSequential(IEnumerable<IEnumerator> coroutines, Action onSuccess = null)
  {
    foreach (var coroutine in coroutines)
    {
      yield return I.StartCoroutine(coroutine);
    }

    onSuccess?.Invoke();
  }

  public static void RunSequential(IEnumerable<IEnumerator> coroutines)
  {
    _ = I.StartCoroutine(RunRoutinesSequential(coroutines));
  }
}

public class CoroutineQueue
{
  private readonly List<IEnumerator> _routines = new List<IEnumerator>();

  public CoroutineQueue Clear()
  {
    _routines.Clear();
    return this;
  }

  public CoroutineQueue Enqueue(IEnumerator coroutine)
  {
    _routines.Add(coroutine);
    return this;
  }

  public CoroutineQueue EnqueueRange(IEnumerable<IEnumerator> coroutines)
  {
    _routines.AddRange(coroutines);
    return this;
  }

  public void RunAll()
  {
#if DEBUG
    if (_routines.Count == 0)
    {
      Debug.LogWarning($"CoroutineQueue -> RunAll() :: Queue empty");
    }
#endif
    CoroutineRunner.RunAllAsync(_routines);
    _routines.Clear();
  }

  public void RunSequential(Action onSuccess = null)
  {
#if DEBUG
    if (_routines.Count == 0)
    {
      Debug.LogWarning($"CoroutineQueue -> RunSequential(Action onSuccess) :: Queue empty");
    }
#endif
    CoroutineRunner.RunSequential(_routines, () =>
    {
      _routines.Clear();
      onSuccess?.Invoke();
    });
  }
}

public static class CoroutineRunnerExt
{
  public static void Run(this IEnumerator coroutine) => CoroutineRunner.Run(coroutine);

  public static void RunAll(this IEnumerable<IEnumerator> coroutines)
  {
    foreach (var coroutine in coroutines)
    {
      CoroutineRunner.Run(coroutine);
    }
  }

  public static void RunSequential(this IEnumerable<IEnumerator> coroutines) => CoroutineRunner.RunSequential(coroutines);

  public static void RunSequential(this IEnumerable<IEnumerator> coroutines, Action onSuccess) => CoroutineRunner.RunSequential(coroutines, onSuccess);
}