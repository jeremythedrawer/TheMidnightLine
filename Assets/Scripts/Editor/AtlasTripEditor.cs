using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

using static Atlas;
using static AtlasRendering;
using static AtlasSpawn;
using static System.Collections.Specialized.BitVector32;

public class AtlasTripEditor : EditorWindow
{
    [Flags] public enum AdjustState
    { 
        None = 0,
        LeftSize = 1 << 0,
        RightSize = 1 << 1,
        TopSize = 1 << 2,
        BottomSize = 1 << 3,
        Position = 1 << 4,
    }


    public const float COLUMN_WIDTH = 150;
    public const float GRAPH_POS_Y = 100;
    public const float PADDING = 70;
    public const float FULL_RECT_HEIGHT = 3000;
    public const float OBJECT_FIELD_WIDTH = 100; 
    public const float SLIDER_FIELD_WIDTH = 200; 

    public static SpawnData spawnData;

    public GUIStyle boldStyle;
    public GUIStyle headerStyle;
    public GUIStyle selectedStyle;
    public GUIStyle leftPanelStyle;
    public GUIStyle buttonStyle;

    public TripData trip;

    public ParticleAtlas selectedParticleAtlas;
    
    public TripEditorPrefab prefabInput;

    public Dictionary<AtlasSO, ParticleAtlas> atlasParticleAtlasDict;

    public Color[] particleAtlasColors;

    float[] kmphInputValues;
    float[] dayNightInputValues;

    public Vector2 graphScroll;
    public Vector2 sidePanelScroll;
    public Vector2 randomInput;

    float curPosY;
    float curPosX;

    public ParticleWidthType selectedWidthType;
    public AdjustState adjustState;

    int selectedParticleCount;
    int selectedPosDataIndex;
    int clickEndStation;
    int clickDepthSize;
    int clickStartStation;
    int clickPosZ;
    int clickMousePosX;
    int clickMousePosY;

    [MenuItem("Tools/Atlas Trip Editor")]
    private static void Open()
    {
        OnOpen();
    }
    private void OnEnable()
    {
        Enable();
    }
    private void OnSelectionChange()
    {
    }
    private void OnGUI()
    {
        SetGUIStyles();
        DrawFirstRow();
        DrawSecondRow();
        DrawThirdRow();
        DrawGraph();
    }
    public static void OnOpen()
    {
        GetWindow<AtlasTripEditor>("Atlas Trip Editor");

    }
    public void Enable()
    {
        string spawnDataPath = AssetDatabase.GUIDToAssetPath("cb76807a71f74704a94a0f5cfe3b2c50");
        SpawnData foundSpawnData = AssetDatabase.LoadAssetAtPath<SpawnData>(spawnDataPath);
        spawnData = foundSpawnData;
    }

