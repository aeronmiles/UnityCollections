using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityCollections.Functional
{
  /// <summary>
  /// Extension methods for Result type to provide additional functionality and integration.
  /// </summary>
  public static class ResultExt
{
  /// <summary>
  /// Converts a nullable value to a Result, treating null as an error.
  /// </summary>
  public static Result<T, E> OkOrErr<T, E>(this T? value, E error) where T : class
  {
    return value != null ? Result<T, E>.Ok(value) : Result<T, E>.Err(error);
  }

  /// <summary>
  /// Converts a nullable value to a Result, treating null as an error.
  /// </summary>
  public static Result<T, E> OkOrErr<T, E>(this T? value, E error) where T : struct
  {
    return value.HasValue ? Result<T, E>.Ok(value.Value) : Result<T, E>.Err(error);
  }

  /// <summary>
  /// Converts a nullable value to a Result with a default error message.
  /// </summary>
  public static Result<T, string> OkOrErr<T>(this T? value, string errorMessage = "Value was null") where T : class
  {
    return value.OkOrErr(errorMessage);
  }

  /// <summary>
  /// Converts a nullable value to a Result with a default error message.
  /// </summary>
  public static Result<T, string> OkOrErr<T>(this T? value, string errorMessage = "Value was null") where T : struct
  {
    return value.OkOrErr(errorMessage);
  }

  /// <summary>
  /// Converts a boolean condition to a Result.
  /// </summary>
  public static Result<T, E> OkIf<T, E>(this T value, bool condition, E error)
  {
    return condition ? Result<T, E>.Ok(value) : Result<T, E>.Err(error);
  }

  /// <summary>
  /// Converts a boolean condition to a Result with a predicate.
  /// </summary>
  public static Result<T, E> OkIf<T, E>(this T value, Func<T, bool> predicate, E error)
  {
    return predicate(value) ? Result<T, E>.Ok(value) : Result<T, E>.Err(error);
  }

  /// <summary>
  /// Filters a Result by a predicate, converting Ok to Err if the predicate fails.
  /// </summary>
  public static Result<T, E> Filter<T, E>(this Result<T, E> result, Func<T, bool> predicate, E error)
  {
    return result.AndThen(value => predicate(value) ? Result<T, E>.Ok(value) : Result<T, E>.Err(error));
  }

  /// <summary>
  /// Flattens a nested Result.
  /// </summary>
  public static Result<T, E> Flatten<T, E>(this Result<Result<T, E>, E> result)
  {
    return result.AndThen(inner => inner);
  }

  /// <summary>
  /// Returns the Ok value or null if the Result is Err.
  /// Note: This loses error information - use TryGetValue for error-preserving access.
  /// </summary>
  public static T? ValueOrNull<T, E>(this Result<T?, E> result) where T : class
  {
    return result.Match(
      value => value,
      error => default(T)
    );
  }

  /// <summary>
  /// Collects a sequence of Results into a Result of a sequence.
  /// If any Result is Err, returns the first Err found.
  /// Note: This method allocates a new List. Use the overload with destination List for hot paths.
  /// </summary>
  public static Result<IEnumerable<T>, E> Collect<T, E>(this IEnumerable<Result<T, E>> results)
  {
    var list = new List<T>();
    foreach (var result in results)
    {
      if (result.IsErr)
        return Result<IEnumerable<T>, E>.Err(result.UnwrapErr());
      list.Add(result.Unwrap());
    }
    return Result<IEnumerable<T>, E>.Ok(list);
  }

  /// <summary>
  /// Collects a sequence of Results into the provided destination list.
  /// If any Result is Err, returns the first Err found and leaves destination in partial state.
  /// Zero-allocation overload for hot paths.
  /// </summary>
  /// <returns>Ok(count) if all succeeded, or Err(first error) if any failed.</returns>
  public static Result<int, E> Collect<T, E>(this IEnumerable<Result<T, E>> results, List<T> destination)
  {
    destination.Clear();
    foreach (var result in results)
    {
      if (result.TryGetError(out var error))
        return Result<int, E>.Err(error);
      if (result.TryGetValue(out var value))
        destination.Add(value);
    }
    return Result<int, E>.Ok(destination.Count);
  }

  /// <summary>
  /// Collects a sequence of Results into a Result of a List.
  /// If any Result is Err, returns the first Err found.
  /// Optimized single-pass implementation.
  /// </summary>
  public static Result<List<T>, E> CollectList<T, E>(this IEnumerable<Result<T, E>> results)
  {
    var list = new List<T>();
    foreach (var result in results)
    {
      if (result.TryGetError(out var error))
        return Result<List<T>, E>.Err(error);
      if (result.TryGetValue(out var value))
        list.Add(value);
    }
    return Result<List<T>, E>.Ok(list);
  }

  /// <summary>
  /// Partitions a sequence of Results into Ok and Err values.
  /// Note: This method allocates new Lists. Use the overload with destination lists for hot paths.
  /// </summary>
  public static (List<T> oks, List<E> errs) Partition<T, E>(this IEnumerable<Result<T, E>> results)
  {
    var oks = new List<T>();
    var errs = new List<E>();
    
    foreach (var result in results)
    {
      result.Match(
        ok => oks.Add(ok),
        err => errs.Add(err)
      );
    }
    
    return (oks, errs);
  }

  /// <summary>
  /// Partitions a sequence of Results into the provided destination lists.
  /// Zero-allocation overload for hot paths.
  /// </summary>
  /// <returns>A tuple of (ok count, error count).</returns>
  public static (int okCount, int errCount) Partition<T, E>(this IEnumerable<Result<T, E>> results, List<T> oks, List<E> errs)
  {
    oks.Clear();
    errs.Clear();
    
    foreach (var result in results)
    {
      if (result.TryGetValue(out var okValue))
        oks.Add(okValue);
      else if (result.TryGetError(out var errValue))
        errs.Add(errValue);
    }
    
    return (oks.Count, errs.Count);
  }

  /// <summary>
  /// Tries to find the first Ok value in a sequence of Results.
  /// Returns the last Err encountered if no Ok values are found.
  /// </summary>
  public static Result<T, E> FirstOk<T, E>(this IEnumerable<Result<T, E>> results)
  {
    Result<T, E> lastErr = default;
    bool hasResults = false;
    
    foreach (var result in results)
    {
      hasResults = true;
      if (result.IsOk)
        return result;
      lastErr = result;
    }
    
    return hasResults ? lastErr : default;
  }

  /// <summary>
  /// Tries to find the first Ok value in a sequence of Results.
  /// Returns the provided error if no Ok values are found.
  /// </summary>
  public static Result<T, E> FirstOkOr<T, E>(this IEnumerable<Result<T, E>> results, E defaultError)
  {
    foreach (var result in results)
    {
      if (result.IsOk)
        return result;
    }
    return Result<T, E>.Err(defaultError);
  }

  /// <summary>
  /// Tries to find the first Ok value in a sequence of Results.
  /// Computes an error using the provided function if no Ok values are found.
  /// </summary>
  public static Result<T, E> FirstOkOrElse<T, E>(this IEnumerable<Result<T, E>> results, Func<E> errorFactory)
  {
    foreach (var result in results)
    {
      if (result.IsOk)
        return result;
    }
    return Result<T, E>.Err(errorFactory());
  }

  /// <summary>
  /// Applies a function that returns a Result to each element and collects the Ok results.
  /// Note: This method allocates via LINQ. Use the overload with List&lt;T&gt; destination for hot paths.
  /// </summary>
  public static IEnumerable<T> FilterMap<TSource, T, E>(this IEnumerable<TSource> source, Func<TSource, Result<T, E>> mapper)
  {
    return source.Select(mapper).Where(r => r.IsOk).Select(r => r.Unwrap());
  }

  /// <summary>
  /// Applies a function that returns a Result to each element and collects the Ok results into a destination list.
  /// Zero-allocation overload for hot paths.
  /// </summary>
  /// <returns>The number of items added to the destination list.</returns>
  public static int FilterMap<TSource, T, E>(this IEnumerable<TSource> source, Func<TSource, Result<T, E>> mapper, List<T> destination)
  {
    destination.Clear();
    foreach (var item in source)
    {
      var result = mapper(item);
      if (result.TryGetValue(out var value))
        destination.Add(value);
    }
    return destination.Count;
  }

  /// <summary>
  /// Executes an action if the Result is Ok, returning the original Result.
  /// </summary>
  public static Result<T, E> Inspect<T, E>(this Result<T, E> result, Action<T> action)
  {
    if (result.IsOk)
      action(result.Unwrap());
    return result;
  }

  /// <summary>
  /// Executes an action if the Result is Err, returning the original Result.
  /// </summary>
  public static Result<T, E> InspectErr<T, E>(this Result<T, E> result, Action<E> action)
  {
    if (result.IsErr)
      action(result.UnwrapErr());
    return result;
  }

  /// <summary>
  /// Logs the Ok value using Unity's Debug.Log.
  /// </summary>
  public static Result<T, E> LogOk<T, E>(this Result<T, E> result, string prefix = "Ok")
  {
    return result.Inspect(value => Debug.Log($"{prefix}: {value}"));
  }

  /// <summary>
  /// Logs the Err value using Unity's Debug.LogError.
  /// </summary>
  public static Result<T, E> LogErr<T, E>(this Result<T, E> result, string prefix = "Error")
  {
    return result.InspectErr(error => Debug.LogError($"{prefix}: {error}"));
  }

  /// <summary>
  /// Logs both Ok and Err values appropriately.
  /// </summary>
  public static Result<T, E> Log<T, E>(this Result<T, E> result, string okPrefix = "Ok", string errPrefix = "Error")
  {
    return result.LogOk(okPrefix).LogErr(errPrefix);
  }

  /// <summary>
  /// Converts a Result to a Task for async operations.
  /// Note: This allocates a Task. Use ToValueTask for hot paths.
  /// </summary>
  public static Task<Result<T, E>> ToTask<T, E>(this Result<T, E> result)
  {
    return Task.FromResult(result);
  }

  /// <summary>
  /// Converts a Result to a ValueTask for async operations.
  /// Zero-allocation overload for hot paths.
  /// </summary>
  public static ValueTask<Result<T, E>> ToValueTask<T, E>(this Result<T, E> result)
  {
    return new ValueTask<Result<T, E>>(result);
  }

  /// <summary>
  /// Awaits a Task and wraps exceptions in a Result.
  /// </summary>
  public static async Task<Result<T, Exception>> ToResult<T>(this Task<T> task)
  {
    try
    {
      var result = await task;
      return Result<T, Exception>.Ok(result);
    }
    catch (Exception ex)
    {
      return Result<T, Exception>.Err(ex);
    }
  }

  /// <summary>
  /// Awaits a Task and wraps exceptions in a Result.
  /// </summary>
  public static async Task<Result<Unit, Exception>> ToResult(this Task task)
  {
    try
    {
      await task;
      return Result<Unit, Exception>.Ok(Unit.Value);
    }
    catch (Exception ex)
    {
      return Result<Unit, Exception>.Err(ex);
    }
  }

  /// <summary>
  /// Awaits a ValueTask and wraps exceptions in a Result.
  /// Optimized for completed ValueTasks to avoid allocation.
  /// </summary>
  public static async ValueTask<Result<T, Exception>> ToResult<T>(this ValueTask<T> task)
  {
    try
    {
      var result = await task;
      return Result<T, Exception>.Ok(result);
    }
    catch (Exception ex)
    {
      return Result<T, Exception>.Err(ex);
    }
  }

  /// <summary>
  /// Awaits a ValueTask and wraps exceptions in a Result.
  /// Optimized for completed ValueTasks to avoid allocation.
  /// </summary>
  public static async ValueTask<Result<Unit, Exception>> ToResult(this ValueTask task)
  {
    try
    {
      await task;
      return Result<Unit, Exception>.Ok(Unit.Value);
    }
    catch (Exception ex)
    {
      return Result<Unit, Exception>.Err(ex);
    }
  }

  /// <summary>
  /// Chains async Result operations.
  /// </summary>
  public static async Task<Result<U, E>> AndThenAsync<T, U, E>(this Result<T, E> result, Func<T, Task<Result<U, E>>> op)
  {
    if (result.IsErr)
      return Result<U, E>.Err(result.UnwrapErr());
    
    return await op(result.Unwrap());
  }

  /// <summary>
  /// Maps async operations over a Result.
  /// </summary>
  public static async Task<Result<U, E>> MapAsync<T, U, E>(this Result<T, E> result, Func<T, Task<U>> op)
  {
    if (result.IsErr)
      return Result<U, E>.Err(result.UnwrapErr());
    
    var mapped = await op(result.Unwrap());
    return Result<U, E>.Ok(mapped);
  }

  /// <summary>
  /// Chains async Result operations using ValueTask.
  /// Optimized for completed ValueTasks to avoid allocation.
  /// </summary>
  public static async ValueTask<Result<U, E>> AndThenAsync<T, U, E>(this Result<T, E> result, Func<T, ValueTask<Result<U, E>>> op)
  {
    if (result.IsErr)
      return Result<U, E>.Err(result.UnwrapErr());
    
    return await op(result.Unwrap());
  }

  /// <summary>
  /// Maps async operations over a Result using ValueTask.
  /// Optimized for completed ValueTasks to avoid allocation.
  /// </summary>
  public static async ValueTask<Result<U, E>> MapAsync<T, U, E>(this Result<T, E> result, Func<T, ValueTask<U>> op)
  {
    if (result.IsErr)
      return Result<U, E>.Err(result.UnwrapErr());
    
    var mapped = await op(result.Unwrap());
    return Result<U, E>.Ok(mapped);
  }

  /// <summary>
  /// Unity-specific: Safe GameObject component operations that might fail.
  /// </summary>
  public static Result<T, GameObjectError> GetComponentSafe<T>(this GameObject gameObject) where T : Component
  {
    var component = gameObject.GetComponent<T>();
    return component != null 
      ? Result<T, GameObjectError>.Ok(component)
      : Result<T, GameObjectError>.Err(GameObjectError.ComponentNotFound(gameObject.name, typeof(T)));
  }

  /// <summary>
  /// Unity-specific: Safe GameObject finding by name.
  /// </summary>
  public static Result<GameObject, GameObjectError> FindGameObjectSafe(string name)
  {
    var gameObject = GameObject.Find(name);
    return gameObject != null 
      ? Result<GameObject, GameObjectError>.Ok(gameObject)
      : Result<GameObject, GameObjectError>.Err(GameObjectError.NotFound(name));
  }

  /// <summary>
  /// Unity-specific: Safe Resources loading.
  /// </summary>
  public static Result<T, UnityAssetError> LoadSafe<T>(string path) where T : UnityEngine.Object
  {
    var resource = Resources.Load<T>(path);
    return resource != null 
      ? Result<T, UnityAssetError>.Ok(resource)
      : Result<T, UnityAssetError>.Err(UnityAssetError.NotFound(path, typeof(T)));
  }
}
}