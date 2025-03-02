#if UNITY_EDITOR
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

public class MapGenerator : EditorWindow
{
  // Generated File Details
  [SerializeField] private string sourceFolderPath = "Assets/Materials";
  [SerializeField] private int folderSearchDepth = 1;

  // Default values for missing textures
  private float defaultMetallicValue = 0f;
  private float defaultRoughnessValue = 0.5f;  // This will be inverted for smoothness
  private float defaultAOValue = 1f;
  private float defaultDetailMaskValue = 1f;

  // Texture set patterns
  [SerializeField] private string[] roughnessMetallicPatterns = new[] { "_roughness_metallic", "_rough_met", "_roughmet", "roughness_metallic" };
  [SerializeField] private string[] metallicPatterns = new[] { "_metallic", "_metal", "_met", "metallic" };
  [SerializeField] private string[] roughnessPatterns = new[] { "_roughness", "_rough", "_r", "roughness" };
  [SerializeField] private string[] emissivePatterns = new[] { "_emission", "_emissive"};
  [SerializeField] private string[] smoothnessPatterns = new[] { "_smoothness", "_smooth", "_s", "smoothness" };
  [SerializeField] private string[] aoPatterns = new[] { "_ao", "_ambient", "_occlusion", "ao", "ambientocclusion" };
  [SerializeField] private string[] detailMaskPatterns = new[] { "_detail", "_mask", "_dtl", "detail", "mask" };
  [SerializeField] private string[] maskMapPatterns = new[] { "_masks", "_maskmap", "mask_map", "maskmap" };

  // Base texture property names to check
  private readonly string[] baseTextureProperties = new[] { "_BaseMap", "_MainTex", "_BaseColor", "_BaseColorMap" };
  private string searchFilter = "";
  private bool filterBySelection = false;
  private HashSet<string> selectedObjectMaterialNames = new HashSet<string>();


  // Process status
  [SerializeField]
  private List<MaterialSet> materialsWithBaseMaps = new List<MaterialSet>();
  [SerializeField]
  private List<MaterialSet> materialsWithoutBaseMaps = new List<MaterialSet>();
  private bool showMaterialsWithoutBaseMaps = false;
  private bool allExpanded = true;
  private bool showOnlyWithMaskMaps = false;
  private Vector2 scrollPosition;

  // Metadata handling
  private const string metaFilename = "MapGeneratorExclusions.json";
  private string MetaFilePath => Path.Combine(Application.dataPath, "..", "ProjectSettings", metaFilename);

  [MenuItem("Tools/Map Generator")]
  public static void ShowWindow()
  {
    GetWindow<MapGenerator>("Map Generator");
  }

  [System.Serializable]
  private class MaterialSet
  {
    public string BaseName;
    public Material Material;
    public string MaterialGuid;
    public Texture2D BaseTexture;
    [SerializeField] public Texture2D RoughnessMetallicTexture;  // Combined roughness (R) and metallic (G) texture
    [SerializeField] public Texture2D EmissiveTexture;  // Combined roughness (R) and metallic (G) texture
    [SerializeField] public Texture2D MetallicTexture;  // Will be hidden if RoughnessMetallicTexture exists
    [SerializeField] public Texture2D RoughnessTexture;  // Will be hidden if RoughnessMetallicTexture exists
    [SerializeField] public Texture2D SmoothnessTexture; // Direct smoothness texture
    [SerializeField] public Texture2D AOTexture;
    [SerializeField] public Texture2D DetailMaskTexture;
    [SerializeField] public Texture2D MaskMapTexture;
    public bool IsProcessed;
    public string ProcessingStatus;
    public bool IsFoldedOut = true;
    public bool IsEditing = false;

    public bool HasMaskMap => MaskMapTexture != null;
    public bool HasCombinedRoughnessMetallic => RoughnessMetallicTexture != null;
  }