    private void SetGUIStyles()
    {
        boldStyle = new GUIStyle();
        boldStyle.alignment = TextAnchor.UpperLeft;
        boldStyle.normal.textColor = Color.white;
        boldStyle.fontStyle = FontStyle.Bold;
        boldStyle.padding.left = 10;
        boldStyle.padding.right = 10;
        boldStyle.padding.top = 5;
        boldStyle.padding.bottom = 5;


        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.alignment = TextAnchor.UpperLeft;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.active.textColor = Color.black;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.padding.left = 10;
        buttonStyle.padding.right = 10;
        buttonStyle.padding.top = 5;
        buttonStyle.padding.bottom = 5;


        headerStyle = new GUIStyle();
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.normal.textColor = Color.white;
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;

        selectedStyle = new GUIStyle();
        selectedStyle.alignment = TextAnchor.MiddleCenter;
        selectedStyle.normal.textColor = Color.black;
        selectedStyle.fontSize = 16;
        selectedStyle.fontStyle = FontStyle.Bold;

        leftPanelStyle = new GUIStyle();
        leftPanelStyle.alignment = TextAnchor.UpperRight;
        leftPanelStyle.normal.textColor = Color.white;
        leftPanelStyle.fontStyle = FontStyle.Normal;
        leftPanelStyle.fontSize = 12;
        leftPanelStyle.padding.left = 10;
        leftPanelStyle.padding.right = 10;
    }
    private void DrawFirstRow()
    {
        curPosX = 0;
        curPosY = 0;

        GUIContent tripContent = new GUIContent("Trip");        
        Vector2 tripLabelSize = boldStyle.CalcSize(tripContent);
        Vector2 tripLabelPos = new Vector2();
        tripLabelPos.x = curPosX;
        tripLabelPos.y = curPosY;
        Rect tripLabelRect = new Rect();
        tripLabelRect.position = tripLabelPos;
        tripLabelRect.size = tripLabelSize;
        GUI.Label(tripLabelRect, tripContent, boldStyle);
        curPosX += tripLabelSize.x;

        Vector2 tripInputPos = new Vector2();
        tripInputPos.x = curPosX;
        tripInputPos.y = curPosY;
        Vector2 tripInputSize = new Vector2();
        tripInputSize.x = OBJECT_FIELD_WIDTH;
        tripInputSize.y = tripLabelSize.y;
        curPosX += tripInputSize.x;

        Rect tripInputRect = new Rect();
        tripInputRect.position = tripInputPos;
        tripInputRect.size = tripInputSize;

        EditorGUI.BeginChangeCheck();
        trip = (TripData)EditorGUI.ObjectField(tripInputRect, GUIContent.none, trip, typeof(TripData), allowSceneObjects: false);
        curPosY += tripInputSize.y;

        if (EditorGUI.EndChangeCheck())
        {
            dayNightInputValues = trip.dayNightValues;
            kmphInputValues = trip.kmValues;

            SetAtlasDict();
        }
    }
    private void DrawSecondRow()
    {
        if (trip == null) return;

        curPosX = 0;

        GUIContent prefabLabelContent = new GUIContent("Prefab");
        Vector2 prefabLabelPos = new Vector2();
        prefabLabelPos.x = curPosX;
        prefabLabelPos.y = curPosY;
        Vector2 prefabLabelSize = boldStyle.CalcSize(prefabLabelContent);
        Rect prefabLabelRect = new Rect();
        prefabLabelRect.position = prefabLabelPos;
        prefabLabelRect.size = prefabLabelSize;
        GUI.Label(prefabLabelRect, prefabLabelContent, boldStyle);
        
        curPosX += prefabLabelSize.x;

        Vector2 prefabInputPos = new Vector2();
        prefabInputPos.x = curPosX;
        prefabInputPos.y = curPosY;
        Vector2 prefabInputSize = new Vector2();
        prefabInputSize.x = OBJECT_FIELD_WIDTH;
        prefabInputSize.y = prefabLabelSize.y;
        Rect prefabInputRect = new Rect();
        prefabInputRect.position = prefabInputPos;
        prefabInputRect.size = prefabInputSize;

        EditorGUI.BeginChangeCheck();
        prefabInput = (TripEditorPrefab)EditorGUI.ObjectField(prefabInputRect, GUIContent.none, prefabInput, typeof(TripEditorPrefab), allowSceneObjects: false);

        if (EditorGUI.EndChangeCheck())
        {
            if (prefabInput != null)
            {
                SetAtlasDict();
                selectedParticleAtlas = atlasParticleAtlasDict[prefabInput.mainRenderer.atlas];
                selectedPosDataIndex = -1;
            }
        }

        curPosX += prefabInputSize.x;

        GUIContent addParticleContent = new GUIContent("Add Particle");
        Vector2 addParticleDataPos = new Vector2();
        addParticleDataPos.x = curPosX;
        addParticleDataPos.y = curPosY;
        Vector2 addParticleDataSize = buttonStyle.CalcSize(addParticleContent);
        Rect addParticleDataRect = new Rect();
        addParticleDataRect.position = addParticleDataPos;
        addParticleDataRect.size = addParticleDataSize;
        bool addParticleData = GUI.Button(addParticleDataRect, addParticleContent, buttonStyle);
        
        curPosX += addParticleDataSize.x;

        if (addParticleData && prefabInput != null)
        {
            randomInput.x = 0;
            randomInput.y = 0;

            ParticlePosData[] particleDataArray = new ParticlePosData[selectedParticleAtlas.posDataArray.Length + 1];

            for (int i = 0; i < selectedParticleAtlas.posDataArray.Length; i++)
            {
                particleDataArray[i] = selectedParticleAtlas.posDataArray[i];
            }

            ParticlePosData newPosData = new ParticlePosData();
            newPosData.startStation = 0;
            newPosData.endStation = 1;
            newPosData.posZ = 1;
            newPosData.posY = prefabInput.transform.position.y;
            newPosData.depthSize = 1;
            selectedWidthType = prefabInput.widthType;
            newPosData.widthType = selectedWidthType;
            newPosData.spritesPerParticle = newPosData.widthType == ParticleWidthType.Slice ? 9 : 1;
            string assetPath = AssetDatabase.GetAssetPath(prefabInput);
            newPosData.prefabGUID = AssetDatabase.AssetPathToGUID(assetPath);

            switch (selectedParticleAtlas.particleType)
            {
                case ParticleType.Scroll:
                {
                    newPosData.particleCount = 2;
                }
                break;
                case ParticleType.Zone:
                {
                    selectedParticleCount = 1;
                    newPosData.particleCount = selectedParticleCount;
                }
                break;
            }

            newPosData = SetQuadScales(newPosData);

            if (prefabInput.preScrollerRenderers != null)
            {
                EdgeScroller preScroller = new EdgeScroller();
                preScroller.edgeSpriteDataArray = new EdgeSpriteData[prefabInput.preScrollerRenderers.Length];
                float posXOffset = 0;

                for (int i = 0; i < prefabInput.preScrollerRenderers.Length; i++)
                {
                    AtlasRenderer preScrollerRenderer = prefabInput.preScrollerRenderers[i];
                    
                    EdgeSpriteData preScrollerSpriteData = new EdgeSpriteData();
                    preScrollerSpriteData.spriteIndex = preScrollerRenderer.spriteIndex;

                    Vector4 offset = new Vector4();
                    offset.x = preScrollerRenderer.transform.localPosition.x;
                    offset.y = preScrollerRenderer.transform.localPosition.y;
                    offset.z = preScrollerRenderer.transform.localPosition.z;

                    preScrollerSpriteData.offset = offset;

                    Vector4 scale = new Vector4();
                    scale.x = preScrollerRenderer.width;
                    scale.y = preScrollerRenderer.height;

                    preScrollerSpriteData.scale = scale;

                    preScroller.edgeSpriteDataArray[i] = preScrollerSpriteData;

                    if (offset.x < posXOffset) posXOffset = offset.x;
                }

                newPosData.preScroller = preScroller;
                newPosData.posX = -posXOffset;
            }
            if (prefabInput.postScrollersRenderers != null)
            {
                EdgeScroller postScroller = new EdgeScroller();
                postScroller.edgeSpriteDataArray = new EdgeSpriteData[prefabInput.postScrollersRenderers.Length];

                float widthOffset;
                if (newPosData.widthType == ParticleWidthType.Slice)
                {
                    widthOffset = selectedParticleAtlas.spriteData[newPosData.spriteIndex].worldPivotAndSize.z;
                    widthOffset += newPosData.scaleX * selectedParticleAtlas.spriteData[newPosData.spriteIndex + 1].worldPivotAndSize.z;
                    widthOffset += selectedParticleAtlas.spriteData[newPosData.spriteIndex + 2].worldPivotAndSize.z;
                }
                else
                {
                    widthOffset = newPosData.scaleX * selectedParticleAtlas.spriteData[newPosData.spriteIndex].worldPivotAndSize.z;
                }
                for (int i = 0; i < prefabInput.postScrollersRenderers.Length; i++)
                {
                    AtlasRenderer postScrollerRenderer = prefabInput.postScrollersRenderers[i];

                    EdgeSpriteData postScrollerSpriteData = new EdgeSpriteData();
                    postScrollerSpriteData.spriteIndex = postScrollerRenderer.spriteIndex;

                    Vector4 offset = new Vector4();
                    offset.x = postScrollerRenderer.transform.localPosition.x + widthOffset;
                    offset.y = postScrollerRenderer.transform.localPosition.y;
                    offset.z = postScrollerRenderer.transform.localPosition.z;

                    postScrollerSpriteData.offset = offset;

                    Vector4 scale = new Vector4();
                    scale.x = postScrollerRenderer.width;
                    scale.y = postScrollerRenderer.height;

                    postScrollerSpriteData.scale = scale;

                    postScroller.edgeSpriteDataArray[i] = postScrollerSpriteData;
                }

                newPosData.postScroller = postScroller;
            }
            
            particleDataArray[selectedParticleAtlas.posDataArray.Length] = newPosData;
            selectedParticleAtlas.posDataArray = particleDataArray;

            selectedPosDataIndex = selectedParticleAtlas.posDataArray.Length - 1;

            ParticlePosData posData = selectedParticleAtlas.posDataArray[selectedPosDataIndex];

            selectedParticleCount = posData.particleCount;
            Repaint();
        }

        GUIContent removeParticleDataContent = new GUIContent("Remove Particle");
        Vector2 removeParticleDataPos = new Vector2();
        removeParticleDataPos.x = curPosX;
        removeParticleDataPos.y = curPosY;
        Vector2 removeParticleDataSize = buttonStyle.CalcSize(removeParticleDataContent);
        Rect removeParticleDataRect = new Rect();
        removeParticleDataRect.position = removeParticleDataPos;
        removeParticleDataRect.size = removeParticleDataSize;
        bool removeParticleData = GUI.Button(removeParticleDataRect, removeParticleDataContent, buttonStyle);

        curPosX += removeParticleDataSize.x;

        if (removeParticleData && selectedParticleAtlas != null)
        {
            List<ParticlePosData> particleDataList = selectedParticleAtlas.posDataArray.ToList();
            particleDataList.RemoveAt(selectedPosDataIndex);
            selectedParticleAtlas.posDataArray = particleDataList.ToArray();
            selectedPosDataIndex = selectedPosDataIndex - 1;

            if (selectedParticleAtlas.posDataArray.Length > 0)
            {
                ParticlePosData posData = selectedParticleAtlas.posDataArray[selectedPosDataIndex];
                selectedParticleCount = posData.particleCount;
            }
        }

        GUIContent applyChangesContent = new GUIContent("Apply Changes");
        Vector2 applyChangesPos = new Vector2();
        applyChangesPos.x = curPosX;
        applyChangesPos.y = curPosY;
        Vector2 applyChangesSize = buttonStyle.CalcSize(applyChangesContent);
        Rect applyChangesRect = new Rect();
        applyChangesRect.position = applyChangesPos;
        applyChangesRect.size = applyChangesSize;
        bool appliedChanges = GUI.Button(applyChangesRect, applyChangesContent, buttonStyle);

        curPosX += applyChangesRect.width;

        if (appliedChanges && selectedParticleAtlas != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(prefabInput);
            string assetGUID = AssetDatabase.AssetPathToGUID(assetPath);

            for (int i = 0; i < selectedParticleAtlas.posDataArray.Length; i++)
            {
                ParticlePosData posData = selectedParticleAtlas.posDataArray[i];
                
                if (posData.prefabGUID != assetGUID) continue;

                posData.posY = prefabInput.transform.position.y;
                posData.widthType = prefabInput.widthType;
                posData.spritesPerParticle = posData.widthType == ParticleWidthType.Slice ? 9 : 1;
                switch (selectedParticleAtlas.particleType)
                {
                    case ParticleType.Scroll:
                    {
                        posData.particleCount = 2;

                        switch (posData.widthType)
                        {
                            case ParticleWidthType.Simple:
                            {
                                posData.spriteIndex = prefabInput.mainRenderer.spriteIndex;

                                posData.scaleX = prefabInput.mainRenderer.width;
                                posData.scaleY = prefabInput.mainRenderer.height;
                                posData.quadScales = new Vector4[1];
                                Vector4 quadScale = new Vector4();
                                quadScale.x = posData.scaleX;
                                quadScale.y = posData.scaleY;
                                posData.quadScales[0] = quadScale;
                            }
                            break;
                            case ParticleWidthType.Tile:
                            {
                                posData.spriteIndex = prefabInput.mainRenderer.spriteIndex;

                                posData.scaleX = Mathf.Ceil(spawnData.bounds.size.x / prefabInput.mainRenderer.worldPivotAndSize.z);
                                posData.scaleY = prefabInput.mainRenderer.height;

                                posData.quadScales = new Vector4[1];
                                Vector4 quadScale = new Vector4();
                                quadScale.x = posData.scaleX;
                                quadScale.y = posData.scaleY;
                                posData.quadScales[0] = quadScale;
                            }
                            break;
                            case ParticleWidthType.Slice:
                            {
                                posData.spriteIndex = selectedParticleAtlas.atlas.simpleSprites.Length + (prefabInput.mainRenderer.spriteIndex * 9);

                                ParticleSpritesData leftSprite = selectedParticleAtlas.spriteData[posData.spriteIndex + 3];
                                ParticleSpritesData centerSprite = selectedParticleAtlas.spriteData[posData.spriteIndex + 4];
                                ParticleSpritesData rightSprite = selectedParticleAtlas.spriteData[posData.spriteIndex + 5];

                                posData.scaleX = (spawnData.bounds.size.x - leftSprite.worldPivotAndSize.z - rightSprite.worldPivotAndSize.z) / centerSprite.worldPivotAndSize.z;

                                SliceSprite sliceSprite = selectedParticleAtlas.atlas.slicedSprites[prefabInput.mainRenderer.spriteIndex];
                                posData.quadScales = GetQuadScalesFromSliceSprite(posData.scaleX, posData.scaleY, sliceSprite);
                            }
                            break;
                        }
                    }
                    break;
                    case ParticleType.Zone:
                    {
                        posData.spriteIndex = 0;
                        posData.particleCount = selectedParticleCount;
                        posData.quadCount = (uint)(posData.spritesPerParticle * posData.particleCount);

                        switch (posData.widthType)
                        {
                            case ParticleWidthType.Simple:
                            {
                                posData.scaleX = prefabInput.mainRenderer.width;
                                posData.scaleY = prefabInput.mainRenderer.height;

                                posData.quadScales = new Vector4[1];
                                Vector4 quadScale = new Vector4();
                                quadScale.x = posData.scaleX;
                                quadScale.y = posData.scaleY;
                                posData.quadScales[0] = quadScale;
                            }
                            break;

                            case ParticleWidthType.Tile:
                            {
                                posData.scaleX = prefabInput.mainRenderer.width;
                                posData.scaleY = prefabInput.mainRenderer.height;
                                posData.quadScales = new Vector4[selectedParticleCount];

                                for (int j = 0; j < posData.particleCount; j++)
                                {
                                    float randScaleX = UnityEngine.Random.Range(-randomInput.x, randomInput.x);
                                    float randScaleY = UnityEngine.Random.Range(-randomInput.y, randomInput.y);
                                    randScaleX = Mathf.Max(posData.scaleX + randScaleX, 1);
                                    randScaleY = Mathf.Max(posData.scaleY + randScaleY, 1);

                                    Vector4 quadScale = new Vector4();
                                    quadScale.x = randScaleX;
                                    quadScale.y = randScaleY;
                                    posData.quadScales[j] = quadScale;
                                }
                            }
                            break;

                            case ParticleWidthType.Slice:
                            {
                                posData.scaleX = prefabInput.mainRenderer.width;
                                posData.scaleY = prefabInput.mainRenderer.height;

                                posData.quadScales = new Vector4[posData.quadCount];
                                int totalIndex = 0;
                                for (int j = 0; j < posData.particleCount; j++)
                                {
                                    float randScaleX = UnityEngine.Random.Range(-randomInput.x, randomInput.x);
                                    float randScaleY = UnityEngine.Random.Range(-randomInput.y, randomInput.y);
                                    randScaleX = Mathf.Max(posData.scaleX + randScaleX, 1);
                                    randScaleY = Mathf.Max(posData.scaleY + randScaleY, 1);

                                    SliceSprite sliceSprite = selectedParticleAtlas.atlas.slicedSprites[prefabInput.mainRenderer.spriteIndex];

                                    Vector4[] quadScales = GetQuadScalesFromSliceSprite(randScaleX, randScaleY, sliceSprite);

                                    for (int k = 0; k < quadScales.Length; k++)
                                    {
                                        posData.quadScales[totalIndex] = quadScales[k];
                                        totalIndex++;
                                    }
                                }
                            }
                            break;
                        }
                }
                break;
                }

                if (prefabInput.preScrollerRenderers != null)
                {
                    EdgeScroller preScroller = new EdgeScroller();
                    preScroller.edgeSpriteDataArray = new EdgeSpriteData[prefabInput.preScrollerRenderers.Length];
                    float posXOffset = 0;

                    for (int j = 0; j < prefabInput.preScrollerRenderers.Length; j++)
                    {
                        AtlasRenderer preScrollerRenderer = prefabInput.preScrollerRenderers[j];

                        EdgeSpriteData preScrollerSpriteData = new EdgeSpriteData();
                        preScrollerSpriteData.spriteIndex = preScrollerRenderer.spriteIndex;

                        Vector4 offset = new Vector4();
                        offset.x = preScrollerRenderer.transform.localPosition.x;
                        offset.y = preScrollerRenderer.transform.localPosition.y;
                        offset.z = preScrollerRenderer.transform.localPosition.z;

                        preScrollerSpriteData.offset = offset;

                        Vector4 scale = new Vector4();
                        scale.x = preScrollerRenderer.width;
                        scale.y = preScrollerRenderer.height;

                        preScrollerSpriteData.scale = scale;

                        preScroller.edgeSpriteDataArray[j] = preScrollerSpriteData;

                        if (offset.x < posXOffset) posXOffset = offset.x;
                    }

                    posData.preScroller = preScroller;
                    posData.posX = -posXOffset;
                }
                if (prefabInput.postScrollersRenderers != null)
                {
                    EdgeScroller postScroller = new EdgeScroller();
                    postScroller.edgeSpriteDataArray = new EdgeSpriteData[prefabInput.postScrollersRenderers.Length];

                    float widthOffset;
                    if (posData.widthType == ParticleWidthType.Slice)
                    {
                        widthOffset = selectedParticleAtlas.spriteData[posData.spriteIndex].worldPivotAndSize.z;
                        widthOffset += posData.scaleX * selectedParticleAtlas.spriteData[posData.spriteIndex + 1].worldPivotAndSize.z;
                        widthOffset += selectedParticleAtlas.spriteData[posData.spriteIndex + 2].worldPivotAndSize.z;
                    }
                    else
                    {
                        widthOffset = posData.scaleX * selectedParticleAtlas.spriteData[posData.spriteIndex].worldPivotAndSize.z;
                    }

                    for (int j = 0; j < prefabInput.postScrollersRenderers.Length; j++)
                    {
                        AtlasRenderer postScrollerRenderer = prefabInput.postScrollersRenderers[j];

                        EdgeSpriteData postScrollerSpriteData = new EdgeSpriteData();
                        postScrollerSpriteData.spriteIndex = postScrollerRenderer.spriteIndex;

                        Vector4 offset = new Vector4();
                        offset.x = postScrollerRenderer.transform.localPosition.x + widthOffset;
                        offset.y = postScrollerRenderer.transform.localPosition.y;
                        offset.z = postScrollerRenderer.transform.localPosition.z;

                        postScrollerSpriteData.offset = offset;

                        Vector4 scale = new Vector4();
                        scale.x = postScrollerRenderer.width;
                        scale.y = postScrollerRenderer.height;

                        postScrollerSpriteData.scale = scale;

                        postScroller.edgeSpriteDataArray[j] = postScrollerSpriteData;
                    }

                    posData.postScroller = postScroller;
                }

                selectedParticleAtlas.posDataArray[i] = posData;
            }


        }

        curPosY += applyChangesSize.y;
    }
    private void DrawThirdRow()
    {
        if (trip == null || selectedParticleAtlas == null || selectedPosDataIndex == -1) return;
        
        curPosX = 0;

        switch (selectedParticleAtlas.particleType)
        {
            case ParticleType.Zone:
            {
                GUIContent countLabelContent = new GUIContent("Count");
                Vector2 countLabelPos = new Vector2();
                countLabelPos.x = curPosX;
                countLabelPos.y = curPosY;
                Vector2 countLabelSize = boldStyle.CalcSize(countLabelContent);
                Rect countLabelRect = new Rect();
                countLabelRect.position = countLabelPos;
                countLabelRect.size = countLabelSize;
                GUI.Label(countLabelRect, countLabelContent, boldStyle);

                curPosX += countLabelSize.x;

                Vector2 countInputPos = new Vector2();
                countInputPos.x = curPosX;
                countInputPos.y = curPosY;
                Vector2 countInputSize = new Vector2();
                countInputSize.x = SLIDER_FIELD_WIDTH;
                countInputSize.y = countLabelSize.y;
                Rect countInputRect = new Rect();
                countInputRect.position = countInputPos;
                countInputRect.size = countInputSize;

                curPosX += countInputSize.x;

                EditorGUI.BeginChangeCheck();
                selectedParticleCount = EditorGUI.IntSlider(countInputRect, selectedParticleCount, 1, ZONE_PARTICLE_COUNT);
                if (EditorGUI.EndChangeCheck())
                {
                    ParticlePosData selectedPosData = selectedParticleAtlas.posDataArray[selectedPosDataIndex];
                    selectedPosData.particleCount = selectedParticleCount;
                    selectedPosData = SetQuadScales(selectedPosData);
                    selectedParticleAtlas.posDataArray[selectedPosDataIndex] = selectedPosData;

                    SaveOrderedPosData();
                }

                ParticlePosData posData = selectedParticleAtlas.posDataArray[selectedPosDataIndex];
                if (posData.widthType == ParticleWidthType.Simple)
                {

                }
                else
                {
                    GUIContent randomLabelContent = new GUIContent("Random");
                    Vector2 randomLabelPos = new Vector2();
                    randomLabelPos.x = curPosX;
                    randomLabelPos.y = curPosY;
                    Vector2 randomLabelSize = boldStyle.CalcSize(randomLabelContent);
                    Rect randomLabelRect = new Rect();
                    randomLabelRect.position = randomLabelPos;
                    randomLabelRect.size = randomLabelSize;
                    GUI.Label(randomLabelRect, randomLabelContent, boldStyle);

                    curPosX += randomLabelSize.x;

                    Vector2 randomInputPos = new Vector2();
                    randomInputPos.x = curPosX;
                    randomInputPos.y = curPosY;
                    Vector2 randomInputSize = new Vector2();
                    randomInputSize.x = SLIDER_FIELD_WIDTH;
                    randomInputSize.y = randomLabelSize.y;
                    Rect randomInputRect = new Rect();
                    randomInputRect.position = randomInputPos;
                    randomInputRect.size = randomInputSize;

                    curPosX += randomInputSize.x;
                    EditorGUI.BeginChangeCheck();
                    randomInput = EditorGUI.Vector2Field(randomInputRect, GUIContent.none, randomInput);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ParticlePosData selectedPosData = selectedParticleAtlas.posDataArray[selectedPosDataIndex];
                        selectedPosData.randomFactor = randomInput;
                        selectedPosData.quadScales = new Vector4[selectedPosData.quadCount];
                        int totalIndex = 0;

                        switch (posData.widthType)
                        {
                            case ParticleWidthType.Tile:
                            {
                                for (int i = 0; i < selectedPosData.particleCount; i++)
                                {
                                    float randScaleX = UnityEngine.Random.Range(-randomInput.x, randomInput.x);
                                    float randScaleY = UnityEngine.Random.Range(-randomInput.y, randomInput.y);
                                    randScaleX = Mathf.Max(selectedPosData.scaleX + randScaleX, 1);
                                    randScaleY = Mathf.Max(selectedPosData.scaleY + randScaleY, 1);

                                    Vector4 quadScales = new Vector4();
                                    quadScales.x = randScaleX;
                                    quadScales.y = randScaleY;

                                    for (int k = 0; k < posData.quadScales.Length; k++)
                                    {
                                        selectedPosData.quadScales[totalIndex] = quadScales;
                                        totalIndex++;
                                    }
                                }
                            }
                            break;

                            case ParticleWidthType.Slice:
                            {
                                for (int i = 0; i < selectedPosData.particleCount; i++)
                                {
                                    float randScaleX = UnityEngine.Random.Range(-randomInput.x, randomInput.x);
                                    float randScaleY = UnityEngine.Random.Range(-randomInput.y, randomInput.y);
                                    randScaleX = Mathf.Max(selectedPosData.scaleX + randScaleX, 1);
                                    randScaleY = Mathf.Max(selectedPosData.scaleY + randScaleY, 1);

                                    int centerSpriteIndex = (totalIndex + 4) % selectedParticleAtlas.spriteData.Length;
                                    ParticleSpritesData centerSprite = selectedParticleAtlas.spriteData[centerSpriteIndex];
                                    Vector4[] quadScales = GetQuadScalesFromParticleSpriteData(randScaleX, randScaleY, centerSprite);

                                    for (int k = 0; k < quadScales.Length; k++)
                                    {
                                        selectedPosData.quadScales[totalIndex] = quadScales[k];
                                        totalIndex++;
                                    }
                                }
                            }
                            break;
                        }
                        selectedParticleAtlas.posDataArray[selectedPosDataIndex] = selectedPosData;
                    }
                }
                break;
            }
        }
        GUIContent widthTypeLabelContent = new GUIContent("Width Type");
        Vector2 widthTypePos = new Vector2();
        widthTypePos.x = curPosX;
        widthTypePos.y = curPosY;
        Vector2 widthTypeLabelSize = boldStyle.CalcSize(widthTypeLabelContent);
        Rect widthTypeLabelRect = new Rect();
        widthTypeLabelRect.position = widthTypePos;
        widthTypeLabelRect.size = widthTypeLabelSize;
        curPosX += widthTypeLabelSize.x;

