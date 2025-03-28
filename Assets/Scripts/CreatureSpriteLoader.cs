using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEditor;

#if UNITY_EDITOR
public class CreatureSpriteLoader : MonoBehaviour
{
    public CreatureAnimator creatureAnimator;
    
    [Header("Sprite Folders")]
    public string walkCyclesBasePath = "Assets/Sprites/walk_cycles";
    
    public void LoadAllSprites()
    {
        if (creatureAnimator == null)
        {
            Debug.LogError("CreatureAnimator reference is missing!");
            return;
        }
        
        // Load Albert sprites
        creatureAnimator.albertTopRightSprites = LoadSpritesFromFolder(Path.Combine(walkCyclesBasePath, "Albert top right walk"));
        creatureAnimator.albertTopLeftSprites = LoadSpritesFromFolder(Path.Combine(walkCyclesBasePath, "Albert top left walk"));
        creatureAnimator.albertBottomRightSprites = LoadSpritesFromFolder(Path.Combine(walkCyclesBasePath, "Albert bottom right walk"));
        creatureAnimator.albertBottomLeftSprites = LoadSpritesFromFolder(Path.Combine(walkCyclesBasePath, "Albert bottom left walk"));
        
        // Load Kai sprites
        creatureAnimator.kaiTopRightSprites = LoadSpritesFromFolder(Path.Combine(walkCyclesBasePath, "Kai top right walk"));
        creatureAnimator.kaiTopLeftSprites = LoadSpritesFromFolder(Path.Combine(walkCyclesBasePath, "Kai top left walk"));
        creatureAnimator.kaiBottomRightSprites = LoadSpritesFromFolder(Path.Combine(walkCyclesBasePath, "Kai bottom right walk"));
        creatureAnimator.kaiBottomLeftSprites = LoadSpritesFromFolder(Path.Combine(walkCyclesBasePath, "Kai bottom left walk"));
        
        Debug.Log("Finished loading all creature sprites!");
    }
    
    private Sprite[] LoadSpritesFromFolder(string folderPath)
    {
        List<Sprite> sprites = new List<Sprite>();
        
        // Get all png files in the directory
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        
        // Sort by filename to ensure proper order
        List<string> paths = new List<string>();
        foreach (string guid in guids)
        {
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        }
        
        // Sort paths to ensure we load frames in order
        paths.Sort();
        
        foreach (string path in paths)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                sprites.Add(sprite);
            }
            else
            {
                Debug.LogWarning($"Failed to load sprite at path: {path}");
            }
        }
        
        Debug.Log($"Loaded {sprites.Count} sprites from {folderPath}");
        return sprites.ToArray();
    }
}

// Custom editor to provide a button for loading sprites
[CustomEditor(typeof(CreatureSpriteLoader))]
public class CreatureSpriteLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        CreatureSpriteLoader loader = (CreatureSpriteLoader)target;
        
        if (GUILayout.Button("Load All Sprites"))
        {
            loader.LoadAllSprites();
        }
    }
}
#endif 