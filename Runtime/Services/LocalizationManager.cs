using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// The non-generic base class that provides the global singleton instance
/// and defines the public API contract for easy access from any script.
/// </summary>
public abstract class ILocalizationManager<TEnum> : MonoBehaviour
{
  /// <summary>
  /// A non-generic event that fires when the language changes, providing the new locale string.
  /// </summary>
  public event Action<TEnum> OnLanguageChanged;
  /// <summary>
  /// Helper method for child classes to raise the non-generic event.
  /// </summary>
  protected void RaiseOnLanguageChanged(TEnum language) => OnLanguageChanged?.Invoke(language);

  #region Abstract Non-Generic API
  public abstract string CurrentLocale { get; }
  public abstract TEnum CurrentLanguage { get; }
  public abstract string CurrentLanguageReadable { get; }
  public abstract List<string> SupportedLocales { get; }
  public abstract List<string> SupportedLanguagesReadable { get; }
  public abstract void SetLanguage(string locale);
  public abstract void SetSupportedLanguages(TEnum defaultLang, IEnumerable<TEnum> supportedLocales);
  public abstract bool ContainsCurrentLanguage(string locales);
  #endregion

  protected abstract string LoadLocale();
  protected abstract void SaveLocale(string locale);
}

/// <summary>
/// The generic class containing all localization logic. It initializes itself based on the
/// provided enum type 'TEnum' and exposes a complete API for language management.
/// </summary>
public class LocalizationManagerBase<TEnum> : ILocalizationManager<TEnum> where TEnum : Enum
{
  // --- Private Fields ---
  private readonly Dictionary<TEnum, string> _enumToReadable = new Dictionary<TEnum, string>();
  private readonly Dictionary<string, TEnum> _localeToEnum = new Dictionary<string, TEnum>();
  private readonly Dictionary<string, TEnum> _readableNameToEnum = new Dictionary<string, TEnum>();
  protected TEnum defaultLanguage;
  private Dictionary<string, string> _installedLanguages = new Dictionary<string, string>();
  private Dictionary<string, string> _activeLanguageDefinitions = new Dictionary<string, string>();
  private static readonly string _PlayerPrefsLocaleKey = "LocalizationManager::_PlayerPrefsLocaleKey";
  private bool _isInitialized = false;

  protected override string LoadLocale() => PlayerPrefs.GetString(_PlayerPrefsLocaleKey, defaultLanguage.ToString());
  protected override void SaveLocale(string locale) => PlayerPrefs.SetString(_PlayerPrefsLocaleKey, locale);

  #region Initialization

  /// <summary>
  /// Discovers all possible languages from the enum via reflection.
  /// This should only run once.
  /// </summary>
  private void InitializeAvailableLanguages()
  {
    if (_isInitialized) return;

    var languageType = typeof(TEnum);
    var values = Enum.GetValues(languageType);
    _installedLanguages = new Dictionary<string, string>();
    allLanguages = new List<TEnum>();

    foreach (var value in values)
    {
      var enumValue = (TEnum)value;
      allLanguages.Add(enumValue);

      var field = languageType.GetField(value.ToString());
      var descriptionAttribute = field.GetCustomAttribute<DescriptionAttribute>();

      if (descriptionAttribute != null)
      {
        var readable = descriptionAttribute.Description;
        var locale = value.ToString();

        _enumToReadable[enumValue] = readable;
        _localeToEnum[locale] = enumValue;
        _readableNameToEnum[readable] = enumValue;
        _installedLanguages[locale] = readable;
      }
      else
      {
        Debug.LogError($"All Language Enum values must have a Description attribute. e.g. [Description(\"English (UK)\")] on value {value}");
      }
    }

    _isInitialized = true;
    Debug.Log($"LocalizationManager discovered {_installedLanguages.Count} total languages.");
  }
  #endregion

  #region Public API

  // --- Properties ---
  public List<TEnum> allLanguages { get; private set; } = new List<TEnum>();
  public override TEnum CurrentLanguage => LocaleAsLanguage(LoadLocale());
  public override string CurrentLocale => LoadLocale();
  public override string CurrentLanguageReadable => LanguageAsReadable(CurrentLanguage);
  public override List<string> SupportedLocales => _activeLanguageDefinitions.Keys.ToList();
  public override List<string> SupportedLanguagesReadable => _activeLanguageDefinitions.Values.ToList();