  void OnGUI()
  {
    GUILayout.Label("HDRP Material Mask Map Generator", EditorStyles.boldLabel);

    EditorGUILayout.HelpBox(
        "Generates HDRP mask maps with:\n" +
        "R: Metallic\n" +
        "G: Ambient Occlusion\n" +
        "B: Detail Mask\n" +
        "A: Smoothness (from smoothness map or inverted from roughness)",
        MessageType.Info);

    EditorGUILayout.Space();

    sourceFolderPath = EditorGUILayout.TextField("Source Folder", sourceFolderPath);
    folderSearchDepth = EditorGUILayout.IntSlider("Parent Folder Search Depth", folderSearchDepth, 0, 5);

    EditorGUILayout.Space();


    EditorGUILayout.LabelField("Default Values for Missing Textures", EditorStyles.boldLabel);
    defaultMetallicValue = EditorGUILayout.Slider("Default Metallic", defaultMetallicValue, 0f, 1f);
    defaultRoughnessValue = EditorGUILayout.Slider("Default Roughness", defaultRoughnessValue, 0f, 1f);
    defaultAOValue = EditorGUILayout.Slider("Default AO", defaultAOValue, 0f, 1f);
    defaultDetailMaskValue = EditorGUILayout.Slider("Default Detail Mask", defaultDetailMaskValue, 0f, 1f);

    EditorGUILayout.Space();

    using (new EditorGUILayout.HorizontalScope())
    {
      if (GUILayout.Button("Scan Folder for Materials"))
      {
        ScanFolder();
      }

      if (GUILayout.Button("Generate Filtered Materials"))
      {
        ProcessAllSets();
      }

      if (GUILayout.Button("Assign Found Mask Maps"))
      {
        AssignFoundMaskMaps();
      }
    }

    EditorGUILayout.Space();

    // Add selection filter UI
    using (new EditorGUILayout.HorizontalScope())
    {
      bool newFilterBySelection = EditorGUILayout.ToggleLeft(
          "Filter by Selected Objects",
          filterBySelection,
          GUILayout.Width(210)
      );

      if (newFilterBySelection != filterBySelection)
      {
        filterBySelection = newFilterBySelection;
        if (filterBySelection)
        {
          UpdateSelectedMaterials();
        }
      }

      if (filterBySelection)
      {
        EditorGUILayout.LabelField($"({selectedObjectMaterialNames.Count} materials in selection)", EditorStyles.miniLabel);
      }
    }
    DisplayMaterialSets();
  }

  private void OnSelectionChange()
  {
    if (filterBySelection)
    {
      UpdateSelectedMaterials();
      Repaint();
    }
  }

  private void UpdateSelectedMaterials()
  {
    selectedObjectMaterialNames.Clear();

    foreach (GameObject obj in Selection.gameObjects)
    {
      // Get all Renderer components (MeshRenderer, SkinnedMeshRenderer, etc.)
      var renderers = obj.GetComponentsInChildren<Renderer>(true);
      foreach (var renderer in renderers)
      {
        foreach (var material in renderer.sharedMaterials)
        {
          if (material != null)
          {
            selectedObjectMaterialNames.Add(material.name.Replace(" (Instance)", ""));
          }
        }
      }
    }
  }

