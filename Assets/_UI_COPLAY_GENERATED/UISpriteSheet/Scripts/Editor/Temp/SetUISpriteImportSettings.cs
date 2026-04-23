using UnityEditor;
using UnityEngine;

public static class SetUISpriteImportSettings
{
    [MenuItem("Tools/UI Generated/UISpriteSheet/Set Sprite Import Settings")]
    public static void Execute()
    {
        var path = "Assets/_UI_COPLAY_GENERATED/UISpriteSheet/Sprites/SpriteSheet.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"TextureImporter not found for {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        Debug.Log("Set Sprite import settings for generated sprite sheet.");
    }
}
