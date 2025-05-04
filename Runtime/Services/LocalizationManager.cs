using System;
using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : MonoSingleton<LocalizationManager>
{
  public static event Action<Language> OnLanguageChanged;

  public enum Language
  {
    EN_GB = 0,
    EN_US = 1,
    ES = 2,
    DE = 3,
    FI = 4,
    NULL = -1,
  }

  public static List<Language> _allLanguages;
  public static List<Language> AllLanguages
  {
    get
    {
      if (_allLanguages == null)
      {
        _allLanguages = new List<Language>();
        foreach (var language in Enum.GetValues(typeof(Language)))
        {
          _allLanguages.Add((Language)language);
        }
      }
      return _allLanguages;
    }
  }
  public static List<string> _allLanguageLocales;
  private static List<string> AllLanguageLocales
  {
    get
    {
      if (_allLanguageLocales == null)
      {
        _allLanguageLocales = new List<string>();
        foreach (var language in Enum.GetValues(typeof(Language)))
        {
          _allLanguageLocales.Add(language.ToString().ToLower());
        }
      }
      return _allLanguageLocales;
    }
  }

  public static string LanguageAsReadable(Language lang)
  {
    switch (lang)
    {
      case Language.EN_GB:
        return "English UK";
      case Language.EN_US:
        return "English US";
      case Language.ES:
        return "Español";
      case Language.DE:
        return "Deutsch";
      case Language.FI:
        return "Suomi";
      default:
        Debug.LogWarning($"LocalizationManager::GetReadableLanguage: Invalid language set: {CurrentLanguage}");
        return "NULL";
    }
  }

  private static readonly List<Language> _SupportedLanguages = new List<Language>();
  private static readonly List<string> _SupportedLanguageLocales = new List<string>();

  private static readonly string _PlayerPrefsLocaleKey = "LocalizationManager::_PlayerPrefsLocaleKey";

  private new void Awake()
  {
    base.Awake();
    if (_SupportedLanguages.Count == 0)
    {
      SetSupportedLanguages(AllLanguages);
    }
  }

  public static void SetSupportedLanguages(List<Language> languages)
  {
    _SupportedLanguages.Clear();
    _SupportedLanguages.AddRange(languages);
    _SupportedLanguageLocales.Clear();
    foreach (var lang in languages)
    {
      _SupportedLanguageLocales.Add(lang.ToString());
    }
    if (!PlayerPrefs.HasKey(_PlayerPrefsLocaleKey))
    {
      PlayerPrefs.SetString(_PlayerPrefsLocaleKey, languages[0].ToString());
    }
  }

  public static void SetLanguage(Language lang)
  {
    PlayerPrefs.SetString(_PlayerPrefsLocaleKey, lang.ToString());
    Debug.Log($"LocalizationManager::SetLanguage: Language set to {lang}");
    OnLanguageChanged?.Invoke(lang);
  }

  public static void SetLanguage(string lang)
  {
    var newLang = LocaleAsLanguage(lang);
    if (newLang != Language.NULL)
    {
      SetLanguage(newLang);
      return;
    }
    newLang = ReadableAsLanguage(lang);
    if (newLang != Language.NULL)
    {
      SetLanguage(newLang);
      return;
    }
    Debug.LogError($"LocalizationManager::SetLanguage: Invalid language identifier {lang}");
  }

  private static string GetLocale()
  {
    if (_SupportedLanguageLocales == null || _SupportedLanguageLocales.Count == 0)
    {
      Debug.LogWarning("LocalizationManager::GetLocale: No supported languages set");
      return string.Empty;
    }

    var locale = PlayerPrefs.GetString(_PlayerPrefsLocaleKey);
    // Migrate old locale identifier for UK English
    if (locale == "EN")
    {
      locale = "EN_GB";
      PlayerPrefs.SetString(_PlayerPrefsLocaleKey, locale);
      PlayerPrefs.Save();
    }
    // Ensure locale is set to a supported language
    if (locale == string.Empty || !_SupportedLanguageLocales.Contains(locale))
    {
      Debug.LogWarning($"LocalizationManager::GetLocale: Invalid locale {locale}, defaulting to {_SupportedLanguageLocales[0]}");
      // Set to first supported language
      locale = _SupportedLanguageLocales[0];
      PlayerPrefs.SetString(_PlayerPrefsLocaleKey, locale);
      PlayerPrefs.Save();
    }
    return locale;
  }

  public static Language CurrentLanguage => LocaleAsLanguage(GetLocale());

  public static string CurrentLanguageReadable => LanguageAsReadable(CurrentLanguage);

  public static Language ReadableAsLanguage(string lang)
  {
    foreach (var l in Enum.GetValues(typeof(Language)))
    {
      if (LanguageAsReadable((Language)l) == lang)
      {
        return (Language)l;
      }
    }
    return Language.NULL;
  }

  public static Language LocaleAsLanguage(string locale)
  {
    if (Enum.TryParse<Language>(locale.ToUpper(), out var lang))
    {
      return lang;
    }
    return Language.NULL;
  }

  public static string LocaleAsReadable(string locale) => LanguageAsReadable(LocaleAsLanguage(locale));

  public static List<string> SupportedLanguagesReadable
  {
    get
    {
      var languages = new List<string>();
      foreach (Language lang in _SupportedLanguages)
      {
        languages.Add(LanguageAsReadable(lang));
      }
      Debug.Log($"LocalizationManager::AvailableLanguages: " + string.Join(", ", languages));
      return languages;
    }
  }

  public static bool ValidateLocalesString(string locales)
  {
    locales = locales.ToLower();
    int count = 0;
    // bool hasExclusion = locales.Contains("!");
    foreach (var locale in AllLanguageLocales)
    {
      if (locales.Contains(locale.ToLower()))
      {
        count++;
      }
      // if (hasExclusion && count > 1)
      // {
      //   Debug.LogError($"LocalizationManager::ValidateLocalesString: Invalid locales string: {locales}, exclusion supports a single locale");
      //   return false;
      // }
    }

    return true;
  }

  public static bool ContainsCurrentLanguage(string locales)
  {
    _ = ValidateLocalesString(locales);
    string currentLocale = GetLocale().ToLower();
    if (locales.Contains("!"))
    {
      return !locales.Contains("!" + currentLocale);
    }
    return locales.Contains(currentLocale);
  }
}