  // --- Language Conversion ---
  public string LanguageAsReadable(TEnum lang) => _enumToReadable.TryGetValue(lang, out var readable) ? readable : null;
  public TEnum ReadableAsLanguage(string langName) => _readableNameToEnum.TryGetValue(langName, out var lang) ? lang : default;
  public TEnum LocaleAsLanguage(string locale) => _localeToEnum.TryGetValue(locale, out var lang) ? lang : default;
  public string LocaleAsReadable(string locale) => LanguageAsReadable(LocaleAsLanguage(locale));

  // --- Language Management ---
  public override void SetSupportedLanguages(TEnum defaultLang, IEnumerable<TEnum> supportedLanguages)
  {
    // Ensure initialization has run (it should have from Awake, but this is a safeguard).
    if (!_isInitialized)
    {
      InitializeAvailableLanguages();
    }

    defaultLanguage = defaultLang;
    var newSupported = new Dictionary<string, string>();

    // Use a HashSet to automatically handle duplicates like `Language.DE` in your Main.cs
    var supportedLanguageSet = new HashSet<TEnum>(supportedLanguages);

    foreach (var langEnum in supportedLanguageSet)
    {
      string locale = GetLocale(langEnum);
      if (_installedLanguages.TryGetValue(locale, out var readableName))
      {
        newSupported.Add(locale, readableName);
      }
    }

    _activeLanguageDefinitions = newSupported;

    // If the currently saved language isn't in the new list, switch to the default.
    string currentLocale = LoadLocale();
    if (!_activeLanguageDefinitions.ContainsKey(currentLocale))
    {
      SetLanguage(defaultLanguage);
    }
    else
    {
      // If the language is still valid, raise the event anyway to notify listeners
      // (like a UI dropdown) that the list of supported languages has been updated.
      RaiseOnLanguageChanged(LocaleAsLanguage(currentLocale));
    }
  }

  public void SetLanguage(TEnum lang) => SetLanguageByLocale(GetLocale(lang));

  public override void SetLanguage(string langIdentifier)
  {
    if (string.IsNullOrEmpty(langIdentifier)) return;

    // First, try to match by locale (e.g., "EN_GB")
    if (_localeToEnum.ContainsKey(langIdentifier))
    {
      SetLanguageByLocale(langIdentifier);
      return;
    }

    // Next, try to match by readable name (e.g., "English (UK)")
    if (_readableNameToEnum.TryGetValue(langIdentifier, out TEnum lang))
    {
      SetLanguage(lang);
      return;
    }

    Debug.LogError($"Could not set language. Identifier '{langIdentifier}' is not a valid locale or readable name.");
  }

  public override bool ContainsCurrentLanguage(string locales)
  {
    string currentLocale = LoadLocale().ToLower();
    if (locales.Contains("!"))
    {
      return !locales.Contains("!" + currentLocale);
    }
    return locales.Contains(currentLocale);
  }

  // --- Internal Logic ---
  private string GetLocale(TEnum language) => language.ToString();

  private void SetLanguageByLocale(string locale)
  {
    // Fallback to default language if the provided locale is not supported.
    if (string.IsNullOrEmpty(locale) || !_activeLanguageDefinitions.ContainsKey(locale))
    {
      Debug.LogWarning($"Locale '{locale}' is not supported. Falling back to default language '{defaultLanguage}'.");
      locale = GetLocale(defaultLanguage);

      // Edge case: if even the default language is not in the supported list, we can't proceed.
      if (!_activeLanguageDefinitions.ContainsKey(locale))
      {
        Debug.LogError($"The default language '{locale}' is not in the supported list. Cannot set a language.");
        return;
      }
    }

    SaveLocale(locale);
    RaiseOnLanguageChanged(LocaleAsLanguage(locale));
  }
  #endregion
}

// Define your languages and decorate them with their details.
public enum ExampleLanguage
{
  [Description("English (UK)")]
  EN_GB,   // English (United Kingdom)
  [Description("Español")]
  ES,      // Spanish
  [Description("Deutsch")]
  DE,      // German
}

public class ExampleLocaleManager : MonoSingleton<LocalizationManagerBase<ExampleLanguage>> { }