  private void DisplayMaterialSets()
  {
    EditorGUILayout.Space();

    // Add search field at the top
    EditorGUILayout.BeginHorizontal();
    GUI.SetNextControlName("SearchField");
    searchFilter = EditorGUILayout.TextField("Search:", searchFilter, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
    if (GUILayout.Button("Clear", GUILayout.Width(50)))
    {
      searchFilter = "";
      GUI.FocusControl(null);
    }
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.Space();

    // Material Sets Header with controls
    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.BeginVertical();

    // Toggle to show materials without base maps
    bool newShowMaterialsWithoutBaseMaps = EditorGUILayout.ToggleLeft(
        "Show Materials Without Base Maps",
        showMaterialsWithoutBaseMaps,
        GUILayout.Width(260)
    );

    if (newShowMaterialsWithoutBaseMaps != showMaterialsWithoutBaseMaps)
    {
      showMaterialsWithoutBaseMaps = newShowMaterialsWithoutBaseMaps;
      GUI.changed = true;
    }

    // Show mask map filter
    bool newShowOnlyWithMaskMaps = EditorGUILayout.ToggleLeft(
        "Show Only With Mask Maps",
        showOnlyWithMaskMaps,
        GUILayout.Width(260)
    );

    if (newShowOnlyWithMaskMaps != showOnlyWithMaskMaps)
    {
      showOnlyWithMaskMaps = newShowOnlyWithMaskMaps;
      GUI.changed = true;
    }

    EditorGUILayout.EndVertical();

    GUILayout.FlexibleSpace();

    // Expand/Collapse All buttons
    if (GUILayout.Button("Expand All", GUILayout.Width(80)))
    {
      allExpanded = true;
      foreach (var set in materialsWithBaseMaps)
      {
        set.IsFoldedOut = true;
      }
      if (showMaterialsWithoutBaseMaps)
      {
        foreach (var set in materialsWithoutBaseMaps)
        {
          set.IsFoldedOut = true;
        }
      }
    }

    if (GUILayout.Button("Collapse All", GUILayout.Width(80)))
    {
      allExpanded = false;
      foreach (var set in materialsWithBaseMaps)
      {
        set.IsFoldedOut = false;
      }
      if (showMaterialsWithoutBaseMaps)
      {
        foreach (var set in materialsWithoutBaseMaps)
        {
          set.IsFoldedOut = false;
        }
      }
    }
    EditorGUILayout.EndHorizontal();

    // Get all materials
    var allMaterials = new List<MaterialSet>(materialsWithBaseMaps);
    if (showMaterialsWithoutBaseMaps)
    {
      allMaterials.AddRange(materialsWithoutBaseMaps);
    }

    EditorGUILayout.LabelField(
        $"Materials ({allMaterials.Count})" +
        (showMaterialsWithoutBaseMaps ? $" (Including {materialsWithoutBaseMaps.Count} without base maps)" : ""),
        EditorStyles.boldLabel);

    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

    // Apply mask map filter
    var filteredSets = showOnlyWithMaskMaps ?
        allMaterials.Where(s => s.HasMaskMap) :
        // allMaterials.Where(s => !s.HasMaskMap);
        allMaterials;

    // Apply selection filter if enabled
    if (filterBySelection && selectedObjectMaterialNames.Count > 0)
    {
      filteredSets = filteredSets.Where(s =>
          selectedObjectMaterialNames.Contains(s.BaseName) ||
          (s.Material != null && selectedObjectMaterialNames.Contains(s.Material.name))
      );
    }

    // Apply search filter
    if (!string.IsNullOrWhiteSpace(searchFilter))
    {
      string search = searchFilter.ToLower();
      filteredSets = filteredSets.Where(s =>
          s.BaseName.ToLower().Contains(search) ||
          (s.Material != null && s.Material.name.ToLower().Contains(search))
      );
    }

    // Display filtered results count
    int totalCount = allMaterials.Count();
    int filteredCount = filteredSets.Count();
    if (filteredCount < totalCount)
    {
      EditorGUILayout.LabelField($"Showing {filteredCount} of {totalCount} materials", EditorStyles.miniLabel);
    }

    foreach (var set in filteredSets.Reverse())
    {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);

      EditorGUILayout.BeginHorizontal();

      set.IsFoldedOut = EditorGUILayout.Foldout(
          set.IsFoldedOut,
          set.BaseName + (set.HasMaskMap ? " (Has Mask Map)" : ""),
          true
      );

      GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
      if (GUILayout.Button("Generate", GUILayout.Width(210), GUILayout.Height(20)))
      {
        try
        {
          ProcessMaterialSet(set);
          set.IsProcessed = true;
          set.ProcessingStatus = "Success";
        }
        catch (System.Exception e)
        {
          set.ProcessingStatus = $"Error: {e.Message}";
          Debug.LogError($"Error processing material {set.BaseName}: {e.Message}");
        }
        GUIUtility.ExitGUI();
      }
      GUI.backgroundColor = Color.white;

      EditorGUILayout.EndHorizontal();

      if (set.IsFoldedOut)
      {
        EditorGUI.indentLevel++;

        EditorGUILayout.ObjectField("Material", set.Material, typeof(Material), false);
        set.BaseTexture = (Texture2D)EditorGUILayout.ObjectField("Base Texture", set.BaseTexture, typeof(Texture2D), false);

        // Show combined roughness-metallic texture first
        set.RoughnessMetallicTexture = (Texture2D)EditorGUILayout.ObjectField("Roughness-Metallic Map", set.RoughnessMetallicTexture, typeof(Texture2D), false);

        // Only show individual metallic and roughness fields if no combined texture exists
        if (set.RoughnessMetallicTexture == null)
        {
          set.MetallicTexture = (Texture2D)EditorGUILayout.ObjectField("Metallic", set.MetallicTexture, typeof(Texture2D), false);

          // Only show roughness if no smoothness texture is assigned
          if (set.SmoothnessTexture == null)
          {
            set.RoughnessTexture = (Texture2D)EditorGUILayout.ObjectField("Roughness", set.RoughnessTexture, typeof(Texture2D), false);
          }
        }

        set.SmoothnessTexture = (Texture2D)EditorGUILayout.ObjectField("Smoothness", set.SmoothnessTexture, typeof(Texture2D), false);
        set.AOTexture = (Texture2D)EditorGUILayout.ObjectField("AO", set.AOTexture, typeof(Texture2D), false);
        set.DetailMaskTexture = (Texture2D)EditorGUILayout.ObjectField("Detail Mask", set.DetailMaskTexture, typeof(Texture2D), false);
        set.MaskMapTexture = (Texture2D)EditorGUILayout.ObjectField("Existing Mask Map", set.MaskMapTexture, typeof(Texture2D), false);

        EditorGUI.indentLevel--;
      }

      EditorGUILayout.EndVertical();
      EditorGUILayout.Space();
    }

    EditorGUILayout.EndScrollView();
  }

