using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;

[CreateAssetMenu(fileName = "SpriteAtlasFactory", menuName = "Editor SOs / Sprite Atlas Factory")]
public class SpriteAtlasFactory : ScriptableObject
{
    [SerializeField] Texture2D spriteAtlas;
    public void SetOriginPoints()
    {
        TextureImporter atlasImporter = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(spriteAtlas));
        atlasImporter.isReadable = true;
        atlasImporter.spriteImportMode = SpriteImportMode.Multiple;

        SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(spriteAtlas);
        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] spriteRects = dataProvider.GetSpriteRects();

        for (int i = 0; i < spriteRects.Length; i++) // Looping through each sprite in atlas
        {
            
            int startX = Mathf.RoundToInt(spriteRects[i].rect.x);
            int startY = Mathf.RoundToInt(spriteRects[i].rect.y);
            int width = Mathf.RoundToInt(spriteRects[i].rect.width);
            int height = Mathf.RoundToInt(spriteRects[i].rect.height);
            bool foundRed = false;

            for (int x = startX; x < startX + width && !foundRed; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    Color color = spriteAtlas.GetPixel(x, y);

                    if (color == Color.red)
                    {
                        float xPiv = (x - startX) / (float)width;
                        float yPiv = (y - startY) / (float)height;

                        spriteRects[i].pivot = new Vector2(xPiv, yPiv);
                        spriteRects[i].alignment = SpriteAlignment.Custom;

                        foundRed = true;
                        break;
                    }
                }
            }

            if (!foundRed) Debug.LogWarning($"never found red pixel origin point of: {spriteRects[i].name}");
        }

        dataProvider.SetSpriteRects(spriteRects);
        dataProvider.Apply();
    }
}

[CustomEditor(typeof(SpriteAtlasFactory))]
public class SpriteAtlasFactorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SpriteAtlasFactory factory = (SpriteAtlasFactory)target;

        if (GUILayout.Button("Set Origin Points"))
        {
            factory.SetOriginPoints();
        }
    }
}

