using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
public class CreatureAnimationSetup : EditorWindow
{
    private string walkCyclesPath = "Assets/Sprites/walk_cycles";
    private GameObject albertPrefab;
    private GameObject kaiPrefab;
    
    [MenuItem("UTMIST/Setup Creature Animations")]
    public static void ShowWindow()
    {
        GetWindow<CreatureAnimationSetup>("Creature Animation Setup");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Creature Animation Setup", EditorStyles.boldLabel);
        
        walkCyclesPath = EditorGUILayout.TextField("Walk Cycles Path", walkCyclesPath);
        
        EditorGUILayout.Space();
        GUILayout.Label("Creature Prefabs", EditorStyles.boldLabel);
        
        albertPrefab = (GameObject)EditorGUILayout.ObjectField("Albert Prefab", albertPrefab, typeof(GameObject), false);
        kaiPrefab = (GameObject)EditorGUILayout.ObjectField("Kai Prefab", kaiPrefab, typeof(GameObject), false);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Import All Walk Cycle Sprites"))
        {
            ImportWalkCycleSprites();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Setup Albert Prefab"))
        {
            if (albertPrefab != null)
            {
                SetupCreaturePrefab(albertPrefab, Creature.CreatureType.Albert);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign the Albert prefab first", "OK");
            }
        }
        
        if (GUILayout.Button("Setup Kai Prefab"))
        {
            if (kaiPrefab != null)
            {
                SetupCreaturePrefab(kaiPrefab, Creature.CreatureType.Kai);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign the Kai prefab first", "OK");
            }
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Setup Both Prefabs"))
        {
            if (albertPrefab != null && kaiPrefab != null)
            {
                SetupCreaturePrefab(albertPrefab, Creature.CreatureType.Albert);
                SetupCreaturePrefab(kaiPrefab, Creature.CreatureType.Kai);
                EditorUtility.DisplayDialog("Success", "Both prefabs have been set up successfully!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign both prefabs first", "OK");
            }
        }
    }
    
    private void ImportWalkCycleSprites()
    {
        // Ensure walk cycles directory exists
        if (!Directory.Exists(walkCyclesPath))
        {
            EditorUtility.DisplayDialog("Error", $"Path not found: {walkCyclesPath}", "OK");
            return;
        }
        
        // Get all subdirectories
        string[] dirs = Directory.GetDirectories(walkCyclesPath);
        
        if (dirs.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No subdirectories found in walk cycles path", "OK");
            return;
        }
        
        int totalSprites = 0;
        
        // Import sprites from each direction folder
        foreach (string dir in dirs)
        {
            string[] pngFiles = Directory.GetFiles(dir, "*.png");
            
            foreach (string pngFile in pngFiles)
            {
                string assetPath = pngFile.Replace("\\", "/");
                
                // Check if the texture import settings need to be updated
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                
                if (importer != null)
                {
                    // Set texture as sprite
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 100;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Point; // Pixel perfect
                    
                    // Apply changes and reimport
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    
                    totalSprites++;
                }
            }
        }
        
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Import Complete", $"Successfully imported {totalSprites} sprites!", "OK");
    }
    
    private void SetupCreaturePrefab(GameObject prefab, Creature.CreatureType creatureType)
    {
        // Open the prefab for editing
        GameObject prefabInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        
        if (prefabInstance == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to instantiate prefab", "OK");
            return;
        }
        
        // Add CreatureAnimator component if not already present
        CreatureAnimator animator = prefabInstance.GetComponent<CreatureAnimator>();
        if (animator == null)
        {
            animator = prefabInstance.AddComponent<CreatureAnimator>();
        }
        
        // Get SpriteRenderer
        SpriteRenderer spriteRenderer = prefabInstance.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = prefabInstance.AddComponent<SpriteRenderer>();
        }
        
        // Connect SpriteRenderer to animator
        animator.spriteRenderer = spriteRenderer;
        
        // Load sprites for the creature
        string typeStr = creatureType == Creature.CreatureType.Albert ? "Albert" : "Kai";
        
        // Use CreatureSpriteLoader to load all sprites
        CreatureSpriteLoader loader = prefabInstance.AddComponent<CreatureSpriteLoader>();
        loader.creatureAnimator = animator;
        loader.walkCyclesBasePath = walkCyclesPath;
        loader.LoadAllSprites();
        
        // Set the creature type
        Creature creature = prefabInstance.GetComponent<Creature>();
        if (creature != null)
        {
            creature.creatureType = creatureType;
        }
        
        // Apply changes back to the prefab
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabInstance));
        DestroyImmediate(prefabInstance);
        
        EditorUtility.DisplayDialog("Success", $"{typeStr} prefab has been set up successfully!", "OK");
    }
}
#endif 