  private void ScanFolder()
  {
    materialsWithBaseMaps.Clear();
    materialsWithoutBaseMaps.Clear();

    if (!Directory.Exists(sourceFolderPath))
    {
      Debug.LogError($"Source folder does not exist: {sourceFolderPath}");
      return;
    }

    List<string> searchFolders = GetSearchFolders(sourceFolderPath);
    string[] materialGuids = AssetDatabase.FindAssets("t:material", new[] { sourceFolderPath });

    foreach (string materialGuid in materialGuids)
    {
      string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
      Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

      if (material == null) continue;

      Texture2D baseTexture = material.mainTexture != null ? material.mainTexture as Texture2D : null;
      MaterialSet set = new MaterialSet
      {
        BaseName = material.name,
        Material = material,
        MaterialGuid = materialGuid,
        BaseTexture = baseTexture
      };

      if (baseTexture != null)
      {
        string baseTexturePath = AssetDatabase.GetAssetPath(baseTexture);
        string baseTextureName = Path.GetFileNameWithoutExtension(baseTexturePath);

        Debug.Log($"\nProcessing material with base texture: {material.name}");
        Debug.Log($"Base texture path: {baseTexturePath}");
        Debug.Log($"Base texture name: {baseTextureName}");

        // Check for existing mask map first
        set.MaskMapTexture = FindTextureInFolders(baseTextureName, maskMapPatterns, searchFolders);
        Debug.Log($"Existing mask map found: {(set.MaskMapTexture != null ? "Yes" : "No")}");

        // Look for combined roughness-metallic texture first
        set.RoughnessMetallicTexture = FindTextureInFolders(baseTextureName, roughnessMetallicPatterns, searchFolders);
        Debug.Log($"Roughness-Metallic texture found: {(set.RoughnessMetallicTexture != null ? "Yes" : "No")}");

        // Only look for individual textures if no combined texture exists
        if (set.RoughnessMetallicTexture == null)
        {
          set.MetallicTexture = FindTextureInFolders(baseTextureName, metallicPatterns, searchFolders);
          Debug.Log($"Metallic texture found: {(set.MetallicTexture != null ? "Yes" : "No")}");

          // Check for smoothness first, then roughness if no smoothness exists
          set.SmoothnessTexture = FindTextureInFolders(baseTextureName, smoothnessPatterns, searchFolders);
          if (set.SmoothnessTexture == null)
          {
            set.RoughnessTexture = FindTextureInFolders(baseTextureName, roughnessPatterns, searchFolders);
          }
        }
        set.EmissiveTexture = FindTextureInFolders(baseTextureName, emissivePatterns, searchFolders);
        set.AOTexture = FindTextureInFolders(baseTextureName, aoPatterns, searchFolders);
        set.DetailMaskTexture = FindTextureInFolders(baseTextureName, detailMaskPatterns, searchFolders);

        materialsWithBaseMaps.Add(set);
      }
      else
      {
        Debug.Log($"Material without base texture found: {material.name}");
        materialsWithoutBaseMaps.Add(set);
      }
    }

    Debug.Log($"Found {materialsWithBaseMaps.Count} materials with base textures");
    Debug.Log($"Found {materialsWithoutBaseMaps.Count} materials without base textures");
  }

  private Texture2D GetBaseTexture(Material material)
  {
    foreach (var propertyName in baseTextureProperties)
    {
      if (material.HasProperty(propertyName))
      {
        var texture = material.GetTexture(propertyName) as Texture2D;
        if (texture != null)
        {
          return texture;
        }
      }
    }
    return null;
  }

