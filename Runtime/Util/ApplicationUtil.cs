using System;
using System.Collections;
using UnityEngine;

public static class ApplicationUtil
{
  public static IEnumerator HasAuthorization(UserAuthorization auth, Action<bool> callback)
  {
    yield return Application.RequestUserAuthorization(auth);
    if (Application.HasUserAuthorization(auth))
    {
      callback(true);
    }
    else
    {
      callback(false);
    }
  }
}
