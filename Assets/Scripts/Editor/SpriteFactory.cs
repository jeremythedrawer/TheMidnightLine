using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SpriteFactory : EditorWindow
{
    AtlasSO atlas;
    Atlas.AnimationType animationType;
    Color32 animationSelectionColor;
    Vector2 scroll;
    int selectedIndex = -1;

    
    float cellSize = 256f;
    float markerSize = 4f;
    const float padding = 10f;
    const float GUIWidthValue = 300f;
    [MenuItem("Tools/Sprite Factory")]
    private static void Open()
    {
        GetWindow<SpriteFactory>("Sprite Factory");
    }
    private void OnGUI()
    {
        GUILayoutOption GUIWidth = GUILayout.Width(GUIWidthValue);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUIWidth);
        EditorGUILayout.LabelField("Inputs", EditorStyles.boldLabel);
        atlas = (AtlasSO)EditorGUILayout.ObjectField("Atlas", atlas, typeof(AtlasSO), false, GUIWidth);

        if (atlas == null || atlas.texture == null || atlas.sprites == null) return;

        if (GUILayout.Button("Generate Sprites", GUIWidth))
        {
            MakeTextureReadable();
            GenerateSprites();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUIWidth);
        EditorGUILayout.LabelField("View", EditorStyles.boldLabel);
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize, GUIWidth);
        markerSize = EditorGUILayout.FloatField("Marker Size", markerSize, GUIWidth);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUIWidth);
        EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
        animationType = (Atlas.AnimationType)EditorGUILayout.EnumPopup("Animation Type", animationType, GUIWidth);
        animationSelectionColor = EditorGUILayout.ColorField("Animation Selection Color", animationSelectionColor, GUIWidth);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        int columns = Mathf.FloorToInt((position.width - padding) / (cellSize + padding));
        columns = Mathf.Max(1, columns);
        for (int i = 0; i < atlas.sprites.Length; i++)
        {
            if (i % columns == 0) EditorGUILayout.BeginHorizontal();

            DrawSpriteCell(atlas.sprites[i], i);

            if (i % columns == columns - 1 || i == atlas.sprites.Length - 1) EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        GUI.enabled = atlas != null;
    }
    private void GenerateSprites()
    {
        Color32[] pixels = atlas.texture.GetPixels32();
        int width = atlas.texture.width;
        int height = atlas.texture.height;

        bool[] visited = new bool[pixels.Length];

        int spriteIndex = 0;

        List<Atlas.Sprite> atlasSprites = new List<Atlas.Sprite>();

        for (int y  = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = x + y * width;

                if (visited[index]) continue;

                if (pixels[index].a == 0) continue;

                List<Vector2Int> pixelPositions = FloodFill(x, y, width, height, visited, pixels);

                if (pixelPositions.Count < 10) continue; 
                Atlas.Sprite newAtlasSprite = CreateAtlasSprite(pixelPositions, width, height, spriteIndex, pixels);
                atlasSprites.Add(newAtlasSprite);
                spriteIndex++;
            }
        }

        atlas.sprites = atlasSprites.ToArray();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    private List<Vector2Int> FloodFill(int startX, int startY, int width, int height, bool[] visited, Color32[] pixels)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(new Vector2Int(startX, startY)); // Starting at the bottom left of the new sprite

        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height) continue; // skip if the current pixel isnt within the texture bounds
            int index = p.x + p.y * width; // Convert to 1D array
            if (visited[index]) continue; // skip if the pixel is already apart of another sprite
            visited[index] = true;
            if (pixels[index].a == 0) continue; // skip if the alpha is 0

            result.Add(p);

            Vector2Int rightNeighbour = new Vector2Int(p.x + 1, p.y);
            Vector2Int leftNeighbour = new Vector2Int(p.x - 1, p.y);
            Vector2Int topNeightbour = new Vector2Int(p.x, p.y + 1);
            Vector2Int bottomNeightbour = new Vector2Int(p.x, p.y - 1);

            queue.Enqueue(rightNeighbour);
            queue.Enqueue(leftNeighbour);
            queue.Enqueue(topNeightbour);
            queue.Enqueue(bottomNeightbour);
        }

        return result;

    }
    private Atlas.Sprite CreateAtlasSprite(List<Vector2Int> pixelPositions, float texWidth, float texHeight, int index, Color32[] pixelColors)
    {
        Atlas.Sprite newAtlasSprite = new Atlas.Sprite();

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < pixelPositions.Count; i++)
        {
            Vector2 p = pixelPositions[i];

            minX = Mathf.Min(minX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxX = Mathf.Max(maxX, p.x);
            maxY = Mathf.Max(maxY, p.y);
        }

        
        List<Atlas.SpriteMarker> spriteMarkers = new List<Atlas.SpriteMarker>();
        Vector2 pivot = Vector2.zero;
        bool foundPivot = false;
        for (int x = (int)minX; x <= maxX; x++)
        {
            for (int y = (int)minY; y <= maxY; y++)
            {
                int pixelIndex = x + y * (int)texWidth;
                Color32 pixelColor = pixelColors[pixelIndex];

                if (!foundPivot && 
                    pixelColor.r == atlas.customPivotColor.r && 
                    pixelColor.g == atlas.customPivotColor.g && 
                    pixelColor.b == atlas.customPivotColor.b && 
                    pixelColor.a == atlas.customPivotColor.a)
                {
                    pivot.x = x;
                    pivot.y = y;
                    foundPivot = true;
                }
                for (int j = 0; j < atlas.markers.Length; j++)
                {
                    Atlas.AtlasMarker atlasMarker = atlas.markers[j];

                    if (atlasMarker.color.r != pixelColor.r || atlasMarker.color.g != pixelColor.g || atlasMarker.color.b != pixelColor.b || atlasMarker.color.a != pixelColor.a) continue;

                    Atlas.SpriteMarker newSpriteMarker = new Atlas.SpriteMarker();
                    newSpriteMarker.type = atlasMarker.type;
                    newSpriteMarker.objectPos.x = (x - minX) / (float)atlas.pixelsPerUnit;
                    newSpriteMarker.objectPos.y = (y - minY) / (float)atlas.pixelsPerUnit;
                    spriteMarkers.Add(newSpriteMarker);
                }
            }
        }

        float spriteWidth = maxX - minX + 1;
        float spriteHeight = maxY - minY + 1;

        newAtlasSprite.index = index;

        newAtlasSprite.uvPos.x = minX / texWidth;
        newAtlasSprite.uvPos.y = minY / texHeight;

        newAtlasSprite.uvSize.x = spriteWidth / texWidth;
        newAtlasSprite.uvSize.y = spriteHeight / texHeight;

        if (foundPivot)
        {
            newAtlasSprite.normPivot.x = (pivot.x - minX) / spriteWidth;
            newAtlasSprite.normPivot.y = (pivot.y - minY) / spriteHeight;
        }
        else
        {
            newAtlasSprite.normPivot.x = 0;
            newAtlasSprite.normPivot.y = 0;
        }

            newAtlasSprite.markers = spriteMarkers.ToArray();
        return newAtlasSprite;
    }
    private void MakeTextureReadable()
    {
        string path = AssetDatabase.GetAssetPath(atlas.texture);
        TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(path);
        if (textureImporter.isReadable) return;

        textureImporter.isReadable = true;
        textureImporter.SaveAndReimport();

    }
    private void DrawSpriteCell(Atlas.Sprite sprite, int index)
    {
        Rect rect = GUILayoutUtility.GetRect(cellSize + padding, cellSize + padding, GUILayout.ExpandWidth(false));
        rect = new Rect(rect.x + padding * 0.5f, rect.y + padding * 0.5f, cellSize, cellSize);

        Rect uvRect = new Rect(sprite.uvPos, sprite.uvSize);
        float spritePixelWidth = sprite.uvSize.x * atlas.texture.width;
        float spritePixelHeight = sprite.uvSize.y * atlas.texture.height;

        float scale = Mathf.Min(rect.width / spritePixelWidth, rect.height / spritePixelHeight);

        Vector2 drawSize = new Vector2(spritePixelWidth * scale, spritePixelHeight * scale);

        Rect drawRect = new Rect(rect.center.x - drawSize.x * 0.5f, rect.center.y - drawSize.y * 0.5f, drawSize.x, drawSize.y);

        GUI.DrawTextureWithTexCoords(drawRect, atlas.texture, uvRect, alphaBlend: true);

        Vector2 pivotPixel = new Vector2(Mathf.Lerp(drawRect.xMin, drawRect.xMax, sprite.normPivot.x),Mathf.Lerp(drawRect.yMax, drawRect.yMin, sprite.normPivot.y));
        Rect pivotRect = new Rect(pivotPixel - Vector2.one * markerSize, Vector2.one * (markerSize * 2));

        Handles.BeginGUI();
        Handles.DrawSolidRectangleWithOutline(drawRect, Color.clear, Color.grey);
        Handles.DrawSolidRectangleWithOutline(pivotRect, Color.clear, Color.red);
        Handles.EndGUI();

        if (sprite.markers.Length != 0)
        {
            for (int i = 0; i < sprite.markers.Length; i++)
            {
                Atlas.SpriteMarker marker = sprite.markers[i];

                float markerXPos = drawRect.x + marker.objectPos.x * atlas.pixelsPerUnit / atlas.texture.width * drawRect.width;
                float markerYPos = drawRect.y + drawRect.height - (marker.objectPos.y * atlas.pixelsPerUnit / atlas.texture.height * drawRect.height);
                Vector2 markerPixel = new Vector2(markerXPos, markerYPos);
                Rect markerRect = new Rect(markerPixel - Vector2.one * 2f, Vector2.one * 4);
                EditorGUI.DrawRect(markerRect, Color.cyan);
            }
        }

        if (GUI.Button(drawRect, GUIContent.none, GUIStyle.none))
        {
            selectedIndex = index;
            //TODO: Add sprite to clip array
        }

        if (selectedIndex == index)
        {
            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(drawRect, Color.clear, Color.green);
            Handles.EndGUI();

        }


    }
    private void AddSpriteToClip(Atlas.Sprite sprite)
    {

    }
}