  private List<string> GetSearchFolders(string basePath)
  {
    List<string> searchFolders = new List<string>();
    string currentPath = basePath;

    searchFolders.Add(currentPath);

    for (int i = 0; i < folderSearchDepth && currentPath.StartsWith("Assets/"); i++)
    {
      string parentPath = Path.GetDirectoryName(currentPath);
      if (string.IsNullOrEmpty(parentPath) || parentPath == "Assets") break;

      searchFolders.Add(parentPath);
      currentPath = parentPath;
    }

    return searchFolders;
  }

  private Texture2D FindTextureInFolders(string baseName, string[] patterns, List<string> searchFolders)
  {
    Debug.Log($"Searching for texture with base name: {baseName}");
    Debug.Log($"Using patterns: {string.Join(", ", patterns)}");

    string[] baseNameParts = baseName.ToLower().Split('_');
    string mainBaseName = string.Join("_", baseNameParts.Take(baseNameParts.Length - 1));

    string[] resolutionSuffixes = { "_1k", "_2k", "_4k", "_8k" };

    foreach (var folder in searchFolders)
    {
      Debug.Log($"Searching in folder: {folder}");
      string[] textureGuids = AssetDatabase.FindAssets("t:texture2D", new[] { folder });

      Debug.Log($"Found {textureGuids.Length} textures in folder");

      foreach (string guid in textureGuids)
      {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        string fileName = Path.GetFileNameWithoutExtension(path).ToLower();

        Debug.Log($"Checking texture: {fileName}");

        if (fileName.EndsWith("basecolor") || fileName.EndsWith("base_color"))
        {
          Debug.Log($"Skipping base color texture: {fileName}");
          continue;
        }

        if (!fileName.Contains(mainBaseName))
        {
          continue;
        }

        Debug.Log($"Found matching base name in: {fileName}");

        foreach (var resSuffix in resolutionSuffixes)
        {
          if (fileName.EndsWith(resSuffix))
          {
            fileName = fileName.Substring(0, fileName.Length - resSuffix.Length);
            Debug.Log($"Removed resolution suffix '{resSuffix}': {fileName}");
            break;
          }
        }

        foreach (var pattern in patterns)
        {
          if (fileName.EndsWith(pattern.ToLower()))
          {
            Debug.Log($"Found matching pattern '{pattern}' in: {fileName}");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
          }
        }
      }
    }

    Debug.Log($"No matching texture found for base name: {baseName}");
    return null;
  }

  private void ProcessAllSets()
  {
    // Get all materials
    var allMaterials = new List<MaterialSet>(materialsWithBaseMaps);
    if (showMaterialsWithoutBaseMaps)
    {
      allMaterials.AddRange(materialsWithoutBaseMaps);
    }

    // Apply mask map filter
    var filteredSets = showOnlyWithMaskMaps ?
        allMaterials.Where(s => s.HasMaskMap) :
        allMaterials.Where(s => !s.HasMaskMap);

    // Apply selection filter if enabled
    if (filterBySelection && selectedObjectMaterialNames.Count > 0)
    {
      filteredSets = filteredSets.Where(s =>
          selectedObjectMaterialNames.Contains(s.BaseName) ||
          (s.Material != null && selectedObjectMaterialNames.Contains(s.Material.name))
      );
    }

    // Apply search filter
    if (!string.IsNullOrWhiteSpace(searchFilter))
    {
      string search = searchFilter.ToLower();
      filteredSets = filteredSets.Where(s =>
          s.BaseName.ToLower().Contains(search) ||
          (s.Material != null && s.Material.name.ToLower().Contains(search))
      );
    }

    // Process each visible material set
    foreach (var set in filteredSets.Where(s => !s.HasMaskMap))
    {
      try
      {
        ProcessMaterialSet(set);
        set.IsProcessed = true;
        set.ProcessingStatus = "Success";
      }
      catch (System.Exception e)
      {
        set.ProcessingStatus = $"Error: {e.Message}";
        Debug.LogError($"Error processing material {set.BaseName}: {e.Message}");
      }
    }

    AssetDatabase.Refresh();
  }

