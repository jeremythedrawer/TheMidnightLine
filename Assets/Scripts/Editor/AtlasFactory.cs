using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Atlas;

public class AtlasFactory : EditorWindow
{
    AtlasSO atlas;
    Vector2 scroll;

    float cellSize = 256f;
    float markerSize = 4f;

    int selectedIndex = -1;
    NPCMotion selectedMotionForSprite;
    int selectedFrameValue;

    private NPCMotion selectedMotion;
    private ClipType selectedClipType;

    const float padding = 50f;
    const float GUIHeaderColomns = 300;

    private float previewTime;
    private int previewFrameIndex;
    private int prevPreviewFrameIndex;
    private double lastEditorTime;
    private bool startAnim;
    private int cachedFrame = -1;

    private static Mesh previewQuad;
    private RenderTexture previewRT;
    [MenuItem("Tools/Atlas Factory")]
    private static void Open()
    {
        GetWindow<AtlasFactory>("Atlas Factory");
    }
    private void Update()
    {
        UpdatePreview();
    }
    private void OnGUI()
    {
        
        float halfWindowWidth = position.width * 0.5f;

        GUILayoutOption GUIWidth = GUILayout.Width(GUIHeaderColomns);
        GUILayoutOption GUIHalfWindowWidth = GUILayout.Width(halfWindowWidth);

        EditorGUILayout.BeginHorizontal();

        #region Input Fields
        EditorGUILayout.BeginVertical(GUIWidth);
        EditorGUILayout.LabelField("Inputs", EditorStyles.boldLabel);
        atlas = (AtlasSO)EditorGUILayout.ObjectField("Atlas", atlas, typeof(AtlasSO), false, GUIWidth);
        if (GUILayout.Button("Generate Sprites", GUIWidth))
        {
            MakeTextureReadable();
            GenerateSprites();
            SortSprites();
            atlas.UpdateAtlas();
            previewRT = null;
            previewQuad = null;
        }
        EditorGUILayout.EndVertical();
        #endregion

        #region View Fields
        EditorGUILayout.BeginVertical(GUIWidth);
        EditorGUILayout.LabelField("View", EditorStyles.boldLabel);
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize, GUIWidth);
        markerSize = EditorGUILayout.FloatField("Marker Size", markerSize, GUIWidth);
        EditorGUILayout.EndVertical();
        #endregion

        #region Debug Fields
        EditorGUILayout.BeginVertical(GUIWidth);
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Selected Index: {selectedIndex}", GUIWidth);
        EditorGUILayout.LabelField($"Selected Motion Type: {selectedMotionForSprite.ToString()}" , GUIWidth);
        EditorGUILayout.LabelField($"Selected Frame Value: {selectedFrameValue}", GUIWidth);
        EditorGUILayout.EndVertical();
        #endregion

        bool hasValidAtlas = atlas != null && atlas.texture != null && atlas.sprites != null;
        if (!hasValidAtlas) { EditorGUILayout.EndHorizontal(); return; };

        #region Animation Fields
        EditorGUILayout.BeginVertical(GUIWidth);
        EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        selectedMotion = (NPCMotion)EditorGUILayout.EnumPopup("Motion", selectedMotion, GUIWidth);
        if (EditorGUI.EndChangeCheck())
        {
            selectedClipType = atlas.clipDict[selectedMotion].clipType;
            previewTime = 0;
            prevPreviewFrameIndex = 0;
        }
        selectedClipType = (ClipType)EditorGUILayout.EnumPopup("Clip Type", selectedClipType, GUIWidth);


        for (int i = 0; i < atlas.clips.Length;  i++)
        {
            if (atlas.clips[i].motion == selectedMotion)
            {
                atlas.clips[i].clipType = selectedClipType;
                break;
            }
        }
        if (GUILayout.Button("Play", GUIWidth))
        {
            startAnim = true;
            previewTime = 0;
            previewFrameIndex = 0;
            atlas.material.SetInt(atlas.materialIDs.ids.atlasIndex, atlas.clipDict[selectedMotion].keyFrames[previewFrameIndex].spriteIndex);
        }
        if (GUILayout.Button("Stop", GUIWidth)) 
        { 
            startAnim = false; 
            previewTime = 0;
            previewFrameIndex = 0;
        }
        EditorGUILayout.EndVertical();
        #endregion

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();



