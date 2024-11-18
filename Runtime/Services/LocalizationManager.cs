
using System;
using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : MonoSingleton<LocalizationManager>
{
  public static event Action<Language> OnLanguageChanged;

  public enum Language
  {
    EN,
    ES
  }

  private static readonly List<Language> _SupportedLanguages = new List<Language>();

  private static readonly string _PlayerPrefsLocaleKey = "LocalizationManager::_PlayerPrefsLocaleKey";

  public static void SetSupportedLanguages(List<Language> languages)
  {
    _SupportedLanguages.Clear();
    _SupportedLanguages.AddRange(languages);
  }

  public static void SetLanguage(Language lang)
  {
    PlayerPrefs.SetString(_PlayerPrefsLocaleKey, lang.ToString());
    Debug.Log($"LocalisationManager::SetLanguage: Language set to {lang}");
    OnLanguageChanged?.Invoke(lang);
  }

  public static void SetLanguage(string lang)
  {
    switch (lang.ToLower())
    {
      case "en":
      case "english":
        SetLanguage(Language.EN);
        break;
      case "es":
      case "español":
        SetLanguage(Language.ES);
        break;
      default:
        Debug.LogWarning($"LocalisationManager::SetLanguage: Invalid language set: {lang}");
        return;
    }
    OnLanguageChanged?.Invoke(CurrentLanguage);
  }

  private static string GetLocale() => PlayerPrefs.GetString(_PlayerPrefsLocaleKey, "EN");

  public static Language CurrentLanguage
  {
    get
    {
      switch (GetLocale())
      {
        case "EN":
          return Language.EN;
        case "ES":
          return Language.ES;
        default:
          Debug.LogWarning($"LocalisationManager::CurrentLanguage: Invalid language set to PlayerPrefs: {GetLocale()}");
          return Language.EN;
      }
    }
  }

  public static string CurrentLanguageReadable => AsReadable(CurrentLanguage);

  private static string AsReadable(Language lang)
  {
    switch (lang)
    {
      case Language.EN:
        return "English";
      case Language.ES:
        return "Español";
      default:
        Debug.LogWarning($"LocalisationManager::GetReadableLanguage: Invalid language set: {CurrentLanguage}");
        return "English";
    }
  }

  public static List<string> SupportedLanguages
  {
    get
    {
      var languages = new List<string>();
      foreach (Language lang in _SupportedLanguages)
      {
        languages.Add(AsReadable(lang));
      }
      Debug.Log($"LocalisationManager::AvailableLanguages: " + string.Join(", ", languages));
      return languages;
    }
  }

  public static bool IsCurrentLanguage(string lang)
  {
    switch (lang.ToLower())
    {
      case "en":
      case "english":
        return CurrentLanguage == Language.EN;
      case "es":
      case "español":
        return CurrentLanguage == Language.ES;
      default:
        Debug.LogWarning($"LocalisationManager::IsLanguage: Invalid language set: {lang}");
        return false;
    }
  }
}