        EditorGUI.LabelField(widthTypeLabelRect, widthTypeLabelContent, boldStyle);

        Vector2 widthTypeInputPos = new Vector2();
        widthTypeInputPos.x = curPosX;
        widthTypeInputPos.y = curPosY;
        Vector2 widthTypeInputSize = new Vector2();
        widthTypeInputSize.x = SLIDER_FIELD_WIDTH;
        widthTypeInputSize.y = widthTypeLabelSize.y;
        Rect widthTypeInputRect = new Rect();
        widthTypeInputRect.position = widthTypeInputPos;
        widthTypeInputRect.size = widthTypeInputSize;
        curPosX += widthTypeInputSize.x;

        EditorGUI.BeginChangeCheck();
        selectedWidthType = (ParticleWidthType)EditorGUI.EnumPopup(widthTypeInputRect, selectedWidthType);
        if (EditorGUI.EndChangeCheck())
        {
            ParticlePosData selectedPosData = selectedParticleAtlas.posDataArray[selectedPosDataIndex];
            selectedPosData.widthType = selectedWidthType;
            selectedPosData = SetQuadScales(selectedPosData);
            selectedParticleAtlas.posDataArray[selectedPosDataIndex] = selectedPosData;
        }

        curPosY += widthTypeInputSize.y;
    }
    private void DrawGraph()
    {
        if (trip == null || trip.stationsDataArray.Length == 0) return;

        curPosX = 0;

        float scrollWindowWidth = position.width - (PADDING * 2);
        float graphWidth = scrollWindowWidth - PADDING;
        float graphColWidth = graphWidth / (float)trip.stationsDataArray.Length;

        float stationHeaderHeight = 0;
        curPosX = PADDING;
        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO station = trip.stationsDataArray[i];

            GUIContent stationHeaderContent = new GUIContent(station.name);
            Vector2 stationHeaderPos = new Vector2();
            stationHeaderPos.x = curPosX;
            stationHeaderPos.y = curPosY;
            stationHeaderHeight = headerStyle.CalcHeight(stationHeaderContent, graphColWidth);
            Vector2 stationHeaderSize = new Vector2();
            stationHeaderSize.x = graphColWidth;
            stationHeaderSize.y = stationHeaderHeight;
            Rect stationHeaderRect = new Rect();
            stationHeaderRect.position = stationHeaderPos;
            stationHeaderRect.size = stationHeaderSize;
            GUI.Label(stationHeaderRect, stationHeaderContent, headerStyle);

            curPosX += graphColWidth;
        }

        curPosX = PADDING;
        curPosY += stationHeaderHeight;


        GUIContent kmphLabelContent = new GUIContent("KMPH");
        Vector2 kmphLabelSize = leftPanelStyle.CalcSize(kmphLabelContent);
        Vector2 kmphLabelPos = new Vector2();
        kmphLabelPos.x = curPosX - kmphLabelSize.x;
        kmphLabelPos.y = curPosY;
        Rect kmphLabelRect = new Rect();
        kmphLabelRect.position = kmphLabelPos;
        kmphLabelRect.size = kmphLabelSize;
        GUI.Label(kmphLabelRect, kmphLabelContent, leftPanelStyle);

        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            Vector2 kmphInputPos = new Vector2();
            kmphInputPos.x = curPosX;
            kmphInputPos.y = curPosY;
            Vector2 kmphInputSize = new Vector2();
            kmphInputSize.x = graphColWidth;
            kmphInputSize.y = kmphLabelSize.y;
            Rect kmphInputRect = new Rect();
            kmphInputRect.position = kmphInputPos;
            kmphInputRect.size = kmphInputSize;

            EditorGUI.BeginChangeCheck();
            kmphInputValues[i] = EditorGUI.Slider(kmphInputRect, kmphInputValues[i], 10, 50);
            if(EditorGUI.EndChangeCheck())
            {
                trip.kmValues[i] = kmphInputValues[i];
            }
            curPosX += graphColWidth;
        }

        curPosX = PADDING;
        curPosY += kmphLabelSize.y;

        GUIContent dayNightLabelContent = new GUIContent("Day/Night");
        Vector2 dayNightLabelSize = leftPanelStyle.CalcSize(dayNightLabelContent);
        Vector2 dayNightLabelPos = new Vector2();
        dayNightLabelPos.x = curPosX - dayNightLabelSize.x;
        dayNightLabelPos.y = curPosY;
        Rect dayNightLabelRect = new Rect();
        dayNightLabelRect.position = dayNightLabelPos;
        dayNightLabelRect.size = dayNightLabelSize;
        GUI.Label(dayNightLabelRect, dayNightLabelContent, leftPanelStyle);

        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            Vector2 dayNightInputPos = new Vector2();
            dayNightInputPos.x = curPosX;
            dayNightInputPos.y = curPosY;
            Vector2 dayNightInputSize = new Vector2();
            dayNightInputSize.x = graphColWidth;
            dayNightInputSize.y = dayNightLabelSize.y;
            Rect dayNightInputRect = new Rect();
            dayNightInputRect.position = dayNightInputPos;
            dayNightInputRect.size = dayNightInputSize;

            EditorGUI.BeginChangeCheck();
            dayNightInputValues[i] = EditorGUI.Slider(dayNightInputRect, dayNightInputValues[i], 0, 1);
            if (EditorGUI.EndChangeCheck())
            {
                trip.dayNightValues[i] = dayNightInputValues[i];
            }
            curPosX += graphColWidth;
        }

        curPosX = 0;
        curPosY += dayNightLabelSize.y;

        Vector2 graphWindowPos = new Vector2();
        graphWindowPos.x = curPosX;
        graphWindowPos.y = curPosY;
        Vector2 graphWindowSize = new Vector2();
        graphWindowSize.x = position.width;
        graphWindowSize.y = position.height - curPosY;
        Rect graphWindowRect = new Rect();
        graphWindowRect.position = graphWindowPos;
        graphWindowRect.size = graphWindowSize;

        Vector2 graphScrollPos = new Vector2();
        graphScrollPos.x = curPosX;
        graphScrollPos.y = curPosY;
        Vector2 graphScrollSize = new Vector2();
        graphScrollSize.x = scrollWindowWidth;
        graphScrollSize.y = FULL_RECT_HEIGHT;
        Rect graphScrollRect = new Rect();
        graphScrollRect.position = graphScrollPos;
        graphScrollRect.size = graphScrollSize;

        float graphRowHeight = graphScrollRect.height / FAR_CLIP;

        graphScroll = GUI.BeginScrollView(graphWindowRect, graphScroll, graphScrollRect);
        Handles.BeginGUI();

        float trainWorldPosZIndex = TRAIN_DEPTH_MIN - 1;
        float trainWorldSizeZIndex = TRAIN_DEPTH_MAX - trainWorldPosZIndex;
        Vector2 trainAreaPos = new Vector2();
        trainAreaPos.x = PADDING + graphScrollRect.xMin;
        trainAreaPos.y = graphScrollRect.yMin + (trainWorldPosZIndex * graphRowHeight);
        Vector2 trainAreaSize = new Vector2();
        trainAreaSize.x = graphWidth;
        trainAreaSize.y = trainWorldSizeZIndex * graphRowHeight;
        Rect trainAreaRect = new Rect();
        trainAreaRect.position = trainAreaPos;
        trainAreaRect.size = trainAreaSize;
        Handles.DrawSolidRectangleWithOutline(trainAreaRect, Color.darkRed, Color.clear);

        Handles.DrawSolidRectangleWithOutline(graphScrollRect, Color.clear, Color.cyan);

        Handles.color = Color.white;
        
        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            float colLinePosX = PADDING + (graphColWidth * i);
            Vector2 colLineTopPos = new Vector2();
            colLineTopPos.x = colLinePosX;
            colLineTopPos.y = graphScrollRect.yMin;
            Vector2 colLineBottomPos = new Vector2();   
            colLineBottomPos.x = colLinePosX;
            colLineBottomPos.y = graphScrollRect.yMax;
            Handles.DrawLine(colLineTopPos, colLineBottomPos);
        }

        for (int i = 0; i < FAR_CLIP; i++)
        {
            float rowPosY = graphScrollRect.yMin + (graphRowHeight * i);
            Vector2 rowLeftPos = new Vector2();
            rowLeftPos.x = graphScrollRect.xMin + PADDING;
            rowLeftPos.y = rowPosY;
            Vector2 rowRightPos = new Vector2();
            rowRightPos.x = graphScrollRect.xMax;
            rowRightPos.y = rowPosY;
            Handles.DrawLine(rowLeftPos, rowRightPos);

            string posZLabelText = "";
            int posZ = i + 1;
            switch (posZ)
            {
                case TRAIN_LINE_DEPTH:
                {
                    posZLabelText = "LINE";
                }
                break;

                case TRAIN_TRACKS_DEPTH:
                {
                    posZLabelText = "TRACKS";

                }
                break;

                default:
                {
                    posZLabelText = posZ.ToString();
                }
                break;
            }
            GUIContent posZLabelContent = new GUIContent(posZLabelText);
            Vector2 posZLabelSize = leftPanelStyle.CalcSize(posZLabelContent);
            rowLeftPos.x -= posZLabelSize.x;
            Rect posZLabelRect = new Rect();
            posZLabelRect.position = rowLeftPos;
            posZLabelRect.size = posZLabelSize;
            GUI.Label(posZLabelRect, posZLabelContent, leftPanelStyle);
        }

        Event e = Event.current;
        bool clickedBar = false;
        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];

            if (particleAtlas.posDataArray == null) continue;

            for (int j = 0; j < particleAtlas.posDataArray.Length; j++)
            {
                ParticlePosData posData = particleAtlas.posDataArray[j];

                float stationLength = posData.endStation - posData.startStation;
                Vector2 barPos = new Vector2();
                barPos.x = PADDING + (posData.startStation * graphColWidth);
                barPos.y = graphScrollRect.yMin + (posData.posZ - 1) * graphRowHeight;
                Vector2 barSize = new Vector2();
                barSize.x = stationLength * graphColWidth;
                barSize.y = posData.depthSize * graphRowHeight;
                Rect barRect = new Rect();
                barRect.position = barPos;
                barRect.size = barSize;

                GUID posDataGuid = new GUID(posData.prefabGUID);
                TripEditorPrefab posDataPrefab = (TripEditorPrefab)AssetDatabase.LoadAssetByGUID(posDataGuid, typeof(TripEditorPrefab));

                Vector2 thirdSize = new Vector2();
                thirdSize.x = barRect.size.x / 3;
                thirdSize.y = barRect.size.y / 3;

                if (e.type == EventType.MouseDown && barRect.Contains(e.mousePosition))
                {
                    selectedParticleAtlas = trip.particleAtlasArray[i];
                    selectedPosDataIndex = j;
                    clickedBar = true;

                    clickMousePosX = (int)((e.mousePosition.x - graphScrollRect.xMin) / graphColWidth);
                    clickMousePosY = (int)((e.mousePosition.y - graphScrollRect.yMin) / graphRowHeight) + 1;

                    clickStartStation = posData.startStation;
                    clickEndStation = posData.endStation;
                    clickPosZ = posData.posZ;
                    clickDepthSize = posData.depthSize;

                    adjustState = AdjustState.None;

                    switch (particleAtlas.particleType)
                    {
                        case ParticleType.Zone:
                        {
                            if (e.mousePosition.y < (barRect.min.y + thirdSize.y))
                            {
                                adjustState |= AdjustState.TopSize;
                            }
                            else if (e.mousePosition.y > (barRect.max.y - thirdSize.y))
                            {
                                adjustState |= AdjustState.BottomSize;
                            }

                            if (e.mousePosition.x < (barRect.min.x + thirdSize.x))
                            {
                                adjustState |= AdjustState.LeftSize;
                            }
                            else if (e.mousePosition.x > (barRect.max.x - thirdSize.x))
                            {
                                adjustState |= AdjustState.RightSize;
                            }

                            if (adjustState == AdjustState.None)
                            {
                                adjustState = AdjustState.Position;
                            }
                        }
                        break;

                        case ParticleType.Scroll:
                        {
                            if (e.mousePosition.x < (barRect.min.x + thirdSize.x))
                            {
                                adjustState |= AdjustState.LeftSize;
                            }
                            else if (e.mousePosition.x > (barRect.max.x - thirdSize.x))
                            {
                                adjustState |= AdjustState.RightSize;
                            }

                            if (adjustState == AdjustState.None)
                            {
                                adjustState = AdjustState.Position;
                            }
                        }
                        break;
                    }

                    selectedParticleCount = posData.particleCount;
                    selectedWidthType = posData.widthType;
                    randomInput = posData.randomFactor;

                    prefabInput = posDataPrefab;
                    Repaint();
                }

                if (selectedPosDataIndex == j && selectedParticleAtlas == trip.particleAtlasArray[i])
                {
                    if (e.type == EventType.MouseDrag)
                    {
                        int dragMousePosX = (int)((e.mousePosition.x - graphScrollRect.xMin) / graphColWidth);
                        int dragMousePosY = (int)((e.mousePosition.y - graphScrollRect.yMin) / graphRowHeight) + 1;

                        if ((adjustState & AdjustState.LeftSize) != 0)
                        {
                            int curStationStart = dragMousePosX;
                            posData.startStation = Mathf.Clamp(curStationStart, 0, posData.endStation - 1);
                        }
                        else if ((adjustState & AdjustState.RightSize) != 0)
                        {
                            int curEndStation = dragMousePosX;
                            posData.endStation = Mathf.Clamp(curEndStation, posData.startStation + 1, trip.stationsDataArray.Length);
                        }
                        
                        if ((adjustState & AdjustState.TopSize) != 0)
                        {
                            int curPosZ = clickPosZ + (dragMousePosY - clickMousePosY);
                            int curDepthSize = clickDepthSize + (clickMousePosY - dragMousePosY);

                            posData.posZ = Mathf.Clamp(curPosZ, 1, (int)FAR_CLIP - 1);
                            posData.depthSize = Mathf.Clamp(curDepthSize, 1, (int)FAR_CLIP - posData.posZ);
                        }
                        else if ((adjustState & AdjustState.BottomSize) != 0)
                        {
                            int curDepthSize = clickDepthSize - (clickMousePosY - dragMousePosY);
                            posData.depthSize = Mathf.Clamp(curDepthSize, 1, (int)FAR_CLIP - posData.posZ);
                        }

                        if (adjustState == AdjustState.Position)
                        {
                            int curPosZ = clickPosZ + (dragMousePosY - clickMousePosY);
                            posData.posZ = Mathf.Clamp(curPosZ, 1, (int)FAR_CLIP - 1);

                            int curStationStart = clickStartStation + (dragMousePosX - clickMousePosX);
                            posData.startStation = curStationStart;

                            int curStationEnd = clickEndStation + (dragMousePosX - clickMousePosX);
                            posData.endStation = Mathf.Clamp(curStationEnd, posData.startStation + 1, trip.stationsDataArray.Length);
                        }
                        particleAtlas.posDataArray[j] = posData;

                        SaveOrderedPosData();
                        Repaint();
                    }

                    Handles.DrawSolidRectangleWithOutline(barRect, Color.white, Color.black);
                    Color prevHandleColor = Handles.color;
                    Handles.color = Color.black;

                    Vector2 firstThirdColTop = new Vector2();
                    firstThirdColTop.x = barRect.xMin + thirdSize.x;
                    firstThirdColTop.y = barRect.yMin;

                    Vector2 firstThirdColBottom = new Vector2();
                    firstThirdColBottom.x = barRect.xMin + thirdSize.x;
                    firstThirdColBottom.y = barRect.yMax;

                    Vector2 secondThirdColTop = new Vector2();
                    secondThirdColTop.x = barRect.xMax - thirdSize.x;
                    secondThirdColTop.y = barRect.yMin;

                    Vector2 secondThirdColBottom = new Vector2();
                    secondThirdColBottom.x = barRect.xMax - thirdSize.x;
                    secondThirdColBottom.y = barRect.yMax;

                    Handles.DrawLine(firstThirdColTop, firstThirdColBottom);
                    Handles.DrawLine(secondThirdColTop, secondThirdColBottom);

                    if (selectedParticleAtlas.particleType == ParticleType.Zone)
                    {

                        Vector2 firstThirdRowLeft = new Vector2();
                        firstThirdRowLeft.x = barRect.xMin;
                        firstThirdRowLeft.y = barRect.yMin + thirdSize.y;

                        Vector2 firstThirdRowRight = new Vector2();
                        firstThirdRowRight.x = barRect.xMax;
                        firstThirdRowRight.y = barRect.yMin + thirdSize.y;

                        Vector2 secondThirdRowLeft = new Vector2();
                        secondThirdRowLeft.x = barRect.xMin;
                        secondThirdRowLeft.y = barRect.yMax - thirdSize.y;

                        Vector2 secondThirdRowRight = new Vector2();
                        secondThirdRowRight.x = barRect.xMax;
                        secondThirdRowRight.y = barRect.yMax - thirdSize.y;

                        Handles.DrawLine(firstThirdRowLeft, firstThirdRowRight);
                        Handles.DrawLine(secondThirdRowLeft, secondThirdRowRight);
                    }
                    Handles.color = prevHandleColor;
                    GUIContent posDataLabelContent = new GUIContent(posDataPrefab.name);
                    GUI.Label(barRect, posDataLabelContent, selectedStyle);
                }
                else
                {
                    Handles.DrawSolidRectangleWithOutline(barRect, particleAtlasColors[i], Color.black);
                    GUIContent posDataLabelContent = new GUIContent(posDataPrefab.name);
                    GUI.Label(barRect, posDataLabelContent, boldStyle);
                }

            }
        }
        if (e.type == EventType.MouseDown && !clickedBar)
        {
            selectedPosDataIndex = -1;
            if (prefabInput)
            {
                SetAtlasDict();
                selectedParticleAtlas = atlasParticleAtlasDict[prefabInput.mainRenderer.atlas];
            }
            else
            {
                selectedParticleAtlas = null;
            }
            Repaint();
        }
        Handles.EndGUI();
        GUI.EndScrollView();
    }
    private void SaveOrderedPosData()
    {
        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];

            ParticlePosData[] ordered = particleAtlas.posDataArray.OrderBy(p => p.startStation).ThenBy(p => p.posZ).ToArray();

            particleAtlas.posDataArray = ordered;
        }

        SetParticleIndices(ParticleType.Zone, ZONE_PARTICLE_COUNT);
        SetParticleIndices(ParticleType.Scroll, SCROLL_PARTICLE_COUNT);

        SavePrevDepthIndices();
    }
    private void SavePrevDepthIndices()
    {
        Dictionary<int, ParticlePosData> latestDepthDict = new Dictionary<int, ParticlePosData>();
        List<(ParticleAtlas atlas, int index, ParticlePosData posData)> allPosData = new();

        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas atlas = trip.particleAtlasArray[i];

            for (int j = 0; j < atlas.posDataArray.Length; j++)
            {
                allPosData.Add((atlas, j, atlas.posDataArray[j]));
            }
        }

        allPosData = allPosData.OrderBy(p => p.posData.startStation).ToList();

        foreach (var entry in allPosData)
        {
            ParticlePosData posData = entry.posData;

            if (latestDepthDict.TryGetValue(posData.posZ, out ParticlePosData prevPosData))
            {
                posData.prevDepthIndices = new Vector2Int(prevPosData.minParticleIndex,prevPosData.maxParticleIndex);
            }
            else
            {
                posData.prevDepthIndices = new Vector2Int(0, 0);
            }

            entry.atlas.posDataArray[entry.index] = posData;
            
            latestDepthDict[posData.posZ] = posData;
        }
    }
    private void SetParticleIndices(ParticleType particleType, int maxParticleCount)
    {
        int minParticle = 0;
        int maxParticle = -1;

        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            for (int j = 0; j < trip.particleAtlasArray.Length; j++)
            {
                ParticleAtlas particleAtlas = trip.particleAtlasArray[j];

                if (particleAtlas.particleType != particleType) continue;

                for (int k = 0; k < particleAtlas.posDataArray.Length; k++)
                {
                    ParticlePosData posData = particleAtlas.posDataArray[k];

                    if (posData.startStation != i) continue;

                    maxParticle += (int)posData.particleCount;
                    maxParticle %= maxParticleCount;

                    posData.minParticleIndex = minParticle;
                    posData.maxParticleIndex = maxParticle;

                    particleAtlas.posDataArray[k] = posData;
                    minParticle = maxParticle + 1;
                    minParticle %= maxParticleCount;
                }
            }
        }
    }
    private ParticlePosData SetQuadScales(ParticlePosData posData)
    {
        posData.quadCount = (uint)(posData.spritesPerParticle * posData.particleCount);

        switch (selectedParticleAtlas.particleType)
        {
            case ParticleType.Scroll:
            {
                switch (posData.widthType)
                {
                    case ParticleWidthType.Simple:
                    {
                        posData.spriteIndex = prefabInput.mainRenderer.spriteIndex;

                        posData.scaleX = prefabInput.mainRenderer.width;
                        posData.scaleY = prefabInput.mainRenderer.height;

                        posData.quadScales = new Vector4[1];
                        Vector4 quadScale = new Vector4();
                        quadScale.x = posData.scaleX;
                        quadScale.y = posData.scaleY;
                        posData.quadScales[0] = quadScale;
                    }
                    break;
                    case ParticleWidthType.Tile:
                    {
                        posData.spriteIndex = prefabInput.mainRenderer.spriteIndex;
                        posData.scaleX = Mathf.Ceil(spawnData.bounds.size.x / prefabInput.mainRenderer.worldPivotAndSize.z);
                        posData.scaleY = prefabInput.mainRenderer.height;

                        posData.quadScales = new Vector4[1];
                        Vector4 quadScale = new Vector4();
                        quadScale.x = posData.scaleX;
                        quadScale.y = posData.scaleY;
                        posData.quadScales[0] = quadScale;
                    }
                    break;
                    case ParticleWidthType.Slice:
                    {

                        posData.spriteIndex = selectedParticleAtlas.atlas.simpleSprites.Length + (prefabInput.mainRenderer.spriteIndex * 9);

                        ParticleSpritesData leftSprite = selectedParticleAtlas.spriteData[posData.spriteIndex + 3];
                        ParticleSpritesData centerSprite = selectedParticleAtlas.spriteData[posData.spriteIndex + 4];
                        ParticleSpritesData rightSprite = selectedParticleAtlas.spriteData[posData.spriteIndex + 5];

                        posData.scaleX = (spawnData.bounds.size.x - leftSprite.worldPivotAndSize.z - rightSprite.worldPivotAndSize.z) / centerSprite.worldPivotAndSize.z;

                        SliceSprite sliceSprite = selectedParticleAtlas.atlas.slicedSprites[prefabInput.mainRenderer.spriteIndex];
                        posData.quadScales = GetQuadScalesFromSliceSprite(posData.scaleX, posData.scaleY, sliceSprite);
                    }
                    break;
                }
            }
            break;
            case ParticleType.Zone:
            {
                switch (posData.widthType)
                {
                    case ParticleWidthType.Simple:
                    {
                        posData.scaleX = prefabInput.mainRenderer.width;
                        posData.scaleY = prefabInput.mainRenderer.height;

                        posData.quadScales = new Vector4[1];
                        Vector4 quadScale = new Vector4();
                        quadScale.x = posData.scaleX;
                        quadScale.y = posData.scaleY;
                        posData.quadScales[0] = quadScale;
                    }
                    break;

                    case ParticleWidthType.Tile:
                    {
                        posData.scaleX = prefabInput.mainRenderer.width;
                        posData.scaleY = prefabInput.mainRenderer.height;
                        posData.quadScales = new Vector4[selectedParticleCount];

                        for (int i = 0; i < posData.particleCount; i++)
                        {
                            Vector4 quadScale = new Vector4();
                            quadScale.x = posData.scaleX;
                            quadScale.y = posData.scaleY;
                            posData.quadScales[i] = quadScale;
                        }
                    }
                    break;

                    case ParticleWidthType.Slice:
                    {
                        posData.scaleX = prefabInput.mainRenderer.width;
                        posData.scaleY = prefabInput.mainRenderer.height;

                        posData.quadScales = new Vector4[posData.quadCount];
                        int totalIndex = 0;
                        for (int i = 0; i < posData.particleCount; i++)
                        {
                            int centerSpriteIndex = (totalIndex + 4) % selectedParticleAtlas.spriteData.Length;
                            ParticleSpritesData centerSprite = selectedParticleAtlas.spriteData[centerSpriteIndex];
                            Vector4[] quadScales = GetQuadScalesFromParticleSpriteData(posData.scaleX, posData.scaleY, centerSprite);
                            for (int j = 0; j < posData.spritesPerParticle; j++)
                            {
                                posData.quadScales[totalIndex] = quadScales[j];
                                totalIndex++;
                            }
                        }
                    }
                    break;
                }
            }
            break;
        }

        return posData;
    }

    private void SetAtlasDict()
    {
        if (atlasParticleAtlasDict != null) return;
        atlasParticleAtlasDict = new Dictionary<AtlasSO, ParticleAtlas>();

        particleAtlasColors = new Color[trip.particleAtlasArray.Length];
        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];
            particleAtlasColors[i] = Color.HSVToRGB((float)i / (float)particleAtlasColors.Length, 1, 1);
            particleAtlasColors[i].a = 0.5f;

            atlasParticleAtlasDict[particleAtlas.atlas] = particleAtlas;
        }
    }
}