  private void ProcessMaterialSet(MaterialSet set)
  {
    // if (set.HasMaskMap)
    // {
    //     Undo.RecordObject(set.Material, "Assign Mask Map");
    //     set.Material.SetTexture("_MaskMap", set.MaskMapTexture);
    //     EditorUtility.SetDirty(set.Material);
    //     Debug.Log($"Skipping generation {set.BaseName}: Already has mask map");
    //     return;
    // }

    List<string> modifiedTextures = new List<string>();
    bool settingsChanged = false;

    try
    {
      // Make all necessary textures readable
      if (set.RoughnessMetallicTexture != null)
        settingsChanged |= EnsureTextureIsReadable(set.RoughnessMetallicTexture, modifiedTextures);
      else
      {
        if (set.MetallicTexture != null)
          settingsChanged |= EnsureTextureIsReadable(set.MetallicTexture, modifiedTextures);
        if (set.RoughnessTexture != null && set.SmoothnessTexture == null)
          settingsChanged |= EnsureTextureIsReadable(set.RoughnessTexture, modifiedTextures);
      }

      if (set.SmoothnessTexture != null)
        settingsChanged |= EnsureTextureIsReadable(set.SmoothnessTexture, modifiedTextures);
      if (set.AOTexture != null)
        settingsChanged |= EnsureTextureIsReadable(set.AOTexture, modifiedTextures);
      if (set.DetailMaskTexture != null)
        settingsChanged |= EnsureTextureIsReadable(set.DetailMaskTexture, modifiedTextures);

      if (settingsChanged)
      {
        AssetDatabase.Refresh();
        Debug.Log("Updated Read/Write settings for textures: " + string.Join(", ", modifiedTextures));

        // Reload the textures
        if (set.RoughnessMetallicTexture != null)
          set.RoughnessMetallicTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(set.RoughnessMetallicTexture));
        else
        {
          if (set.MetallicTexture != null)
            set.MetallicTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(set.MetallicTexture));
          if (set.RoughnessTexture != null)
            set.RoughnessTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(set.RoughnessTexture));
        }
        if (set.SmoothnessTexture != null)
          set.SmoothnessTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(set.SmoothnessTexture));
        if (set.AOTexture != null)
          set.AOTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(set.AOTexture));
        if (set.DetailMaskTexture != null)
          set.DetailMaskTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(set.DetailMaskTexture));
      }

      // Find maximum dimensions
      int maxWidth = set.BaseTexture.width;
      int maxHeight = set.BaseTexture.height;

      if (set.RoughnessMetallicTexture != null)
      {
        maxWidth = Mathf.Max(maxWidth, set.RoughnessMetallicTexture.width);
        maxHeight = Mathf.Max(maxHeight, set.RoughnessMetallicTexture.height);
      }
      else
      {
        if (set.MetallicTexture != null)
        {
          maxWidth = Mathf.Max(maxWidth, set.MetallicTexture.width);
          maxHeight = Mathf.Max(maxHeight, set.MetallicTexture.height);
        }
        if (set.RoughnessTexture != null && set.SmoothnessTexture == null)
        {
          maxWidth = Mathf.Max(maxWidth, set.RoughnessTexture.width);
          maxHeight = Mathf.Max(maxHeight, set.RoughnessTexture.height);
        }
      }
      if (set.SmoothnessTexture != null)
      {
        maxWidth = Mathf.Max(maxWidth, set.SmoothnessTexture.width);
        maxHeight = Mathf.Max(maxHeight, set.SmoothnessTexture.height);
      }
      if (set.AOTexture != null)
      {
        maxWidth = Mathf.Max(maxWidth, set.AOTexture.width);
        maxHeight = Mathf.Max(maxHeight, set.AOTexture.height);
      }
      if (set.DetailMaskTexture != null)
      {
        maxWidth = Mathf.Max(maxWidth, set.DetailMaskTexture.width);
        maxHeight = Mathf.Max(maxHeight, set.DetailMaskTexture.height);
      }

      // Create or resize textures
      Texture2D metallicTexture;
      Texture2D smoothnessTexture;

      // Handle combined roughness-metallic texture
      if (set.RoughnessMetallicTexture != null)
      {
        Texture2D resizedRM = ResizeTexture(set.RoughnessMetallicTexture, maxWidth, maxHeight);
        metallicTexture = new Texture2D(maxWidth, maxHeight, TextureFormat.RGB24, false);
        smoothnessTexture = new Texture2D(maxWidth, maxHeight, TextureFormat.RGB24, false);

        // Extract metallic from G channel and roughness from R channel
        for (int y = 0; y < maxHeight; y++)
        {
          for (int x = 0; x < maxWidth; x++)
          {
            Color rmPixel = resizedRM.GetPixel(x, y);
            metallicTexture.SetPixel(x, y, new Color(rmPixel.g, rmPixel.g, rmPixel.g));
            smoothnessTexture.SetPixel(x, y, new Color(1f - rmPixel.r, 1f - rmPixel.r, 1f - rmPixel.r));
          }
        }
        metallicTexture.Apply();
        smoothnessTexture.Apply();

        if (resizedRM != set.RoughnessMetallicTexture)
          DestroyImmediate(resizedRM);
      }
      else
      {
        // Handle separate metallic texture
        metallicTexture = set.MetallicTexture != null ?
            ResizeTexture(set.MetallicTexture, maxWidth, maxHeight) :
            CreateSolidColorTexture(maxWidth, maxHeight, defaultMetallicValue);

        // Handle smoothness/roughness texture
        if (set.SmoothnessTexture != null)
        {
          smoothnessTexture = ResizeTexture(set.SmoothnessTexture, maxWidth, maxHeight);
        }
        else if (set.RoughnessTexture != null)
        {
          Texture2D roughnessTexture = ResizeTexture(set.RoughnessTexture, maxWidth, maxHeight);
          smoothnessTexture = new Texture2D(maxWidth, maxHeight, TextureFormat.RGB24, false);

          // Invert roughness to get smoothness
          for (int y = 0; y < maxHeight; y++)
          {
            for (int x = 0; x < maxWidth; x++)
            {
              float roughness = roughnessTexture.GetPixel(x, y).r;
              smoothnessTexture.SetPixel(x, y, new Color(1f - roughness, 1f - roughness, 1f - roughness));
            }
          }
          smoothnessTexture.Apply();

          if (roughnessTexture != set.RoughnessTexture)
            DestroyImmediate(roughnessTexture);
        }
        else
        {
          smoothnessTexture = CreateSolidColorTexture(maxWidth, maxHeight, 1f - defaultRoughnessValue);
        }
      }

      // Handle AO and detail mask textures
      Texture2D aoTexture = set.AOTexture != null ?
          ResizeTexture(set.AOTexture, maxWidth, maxHeight) :
          CreateSolidColorTexture(maxWidth, maxHeight, defaultAOValue);

      Texture2D detailMaskTexture = set.DetailMaskTexture != null ?
          ResizeTexture(set.DetailMaskTexture, maxWidth, maxHeight) :
          CreateSolidColorTexture(maxWidth, maxHeight, defaultDetailMaskValue);

      // Generate the HDRP mask map
      Texture2D maskMapTexture = new Texture2D(maxWidth, maxHeight, TextureFormat.RGBA32, false);

      for (int y = 0; y < maxHeight; y++)
      {
        for (int x = 0; x < maxWidth; x++)
        {
          Color maskColor = new Color(
              metallicTexture.GetPixel(x, y).r,      // Metallic in R channel
              aoTexture.GetPixel(x, y).r,            // AO in G channel
              detailMaskTexture.GetPixel(x, y).r,    // Detail Mask in B channel
              smoothnessTexture.GetPixel(x, y).r     // Smoothness in A channel
          );
          maskMapTexture.SetPixel(x, y, maskColor);
        }
      }

      maskMapTexture.Apply();

      // Save the mask map
      string maskMapPath = Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(set.BaseTexture)),
          $"{set.BaseName}_masks.png");
      byte[] pngData = maskMapTexture.EncodeToPNG();
      File.WriteAllBytes(maskMapPath, pngData);

      // Clean up temporary textures
      if (metallicTexture != set.MetallicTexture) DestroyImmediate(metallicTexture);
      if (smoothnessTexture != set.SmoothnessTexture) DestroyImmediate(smoothnessTexture);
      if (aoTexture != set.AOTexture) DestroyImmediate(aoTexture);
      if (detailMaskTexture != set.DetailMaskTexture) DestroyImmediate(detailMaskTexture);
      DestroyImmediate(maskMapTexture);

      AssetDatabase.Refresh();

      ConfigureMaskMapImportSettings(maskMapPath, maxWidth, maxHeight);

      // Load and assign the mask map
      Texture2D savedMaskMap = AssetDatabase.LoadAssetAtPath<Texture2D>(maskMapPath);
      set.MaskMapTexture = savedMaskMap;

      Undo.RecordObject(set.Material, "Assign Mask Map");
      set.Material.SetTexture("_MaskMap", savedMaskMap);
      EditorUtility.SetDirty(set.Material);

      Debug.Log($"Successfully generated HDRP mask map for {set.BaseName}");
    }
    finally
    {
      if (settingsChanged)
      {
        RestoreTextureImportSettings(modifiedTextures);
        AssetDatabase.Refresh();
      }
    }
  }
  
  private void AssignFoundMaskMaps()
  {
    // Get all materials
    var allMaterials = new List<MaterialSet>(materialsWithBaseMaps);
    if (showMaterialsWithoutBaseMaps)
    {
      allMaterials.AddRange(materialsWithoutBaseMaps);
    }

    // Apply mask map filter
    var filteredSets = showOnlyWithMaskMaps ?
        allMaterials.Where(s => s.HasMaskMap) :
        allMaterials;

    // Apply selection filter if enabled
    if (filterBySelection && selectedObjectMaterialNames.Count > 0)
    {
      filteredSets = filteredSets.Where(s =>
          selectedObjectMaterialNames.Contains(s.BaseName) ||
          (s.Material != null && selectedObjectMaterialNames.Contains(s.Material.name))
      );
    }

    // Apply search filter
    if (!string.IsNullOrWhiteSpace(searchFilter))
    {
      string search = searchFilter.ToLower();
      filteredSets = filteredSets.Where(s =>
          s.BaseName.ToLower().Contains(search) ||
          (s.Material != null && s.Material.name.ToLower().Contains(search))
      );
    }

    // Process each material set
    foreach (var set in filteredSets)
    {
      // If set has a MaskMapTexture and the material exists
      if (set.MaskMapTexture != null && set.Material != null)
      {
        var currentMaskMap = set.Material.GetTexture("_MaskMap");
        if (currentMaskMap != set.MaskMapTexture)
        {
          Undo.RecordObject(set.Material, "Assign Mask Map");
          set.Material.SetTexture("_MaskMap", set.MaskMapTexture);
          EditorUtility.SetDirty(set.Material);
          Debug.Log($"Assigned Mask Map to material {set.BaseName}");
        }
        else
        {
          Debug.Log($"Material {set.BaseName} already has the Mask Map assigned");
        }
      }
      else
      {
        Debug.Log($"No Mask Map found or material is missing for {set.BaseName}");
      }
    }

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
  }

  private bool EnsureTextureIsReadable(Texture2D texture, List<string> modifiedTextures)
  {
    string path = AssetDatabase.GetAssetPath(texture);
    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

    if (importer != null && !importer.isReadable)
    {
      Debug.Log($"Making texture readable: {path}");
      importer.isReadable = true;
      importer.SaveAndReimport();
      modifiedTextures.Add(path);
      return true;
    }

    return false;
  }

  private void RestoreTextureImportSettings(List<string> modifiedTextures)
  {
    foreach (string path in modifiedTextures)
    {
      TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
      if (importer != null)
      {
        importer.isReadable = false;
        importer.SaveAndReimport();
      }
    }
  }

  private void ConfigureMaskMapImportSettings(string maskMapPath, int maxWidth, int maxHeight)
  {
    TextureImporter importer = AssetImporter.GetAtPath(maskMapPath) as TextureImporter;
    if (importer != null)
    {
      importer.textureType = TextureImporterType.Default;
      importer.sRGBTexture = false;
      importer.isReadable = true;
      importer.maxTextureSize = Mathf.Max(maxWidth, maxHeight);
      importer.SaveAndReimport();
    }
  }

  private Texture2D CreateSolidColorTexture(int width, int height, float value)
  {
    Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
    Color color = new Color(value, value, value, value);
    Color[] colors = new Color[width * height];
    for (int i = 0; i < colors.Length; i++)
    {
      colors[i] = color;
    }
    texture.SetPixels(colors);
    texture.Apply();
    return texture;
  }

  private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
  {
    if (source.width == targetWidth && source.height == targetHeight)
      return source;

    RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0);
    RenderTexture.active = rt;

    Graphics.Blit(source, rt);

    Texture2D resized = new Texture2D(targetWidth, targetHeight);
    resized.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
    resized.Apply();

    RenderTexture.active = null;
    RenderTexture.ReleaseTemporary(rt);

    return resized;
  }
}
#endif