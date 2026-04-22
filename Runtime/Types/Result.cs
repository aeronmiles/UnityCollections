using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityCollections.Functional
{
  /// <summary>
  /// A type that represents either success (Ok) or failure (Err), inspired by Rust's Result&lt;T, E&gt;.
  /// This type forces explicit error handling and eliminates null reference exceptions.
  /// </summary>
  /// <typeparam name="T">The type of the success value</typeparam>
  /// <typeparam name="E">The type of the error value</typeparam>
  [Serializable]
  public readonly struct Result<T, E> : IEquatable<Result<T, E>>
{
  private readonly bool _isOk;
  private readonly T _value;
  private readonly E _error;

  private Result(T value)
  {
    _isOk = true;
    _value = value;
    _error = default(E);
  }

  private Result(E error)
  {
    _isOk = false;
    _value = default(T);
    _error = error;
  }

  /// <summary>Creates a success Result containing the given value.</summary>
  public static Result<T, E> Ok(T value) => new Result<T, E>(value);

  /// <summary>Creates an error Result containing the given error.</summary>
  public static Result<T, E> Err(E error) => new Result<T, E>(error);

  /// <summary>Returns true if the result is Ok.</summary>
  public bool IsOk => _isOk;

  /// <summary>Returns true if the result is Err.</summary>
  public bool IsErr => !_isOk;

  /// <summary>
  /// Returns the contained Ok value, consuming the Result.
  /// Throws an exception if the Result is Err.
  /// </summary>
  public T Unwrap()
  {
    if (!_isOk)
      throw new InvalidOperationException($"Called Unwrap on an Err value: {_error}");
    return _value;
  }

  /// <summary>
  /// Returns the contained Err value, consuming the Result.
  /// Throws an exception if the Result is Ok.
  /// </summary>
  public E UnwrapErr()
  {
    if (_isOk)
      throw new InvalidOperationException($"Called UnwrapErr on an Ok value: {_value}");
    return _error;
  }

  /// <summary>
  /// Returns the contained Ok value or a provided default.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public T UnwrapOr(T defaultValue) => _isOk ? _value : defaultValue;

  /// <summary>
  /// Returns the contained Ok value or computes it from a function.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public T UnwrapOrElse(Func<E, T> op) => _isOk ? _value : op(_error);

  /// <summary>
  /// Maps a Result&lt;T, E&gt; to Result&lt;U, E&gt; by applying a function to the contained Ok value.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Result<U, E> Map<U>(Func<T, U> op)
  {
    return _isOk ? Result<U, E>.Ok(op(_value)) : Result<U, E>.Err(_error);
  }

  /// <summary>
  /// Maps a Result&lt;T, E&gt; to U by applying a function to the contained Ok value, or returns a default.
  /// </summary>
  public U MapOr<U>(U defaultValue, Func<T, U> op)
  {
    return _isOk ? op(_value) : defaultValue;
  }

  /// <summary>
  /// Maps a Result&lt;T, E&gt; to U by applying a function to the contained Ok value, or computes a default.
  /// </summary>
  public U MapOrElse<U>(Func<E, U> defaultOp, Func<T, U> op)
  {
    return _isOk ? op(_value) : defaultOp(_error);
  }

  /// <summary>
  /// Maps a Result&lt;T, E&gt; to Result&lt;T, F&gt; by applying a function to the contained Err value.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Result<T, F> MapErr<F>(Func<E, F> op)
  {
    return _isOk ? Result<T, F>.Ok(_value) : Result<T, F>.Err(op(_error));
  }

  /// <summary>
  /// Returns the other result if this is Ok, otherwise returns this Err.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Result<U, E> And<U>(Result<U, E> other)
  {
    return _isOk ? other : Result<U, E>.Err(_error);
  }

  /// <summary>
  /// Calls the provided function with the contained Ok value and returns the result,
  /// or returns this Err.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Result<U, E> AndThen<U>(Func<T, Result<U, E>> op)
  {
    return _isOk ? op(_value) : Result<U, E>.Err(_error);
  }

  /// <summary>
  /// Returns this result if it's Ok, otherwise returns the other result.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Result<T, F> Or<F>(Result<T, F> other)
  {
    return _isOk ? Result<T, F>.Ok(_value) : other;
  }

  /// <summary>
  /// Returns this result if it's Ok, otherwise calls the provided function with the Err value.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Result<T, F> OrElse<F>(Func<E, Result<T, F>> op)
  {
    return _isOk ? Result<T, F>.Ok(_value) : op(_error);
  }

  /// <summary>
  /// Tries to get the Ok value if this Result is Ok.
  /// </summary>
  public bool TryGetValue(out T value)
  {
    if (_isOk)
    {
      value = _value;
      return true;
    }
    value = default(T);
    return false;
  }

  /// <summary>
  /// Tries to get the Err value if this Result is Err.
  /// </summary>
  public bool TryGetError(out E error)
  {
    if (!_isOk)
    {
      error = _error;
      return true;
    }
    error = default(E);
    return false;
  }

  /// <summary>
  /// Pattern matching support - executes onOk if Ok, onErr if Err.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public U Match<U>(Func<T, U> onOk, Func<E, U> onErr)
  {
    return _isOk ? onOk(_value) : onErr(_error);
  }

  /// <summary>
  /// Pattern matching support - executes onOk if Ok, onErr if Err.
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Match(Action<T> onOk, Action<E> onErr)
  {
    if (_isOk)
      onOk(_value);
    else
      onErr(_error);
  }

  public bool Equals(Result<T, E> other)
  {
    if (_isOk != other._isOk) return false;
    if (_isOk)
      return EqualityComparer<T>.Default.Equals(_value, other._value);
    else
      return EqualityComparer<E>.Default.Equals(_error, other._error);
  }

  public override bool Equals(object obj)
  {
    return obj is Result<T, E> other && Equals(other);
  }

  public override int GetHashCode()
  {
    return _isOk 
      ? HashCode.Combine(true, EqualityComparer<T>.Default.GetHashCode(_value))
      : HashCode.Combine(false, EqualityComparer<E>.Default.GetHashCode(_error));
  }

  public static bool operator ==(Result<T, E> left, Result<T, E> right)
  {
    return left.Equals(right);
  }

  public static bool operator !=(Result<T, E> left, Result<T, E> right)
  {
    return !left.Equals(right);
  }

  public override string ToString()
  {
    if (_isOk)
      return $"Ok({_value})";
    else
      return $"Err({_error})";
  }

  /// <summary>Implicit conversion from T to Result&lt;T, E&gt;.Ok</summary>
  public static implicit operator Result<T, E>(T value) => Ok(value);
}

/// <summary>
/// Non-generic helper methods for Result creation and operations.
/// </summary>
public static class Result
{
  /// <summary>Creates a success Result containing the given value.</summary>
  public static Result<T, E> Ok<T, E>(T value) => Result<T, E>.Ok(value);

  /// <summary>Creates an error Result containing the given error.</summary>
  public static Result<T, E> Err<T, E>(E error) => Result<T, E>.Err(error);

  /// <summary>
  /// Tries to execute the given function, returning Ok with the result or Err with the exception.
  /// </summary>
  public static Result<T, Exception> Try<T>(Func<T> operation)
  {
    try
    {
      return Ok<T, Exception>(operation());
    }
    catch (Exception ex)
    {
      return Err<T, Exception>(ex);
    }
  }

  /// <summary>
  /// Tries to execute the given action, returning Ok with Unit or Err with the exception.
  /// </summary>
  public static Result<Unit, Exception> Try(Action operation)
  {
    try
    {
      operation();
      return Ok<Unit, Exception>(Unit.Value);
    }
    catch (Exception ex)
    {
      return Err<Unit, Exception>(ex);
    }
  }
}

/// <summary>
/// Unit type for representing void operations in Result context.
/// </summary>
[Serializable]
public readonly struct Unit : IEquatable<Unit>
{
  public static readonly Unit Value = new Unit();

  public bool Equals(Unit other) => true;
  public override bool Equals(object obj) => obj is Unit;
  public override int GetHashCode() => 0;
  public override string ToString() => "()";

  public static bool operator ==(Unit left, Unit right) => true;
  public static bool operator !=(Unit left, Unit right) => false;
}
}