        #region Sprite Editor
        EditorGUILayout.BeginVertical(GUIHalfWindowWidth);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        int columns = Mathf.RoundToInt((halfWindowWidth - padding) / (cellSize + padding));
        columns = Mathf.Max(1, columns);
        for (int i = 0; i < atlas.sprites.Length; i++)
        {
            if (i % columns == 0) EditorGUILayout.BeginHorizontal();

            EditorDrawAtlasSprite(atlas.sprites[i], i);

            if (i % columns == columns - 1 || i == atlas.sprites.Length - 1) EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        #endregion

        #region Animation Window
        EditorGUILayout.BeginVertical(GUIHalfWindowWidth);
        EditorGUILayout.LabelField("Preview Animation", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField($"Preview Frame Index: {previewFrameIndex.ToString()}", GUIWidth);
        if (previewRT == null)
        {
            previewRT = new RenderTexture((int)(halfWindowWidth), (int)(halfWindowWidth), 16, RenderTextureFormat.ARGB32); 
            previewRT.Create(); 
        }
        if (atlas != null && atlas.sprites != null)
        {
            Rect quadRect = new Rect(halfWindowWidth + 50, 200, halfWindowWidth * 0.75f, halfWindowWidth * 0.75f);

            if (atlas.material != null)
            {
                DrawPreview();
            }
            GUI.DrawTexture(quadRect, previewRT, ScaleMode.ScaleToFit, alphaBlend: true);
            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(quadRect, Color.clear, Color.grey);
        }
        EditorGUILayout.EndVertical();
        #endregion

        EditorGUILayout.EndHorizontal();

        GUI.enabled = atlas != null;
    }
    private void GenerateSprites()
    {
        Color32[] pixels = atlas.texture.GetPixels32();
        int width = atlas.texture.width;
        int height = atlas.texture.height;

        bool[] visited = new bool[pixels.Length];

        int spriteIndex = 0;

        List<Atlas.AtlasSprite> atlasSprites = new List<Atlas.AtlasSprite>();

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                int index = x + y * width;

                if (visited[index]) continue;

                if (pixels[index].a == 0) continue;

                List<Vector2Int> pixelPositions = FloodFill(x, y, width, height, visited, pixels);

                if (pixelPositions.Count < 10) continue; 
                Atlas.AtlasSprite newAtlasSprite = CreateAtlasSprite(pixelPositions, width, height, spriteIndex, pixels);
                atlasSprites.Add(newAtlasSprite);
                spriteIndex++;
            }
        }

