using System.IO;
using UnityEditor;
using UnityEngine;

public class TexturePostProcessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        for (int i = 0; i < importedAssets.Length; i++)
        {
            string importedAssetPath = importedAssets[i];

            if (!importedAssetPath.EndsWith(".png")) return;

            string[] importedAssetTokens = Path.GetFileNameWithoutExtension(importedAssetPath).ToLowerInvariant().Split('_');

            AssetImporter assetImporter = AssetImporter.GetAtPath(importedAssetPath);
            TextureImporter textureImporter = (TextureImporter)assetImporter;
            
            for (int j = 0; j < importedAssetTokens.Length; j++)
            {
                if (importedAssetTokens[j] == "atlas")
                {
                    if (assetImporter.userData.Contains("AUTO_PROCESSED")) break;

                    Texture loadedTexture = AssetDatabase.LoadAssetAtPath<Texture>(importedAssetPath);

                    if (loadedTexture == null)
                    {
                        Debug.LogError("Error loading texture at path: " + importedAssetPath);
                        continue;
                    }

                    textureImporter.textureType = TextureImporterType.Default;
                    textureImporter.alphaIsTransparency = true;
                    textureImporter.mipmapEnabled = false;
                    textureImporter.wrapMode = TextureWrapMode.Repeat;
                    textureImporter.filterMode = FilterMode.Point;
                    textureImporter.isReadable = true;

                    assetImporter.userData = "AUTO_PROCESSED";

                    AssetDatabase.WriteImportSettingsIfDirty(importedAssetPath);
                    AssetDatabase.Refresh();
                    return;
                }
            }
        }
    }
}