        atlas.sprites = atlasSprites.ToArray();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    private void SortSprites()
    {
        float yTolerance = 0.01f;

        Array.Sort(atlas.sprites, (a, b) =>
        {
            if (Mathf.Abs(b.uvPos.y - a.uvPos.y) > yTolerance) return b.uvPos.y.CompareTo(a.uvPos.y);

            return a.uvPos.x.CompareTo(b.uvPos.x);
        });

        for (int i = 0; i < atlas.sprites.Length; i++)
        {
            atlas.sprites[i].index = i;
        }
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
    private AtlasSprite CreateAtlasSprite(List<Vector2Int> pixelPositions, float texWidth, float texHeight, int index, Color32[] pixelColors)
    {
        AtlasSprite newAtlasSprite = new AtlasSprite();

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
        
        List<SpriteMarker> spriteMarkers = new List<SpriteMarker>();
        Vector2 pivot = Vector2.zero;
        bool foundPivot = false;
        for (int x = (int)minX; x <= maxX; x++)
        {
            for (int y = (int)minY; y <= maxY; y++)
            {
                int pixelIndex = x + y * (int)texWidth;
                Color32 pixelColor = pixelColors[pixelIndex];

                if (pixelColor.a == 0) continue; 

                if (!foundPivot && 
                    pixelColor.r == atlas.customPivotColor.r && 
                    pixelColor.g == atlas.customPivotColor.g && 
                    pixelColor.b == atlas.customPivotColor.b)
                {
                    pivot.x = x;
                    pivot.y = y;
                    foundPivot = true;
                }
                for (int j = 0; j < atlas.markers.Length; j++)
                {
                    AtlasMarker atlasMarker = atlas.markers[j];

                    if (atlasMarker.color.r != pixelColor.r || atlasMarker.color.g != pixelColor.g || atlasMarker.color.b != pixelColor.b) continue;
                    SpriteMarker newSpriteMarker = new SpriteMarker();
                    newSpriteMarker.type = atlasMarker.type;
                    newSpriteMarker.objectPos.x = (x - minX) / (float)atlas.pixelsPerUnit;
                    newSpriteMarker.objectPos.y = (y - minY) / (float)atlas.pixelsPerUnit;
                    spriteMarkers.Add(newSpriteMarker);
                }
            }
        }

        float spriteWidth = maxX - minX + 1;
        float spriteHeight = maxY - minY + 1;

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
    private void EditorDrawAtlasSprite(AtlasSprite atlasSprite, int index)
    {
        Rect rect = GUILayoutUtility.GetRect(cellSize + padding, cellSize + padding, GUILayout.ExpandWidth(false));
        rect = new Rect(rect.x + padding * 0.5f, rect.y + padding * 0.5f, cellSize, cellSize);

        Rect uvRect = new Rect(atlasSprite.uvPos, atlasSprite.uvSize);
        float spritePixelWidth = atlasSprite.uvSize.x * atlas.texture.width;
        float spritePixelHeight = atlasSprite.uvSize.y * atlas.texture.height;

        float aspectRatio = Mathf.Min(rect.width / spritePixelWidth, rect.height / spritePixelHeight);
        Vector2 drawSize = new Vector2(spritePixelWidth * aspectRatio, spritePixelHeight * aspectRatio);
        Rect spriteRect = new Rect(rect.center.x - drawSize.x * 0.5f, rect.center.y - drawSize.y * 0.5f, drawSize.x, drawSize.y);

        GUI.DrawTextureWithTexCoords(spriteRect, atlas.texture, uvRect, alphaBlend: true);

        Vector2 pivotPos = new Vector2(Mathf.Lerp(spriteRect.xMin, spriteRect.xMax, atlasSprite.normPivot.x),Mathf.Lerp(spriteRect.yMax, spriteRect.yMin, atlasSprite.normPivot.y));
        Rect pivotRect = new Rect(pivotPos - Vector2.one * markerSize, Vector2.one * (markerSize * 2));

        Handles.BeginGUI();
        Handles.DrawSolidRectangleWithOutline(spriteRect, Color.clear, Color.grey);
        Handles.DrawSolidRectangleWithOutline(pivotRect, Color.clear, atlas.customPivotColor);

        if (atlasSprite.markers.Length != 0)
        {
            for (int i = 0; i < atlasSprite.markers.Length; i++)
            {
                Atlas.SpriteMarker marker = atlasSprite.markers[i];
                Vector2 markerPixelInSprite = marker.objectPos * atlas.pixelsPerUnit;
                Vector2 markerNormalized = new Vector2(markerPixelInSprite.x / spritePixelWidth, markerPixelInSprite.y / spritePixelHeight);

                Vector2 markerPixel = new Vector2(Mathf.Lerp(spriteRect.xMin, spriteRect.xMax, markerNormalized.x), Mathf.Lerp(spriteRect.yMax, spriteRect.yMin, markerNormalized.y));
                Rect markerRect = new Rect(markerPixel - Vector2.one * markerSize, Vector2.one * (markerSize * 2));

                Color32 markerColor = Color.white;
                for (int j = 0; j < atlas.markers.Length; j++)
                {
                    Atlas.AtlasMarker atlasMarker = atlas.markers[j];
                    if ((marker.type & atlasMarker.type) != 0)
                    {
                        markerColor = atlasMarker.color;
                        break;
                    }
                }
                Handles.DrawSolidRectangleWithOutline(markerRect, Color.clear, markerColor);
            }
        }
        Handles.EndGUI();

        //Sprite Index Label
        Vector2 indexPos = new Vector2(spriteRect.xMin, spriteRect.yMin);
        GUIStyle indexStyle = new GUIStyle(EditorStyles.boldLabel) 
        { 
            alignment = TextAnchor.UpperLeft, normal = { textColor = Color.white } 
        };
        GUI.Label(new Rect(indexPos, new Vector2(20, 20)), index.ToString(), indexStyle);



        float clipDataRectWidth = cellSize * 0.5f;
        float clipDataRectHeight = 16;
        float clipDataRectYPos = spriteRect.yMax + 2;
        float minRectX = spriteRect.center.x - clipDataRectWidth;
        Rect animTypeRect = new Rect(minRectX, clipDataRectYPos, clipDataRectWidth, clipDataRectHeight);
        Rect frameRect = new Rect(spriteRect.center.x, clipDataRectYPos, clipDataRectWidth, clipDataRectHeight);
        NPCMotion animType = NPCMotion.None;
        int frameValue = -1;
        int existingClipIndex = -1;
        for (int i = 0; i < atlas.clips.Length; i++)
        {
            AtlasClip clip = atlas.clips[i];
            for (int j = 0; j < clip.keyFrames.Length; j++)
            {
                AtlasKeyframe keyframe = clip.keyFrames[j];

                if (keyframe.spriteIndex == atlasSprite.index)
                {
                    existingClipIndex = i;
                    frameValue = keyframe.holdTime;
                }
            }
        }
        animType = existingClipIndex == -1 ? NPCMotion.None : atlas.clips[existingClipIndex].motion;

        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        {
            if (selectedIndex != index)
            {
                selectedIndex = index;
                selectedMotionForSprite = animType;
                selectedFrameValue = frameValue;
            }
        }

        if (selectedIndex == index)
        {
            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(spriteRect, Color.clear, Color.limeGreen);
            Handles.EndGUI();

            EditorGUI.BeginChangeCheck();

            NPCMotion prevAnimType = animType;

            selectedMotionForSprite = (NPCMotion)EditorGUI.EnumPopup(animTypeRect, selectedMotionForSprite);
            selectedFrameValue = EditorGUI.IntField(frameRect, selectedFrameValue);
            if (EditorGUI.EndChangeCheck())
            {
                bool foundKeyframe = false;
                int selectedClipIndex = -1;
                int prevClipIndex = -1;

                for (int i = 0; i < atlas.clips.Length; i++)
                {
                    Atlas.AtlasClip clip = atlas.clips[i];

                    if (clip.motion == selectedMotionForSprite)
                    {
                        for (int j = 0; j < clip.keyFrames.Length; j++)
                        {
                            AtlasKeyframe keyframe = clip.keyFrames[j];
                            if (keyframe.spriteIndex == selectedIndex)
                            {
                                foundKeyframe = true;
                                if (selectedFrameValue > -1)
                                {
                                    atlas.clips[i].keyFrames[j].holdTime = selectedFrameValue;
                                }
                                break;
                            }
                        }
                        selectedClipIndex = i;
                    }
                    else if (clip.motion == prevAnimType)
                    {
                        prevClipIndex = i;
                    }
                    if (prevClipIndex != -1 && selectedClipIndex != -1) break;
                }

                if (selectedClipIndex == -1)
                {
                    if (selectedMotionForSprite != NPCMotion.None && selectedFrameValue > -1)
                    {
                        List<AtlasClip> clipsList = atlas.clips.ToList();
                        AtlasClip newClip = new AtlasClip();
                        newClip.motion = selectedMotionForSprite;
                        newClip.keyFrames = new Atlas.AtlasKeyframe[1];
                        newClip.keyFrames[0].spriteIndex = selectedIndex;
                        newClip.keyFrames[0].holdTime = selectedFrameValue;
                        clipsList.Add(newClip);
                        atlas.clips = clipsList.ToArray();
                    }
                }
                else if (!foundKeyframe && selectedFrameValue > -1)
                {
                    List<AtlasKeyframe> selectedClipKeyframesList = atlas.clips[selectedClipIndex].keyFrames.ToList();
                    AtlasKeyframe newKeyframe = new AtlasKeyframe();
                    newKeyframe.spriteIndex = selectedIndex;
                    newKeyframe.holdTime = selectedFrameValue;
                    selectedClipKeyframesList.Add(newKeyframe);
                    atlas.clips[selectedClipIndex].keyFrames = selectedClipKeyframesList.ToArray();
                }

                if (prevClipIndex != -1)
                {
                    List<AtlasKeyframe> prevClipKeyframesList = atlas.clips[prevClipIndex].keyFrames.ToList();
                    for (int i = 0; i < prevClipKeyframesList.Count; i++)
                    {
                        if (prevClipKeyframesList[i].spriteIndex == selectedIndex)
                        {
                            prevClipKeyframesList.RemoveAt(i);
                            break;
                        }
                    }
                    atlas.clips[prevClipIndex].keyFrames = prevClipKeyframesList.ToArray();
                }

                previewTime = 0;
                prevPreviewFrameIndex = 0;
                atlas.UpdateAtlas();
                EditorUtility.SetDirty(atlas);
                AssetDatabase.SaveAssets();
            }
        }
        else if (animType != NPCMotion.None)
        {
            animType = (NPCMotion)EditorGUI.EnumPopup(animTypeRect, animType);
            if (frameValue > -1)
            {
                frameValue = EditorGUI.IntField(frameRect, frameValue);
            }
        }
    }
    private void DrawPreview()
    {
        if (atlas == null || atlas.sprites == null) return;

        if (previewFrameIndex == cachedFrame) return;

        cachedFrame = previewFrameIndex;

        RenderTexture activeRT = RenderTexture.active;
        RenderTexture.active = previewRT;
        GL.Clear(true, true, Color.clear);

        if (atlas.material != null)
        {
            atlas.material.SetPass(0);

            GL.PushMatrix();
            GL.LoadProjectionMatrix(Matrix4x4.Ortho(-1, 1, 0, 2, -1, 1));
            Graphics.DrawMeshNow(GetBottomCenterQuad(), Matrix4x4.identity);
            GL.PopMatrix();
        }

        RenderTexture.active = activeRT;
    }
    private void UpdatePreview()
    {
        if (atlas == null || atlas.sprites == null) return;
        double now = EditorApplication.timeSinceStartup;
        float delta = (float)(now - lastEditorTime);
        lastEditorTime = now;
        delta = Mathf.Clamp01(delta);
        previewTime += delta;
        if (startAnim)
        {
            if (!atlas.clipDict.TryGetValue(selectedMotion, out AtlasClip clip)) return;
            int newFrameIndex = GetCurrentFrameIndex(clip, selectedClipType, atlas.framesPerSecond, previewTime, previewFrameIndex, prevPreviewFrameIndex);

            if (newFrameIndex != previewFrameIndex)
            {
                prevPreviewFrameIndex = previewFrameIndex;
                previewFrameIndex = newFrameIndex;
                atlas.material.SetInt(atlas.materialIDs.ids.atlasIndex, atlas.clipDict[selectedMotion].keyFrames[previewFrameIndex].spriteIndex);
                previewTime = 0;
                Repaint();
            }
        }
    }
    private Mesh GetBottomCenterQuad()
    {
        if (previewQuad != null) return previewQuad;
        Debug.Log("generating new quad");
        previewQuad = new Mesh();
        previewQuad.name = "BottomCenterQuad";

        previewQuad.vertices = new[]
        {
            new Vector3(-1f, 0f, 0f), // bottom left
            new Vector3(1f, 0f, 0f), // bottom right
            new Vector3(1f, 2f, 0f), // top right
            new Vector3(-1f, 2f, 0f), // top left
        };

        previewQuad.uv = new[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
        };

        previewQuad.triangles = new[]
        {
            0, 2, 1,
            0, 3, 2
        };

        previewQuad.RecalculateNormals();
        previewQuad.RecalculateBounds();

        return previewQuad;
    }
}
