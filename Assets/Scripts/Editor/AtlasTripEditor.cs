using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

using static Atlas;
using static AtlasRendering;
using static AtlasSpawn;

public class AtlasTripEditor : EditorWindow
{
    const float ROW_HEIGHT = 20;
    const float COLUMN_WIDTH = 150;
    const float GRAPH_POS_Y = 100;
    const float PADDING = 70;
    const float SIDE_PANEL_WIDTH = 250;
    const float EDGE_SCROLLER_INPUT_AMOUNT = 3;
    const float FULL_RECT_HEIGHT = 3000;

    const int STATION_LABEL_ROW = 1;
    const int NPC_GRADIENT_ROW = 2;
    const int KM_LABEL_ROW = 3;
    const int DAY_NIGHT_ROW = 4;
    const int ELEVATION_ROW = 5;

    static SpawnData spawnData;
    
    GUIStyle depthLabel;
    GUIStyle graphHeaderLabel;

    Gradient npcGradient;

    TripSO trip;

    ParticleAtlas selectedParticleAtlas;
    
    TripEditorPrefab selectedPrefab;

    Color[] particleAtlasColors;

    Vector2[] selectedElevationValues;

    float[] selectedKMValues;
    float[] selectedDayNightValues;

    Rect fullRect;
    Rect graphScrollRect;
    Rect graphRect;
    Rect sidePanelScrollRect;
    Rect sidePanelRect;

    ParticleWidthType selectedWidthType;

    Vector2 graphScroll;
    Vector2 sidePanelScroll;

    int selectedParticleCount;
    int selectedPosDataIndex;
    int curPosY;
    int totalTicketChecks;
    int dragOffsetWidth;
    int dragOffsetHeight;
    int dragOffsetStartX;
    int dragOffsetStartY;

    float selectedRandScale;
    float graphRowHeight;

    bool adjustingBar;
    bool isAdjustingMetersStart;
    bool isAdjustingDepthSize;
    bool selectedElevation;

    Dictionary<AtlasSO, ParticleAtlas> atlasToParticleAtlasDict;

    [MenuItem("Tools/Atlas Trip Editor")]
    private static void Open()
    {
        AtlasTripEditor tripEditor = GetWindow<AtlasTripEditor>("Atlas Trip Editor");
        string spawnDataPath = AssetDatabase.GUIDToAssetPath("cb76807a71f74704a94a0f5cfe3b2c50");
        if (Directory.Exists(spawnDataPath))
        {
            SpawnData foundSpawnData = AssetDatabase.LoadAssetAtPath<SpawnData>(spawnDataPath);
            if (foundSpawnData != null)
            {
                spawnData = foundSpawnData;
            }
        }
    }
    private void OnGUI()
    {
        Init();

        DrawFirstHeader();
        DrawSecondHeader();
        DrawThirdHeader();
        DrawTripSettings();

        DrawGraph();
        DrawSidePanel();
    }
    public void Init()
    {
        if (spawnData == null)
        {
            string spawnDataPath = AssetDatabase.GUIDToAssetPath("cb76807a71f74704a94a0f5cfe3b2c50");
            
            if (!AssetDatabase.AssetPathExists(spawnDataPath)) return;
            SpawnData foundSpawnData = AssetDatabase.LoadAssetAtPath<SpawnData>(spawnDataPath);
            if (foundSpawnData == null) return;
            spawnData = foundSpawnData;
        }

        fullRect = new Rect(0, 0, position.width, FULL_RECT_HEIGHT);

        float doublePadding = PADDING * 2;
        Vector2 graphPos = new Vector2(PADDING, GRAPH_POS_Y);
        Vector2 graphSize = new Vector2(position.width - SIDE_PANEL_WIDTH - (PADDING * 2), FULL_RECT_HEIGHT - GRAPH_POS_Y);

        graphRect = new Rect(graphPos, graphSize);
        graphScrollRect = new Rect(0, graphRect.y + 100, graphRect.width + doublePadding, position.height - (graphRect.y + GRAPH_POS_Y));

        Vector2 sidePanelPos = new Vector2(graphRect.xMax + doublePadding, graphRect.y);
        Vector2 sidePanelSize = new Vector2(SIDE_PANEL_WIDTH - doublePadding, fullRect.height);

        sidePanelRect = new Rect(sidePanelPos, sidePanelSize);
        sidePanelScrollRect = new Rect(0, graphScrollRect.y, position.width, position.height);

        graphRowHeight = graphRect.height / FAR_CLIP;

        depthLabel = new GUIStyle();
        depthLabel.alignment = TextAnchor.MiddleRight;
        depthLabel.normal.textColor = Color.white;
        depthLabel.fontSize = 16;

        graphHeaderLabel = new GUIStyle();
        graphHeaderLabel.alignment = TextAnchor.LowerLeft;
        graphHeaderLabel.normal.textColor = Color.white;
        graphHeaderLabel.fontSize = 16;
        graphHeaderLabel.fontStyle = FontStyle.Normal;

        curPosY = 0;

        if (trip != null)
        {
            totalTicketChecks = 0;
            for (int i = 0; i < trip.stationsDataArray.Length; i++)
            {
                StationSO station = trip.stationsDataArray[i];
                totalTicketChecks += station.ticketsToCheckBeforeSpawn;
            }
            trip.dayNightValues = CorrectArrayLength(trip.dayNightValues);
            trip.elevationValues = CorrectArrayLength(trip.elevationValues);
            trip.kmValues = CorrectArrayLength(trip.kmValues);

            selectedDayNightValues = trip.dayNightValues;
            selectedElevationValues = trip.elevationValues;
            selectedKMValues = trip.kmValues;

            particleAtlasColors = new Color[trip.particleAtlasArray.Length];
            for (int i = 0; i < particleAtlasColors.Length; i++)
            {
                particleAtlasColors[i] = Color.HSVToRGB((float)i / (float)particleAtlasColors.Length, 1, 1);
                particleAtlasColors[i].a = 0.5f;
            }
            npcGradient = new Gradient();
        }
    }
    private T[] CorrectArrayLength<T>(T[] array)
    {
        if (array ==  null)
        {
            return new T[totalTicketChecks];
        }

        int diff = array.Length - totalTicketChecks;


        List<T> list = array.ToList();
        if (diff < 0)
        {
            for (int i = 0; i < -diff; i++)
            {
                T t = array[i];
                list.Add(t);
            }
        }
        else if (diff > 0)
        {
            int startRemoveIndex = array.Length - diff;
            list.RemoveRange(startRemoveIndex, diff);
        }

        return list.ToArray();
    }
    private void DrawFirstHeader()
    {
        Rect tripLabelRect = new Rect(0, curPosY, graphRect.x, ROW_HEIGHT);
        GUIContent tripContent = new GUIContent("Trip");
        EditorGUI.LabelField(tripLabelRect, tripContent);

        Rect tripInputRect = new Rect(graphRect.x, curPosY, COLUMN_WIDTH, ROW_HEIGHT);
        
        EditorGUI.BeginChangeCheck();
        trip = (TripSO)EditorGUI.ObjectField(tripInputRect, GUIContent.none, trip, typeof(TripSO), allowSceneObjects: false);

        if (EditorGUI.EndChangeCheck())
        {
            if (trip == null) return;
            totalTicketChecks = 0;
            for (int i = 0; i < trip.stationsDataArray.Length; i++)
            {
                StationSO station = trip.stationsDataArray[i];
                totalTicketChecks += station.ticketsToCheckBeforeSpawn;
            }
        }

        curPosY += (int)ROW_HEIGHT;
    }
    private void DrawSecondHeader()
    {
        if (trip == null) return;

        float curPosX = graphRect.x;

        Rect atlasOptionsLabelRect = new Rect(0, curPosY, graphRect.x, ROW_HEIGHT);
        EditorGUI.LabelField(atlasOptionsLabelRect, "Array");

        Rect prefabLabelRect = new Rect(curPosX, curPosY, COLUMN_WIDTH, ROW_HEIGHT);
        curPosX += prefabLabelRect.width;
        EditorGUI.LabelField(prefabLabelRect, "Prefab");


        Rect selectedPrefabRect = new Rect(curPosX, curPosY, COLUMN_WIDTH, ROW_HEIGHT);
        curPosX += selectedPrefabRect.width;
        selectedPrefab = (TripEditorPrefab)EditorGUI.ObjectField(selectedPrefabRect, selectedPrefab, typeof(TripEditorPrefab), allowSceneObjects: false);


        Rect addPosDataRect = new Rect(curPosX, curPosY, COLUMN_WIDTH, ROW_HEIGHT);
        curPosX += addPosDataRect.width;

        bool addParticleData = GUI.Button(addPosDataRect, "Add Particle");
        if (addParticleData)
        {
            if (atlasToParticleAtlasDict.TryGetValue(selectedPrefab.mainRenderer.atlas, out selectedParticleAtlas))
            {
                List<ParticlePosData> particleDataList = selectedParticleAtlas.posData.ToList();

                ParticlePosData newParticleData = new ParticlePosData();
                newParticleData.ticketCheckStart = 0;
                newParticleData.ticketCheckEnd = 1;
                newParticleData.depth = 1;
                newParticleData.depthSize = 1;

                newParticleData.spriteIndex = 0;
                newParticleData.scaleX = 1;
                newParticleData.scaleY = 1;
                newParticleData.randScale = 0;
                newParticleData.widthType = ParticleWidthType.Simple;
                    
                particleDataList.Add(newParticleData);

                selectedParticleAtlas.posData = particleDataList.ToArray();

                selectedPosDataIndex = selectedParticleAtlas.posData.Length - 1;

                ParticlePosData posData = selectedParticleAtlas.posData[selectedPosDataIndex];

                selectedParticleCount = posData.particleCount;
                selectedRandScale = posData.randScale;
                selectedWidthType = posData.widthType;

                ApplyPrefabToPosData(ref newParticleData);
            }

        }

        Rect removePosDataRect = new Rect(curPosX, curPosY, COLUMN_WIDTH, ROW_HEIGHT);
        curPosX += removePosDataRect.width;

        bool removeParticleData = GUI.Button(removePosDataRect, "Remove Particle");
        if (removeParticleData)
        {
            if (atlasToParticleAtlasDict.TryGetValue(selectedPrefab.mainRenderer.atlas, out selectedParticleAtlas))
            {
                List<ParticlePosData> particleDataList = selectedParticleAtlas.posData.ToList();
                particleDataList.RemoveAt(selectedPosDataIndex);
                selectedParticleAtlas.posData = particleDataList.ToArray();
                selectedPosDataIndex = Mathf.Clamp(selectedPosDataIndex - 1, 0, selectedParticleAtlas.posData.Length - 1);

                if (selectedParticleAtlas.posData.Length > 0)
                {
                    ParticlePosData posData = selectedParticleAtlas.posData[selectedPosDataIndex];
                    selectedParticleCount = posData.particleCount;
                }
            }
        }


        Rect applyChangesRect = new Rect(curPosX, curPosY, COLUMN_WIDTH, ROW_HEIGHT);
        curPosX += applyChangesRect.width;
        bool appliedChanges = GUI.Button(applyChangesRect, "Apply Changes");


        if (appliedChanges)
        {
            if (selectedPrefab == null)
            {
                selectedParticleAtlas = null;
            }

            if (selectedPosDataIndex != -1 && atlasToParticleAtlasDict.TryGetValue(selectedPrefab.mainRenderer.atlas, out selectedParticleAtlas))
            {
                Debug.Log(selectedPosDataIndex);
                ParticlePosData selectedPosData = selectedParticleAtlas.posData[selectedPosDataIndex];
                ApplyPrefabToPosData(ref selectedPosData);
            }
            else
            {
                selectedParticleAtlas = null;
            }
        }

        curPosY += (int)ROW_HEIGHT;
    }
    private void DrawThirdHeader()
    {
        if (trip == null || selectedPosDataIndex == -1) return;
        
        Rect posDataLabelRect = new Rect(0, curPosY, graphRect.x, ROW_HEIGHT);
        EditorGUI.LabelField(posDataLabelRect, "Pos Data");

        float curPosX = graphRect.x;

        if (selectedParticleAtlas == null || selectedParticleAtlas.posData.Length == 0) return;
        
        Rect elevationToggleLabelRect = new Rect(curPosX, curPosY, COLUMN_WIDTH / 2, ROW_HEIGHT);
        curPosX += elevationToggleLabelRect.width;
        EditorGUI.LabelField(elevationToggleLabelRect, "Elevate");

        Rect elevationToggleRect = new Rect(curPosX, curPosY, COLUMN_WIDTH / 2, ROW_HEIGHT);
        curPosX += elevationToggleRect.width;
        EditorGUI.BeginChangeCheck();
        selectedElevation = EditorGUI.Toggle(elevationToggleRect, selectedElevation);
        if (EditorGUI.EndChangeCheck())
        {
            atlasToParticleAtlasDict = new Dictionary<AtlasSO, ParticleAtlas>();
            for (int i = 0; i < trip.particleAtlasArray.Length; i++)
            {
                ParticleAtlas particleAtlasKeyValue = trip.particleAtlasArray[i];
                atlasToParticleAtlasDict.Add(particleAtlasKeyValue.atlas, particleAtlasKeyValue);
            }

            if (atlasToParticleAtlasDict.TryGetValue(selectedPrefab.mainRenderer.atlas, out selectedParticleAtlas))
            {
                ParticlePosData selectedPosData = selectedParticleAtlas.posData[selectedPosDataIndex];
                selectedPosData.elevate = selectedElevation;
                selectedParticleAtlas.posData[selectedPosDataIndex] = selectedPosData;
            }
        }


        switch (selectedParticleAtlas.particleType)
        {
            case ParticleType.Zone:
            {
                Rect selectedParticleCountLabelRect = new Rect(curPosX, curPosY, COLUMN_WIDTH / 2, ROW_HEIGHT);
                curPosX += selectedParticleCountLabelRect.width;
                EditorGUI.LabelField(selectedParticleCountLabelRect, "Count");

                Rect selectedParticleCountRect = new Rect(curPosX, curPosY, COLUMN_WIDTH / 2, ROW_HEIGHT);
                curPosX += selectedParticleCountRect.width;

                EditorGUI.BeginChangeCheck();
                selectedParticleCount = EditorGUI.IntSlider(selectedParticleCountRect, selectedParticleCount, 1, ZONE_PARTICLE_COUNT);
                if (EditorGUI.EndChangeCheck())
                {
                    if (atlasToParticleAtlasDict.TryGetValue(selectedPrefab.mainRenderer.atlas, out selectedParticleAtlas))
                    {
                        ParticlePosData selectedPosData = selectedParticleAtlas.posData[selectedPosDataIndex];
                        selectedPosData.particleCount = selectedParticleCount;
                        selectedParticleAtlas.posData[selectedPosDataIndex] = selectedPosData;
                    }
                }


                switch (selectedParticleAtlas.posData[selectedPosDataIndex].widthType)
                {
                    case ParticleWidthType.Tiled:
                    case ParticleWidthType.Sliced:
                    {
                        Rect randScaleLabelRect = new Rect(curPosX, curPosY, COLUMN_WIDTH / 2, ROW_HEIGHT);
                        curPosX += randScaleLabelRect.width;
                        EditorGUI.LabelField(randScaleLabelRect, "Random");

                        Rect randScaleRect = new Rect(curPosX, curPosY, COLUMN_WIDTH / 2, ROW_HEIGHT);
                        curPosX += randScaleRect.width;

                        EditorGUI.BeginChangeCheck();
                        selectedRandScale = EditorGUI.Slider(randScaleRect, selectedRandScale, 0, 10);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (atlasToParticleAtlasDict.TryGetValue(selectedPrefab.mainRenderer.atlas, out selectedParticleAtlas))
                            {
                                ParticlePosData selectedPosData = selectedParticleAtlas.posData[selectedPosDataIndex];
                                selectedPosData.randScale = selectedRandScale;
                                selectedParticleAtlas.posData[selectedPosDataIndex] = selectedPosData;
                            }
                        }

                    }
                    break;
                }
            }
            break;
        }

        if (selectedParticleAtlas.posData[selectedPosDataIndex].widthType != ParticleWidthType.Sliced)
        {
            Rect widthTypeLabelRect = new Rect(curPosX, curPosY, COLUMN_WIDTH / 2, ROW_HEIGHT);
            curPosX += widthTypeLabelRect.width;

            EditorGUI.LabelField(widthTypeLabelRect, "Width");

            Rect widthTypeRect = new Rect(curPosX, curPosY, COLUMN_WIDTH / 2, ROW_HEIGHT);
            curPosX += widthTypeRect.width;

            EditorGUI.BeginChangeCheck();
            selectedWidthType = (ParticleWidthType)EditorGUI.EnumPopup(widthTypeRect, selectedWidthType);
            if (EditorGUI.EndChangeCheck())
            {
                if (atlasToParticleAtlasDict.TryGetValue(selectedPrefab.mainRenderer.atlas, out selectedParticleAtlas))
                {
                    ParticlePosData selectedPosData = selectedParticleAtlas.posData[selectedPosDataIndex];
                    selectedPosData.widthType = selectedWidthType;
                    selectedParticleAtlas.posData[selectedPosDataIndex] = selectedPosData;
                }
            }
        }

        curPosY += (int)ROW_HEIGHT;
    }
    private void DrawSidePanel()
    {
        if (trip == null) return;
        if (selectedParticleAtlas == null) return;

        if (selectedParticleAtlas.particleType != ParticleType.Scroll) return;

        Event e = Event.current;

        if (selectedParticleAtlas.posData.Length == 0) return;
        ParticlePosData posData = selectedParticleAtlas.posData[selectedPosDataIndex];

        float halfSidePanelWidth = sidePanelRect.width * 0.5f;
        float quarterSidePanelWidth = sidePanelRect.width * 0.25f; 

        Rect preScrollerObjectRect = new Rect(sidePanelRect.x, ROW_HEIGHT * 3, halfSidePanelWidth + quarterSidePanelWidth, ROW_HEIGHT);
        Rect postScrollerObjectRect = new Rect(sidePanelRect.x, preScrollerObjectRect.y + ROW_HEIGHT, preScrollerObjectRect.width, ROW_HEIGHT);

        if (posData.preScrollers == null || posData.preScrollers.Length == 0) return;

        sidePanelScroll = GUI.BeginScrollView(sidePanelScrollRect, sidePanelScroll, fullRect);
        {
            Handles.BeginGUI();
            {
                Handles.DrawSolidRectangleWithOutline(sidePanelRect, Color.clear, Color.white);
                float containterHeight = ROW_HEIGHT * EDGE_SCROLLER_INPUT_AMOUNT;
                
                Rect preScrollerHeaderRect = new Rect(sidePanelRect.x, sidePanelRect.y, sidePanelRect.width, ROW_HEIGHT);
                EditorGUI.LabelField(preScrollerHeaderRect, new GUIContent("Pre Scrollers"));
                DrawEdgeScrollers(posData.preScrollers[0].spriteData, containterHeight, e, selectedParticleAtlas, ROW_HEIGHT, isPreScroller: true);


            }
            Handles.EndGUI();
        }
        GUI.EndScrollView();
    }
    private void DrawEdgeScrollers(EdgeSpriteData[] edgeScrollers, float containerHeight, Event e, ParticleAtlas particleAtlas, float startHeight, bool isPreScroller)
    {
        for (int i = 0; i < edgeScrollers.Length; i++)
        {
            EdgeSpriteData edgeScroller = edgeScrollers[i];

            float posY = startHeight + sidePanelRect.y + containerHeight * i;

            Handles.DrawLine(new Vector2(sidePanelRect.x, posY), new Vector2(sidePanelRect.xMax, posY));

            Rect texPosRect = new Rect(sidePanelRect.x, posY, sidePanelRect.width, containerHeight);

            ParticleSpritesData s = particleAtlas.spriteData[edgeScroller.spriteIndex];

            Vector2 uvPos = new Vector2(s.uvSizeAndPos.z, s.uvSizeAndPos.w);
            Vector2 uvSize = new Vector2(s.uvSizeAndPos.x, s.uvSizeAndPos.y);

            Rect texUVRect = new Rect(uvPos, uvSize);
            GUI.DrawTextureWithTexCoords(texPosRect, particleAtlas.atlas.texture, texUVRect);
        }
    }
    private void DrawTripSettings()
    {
        if (trip == null || trip.stationsDataArray.Length == 0) return;

        float ticketCheckColWidth = graphRect.width / (float)totalTicketChecks;

        int stationIndex = 0;
        int ticketChecks = 0;

        float stationRectPosY = curPosY + (ROW_HEIGHT * STATION_LABEL_ROW);
        float npcGradientPosY = curPosY + (ROW_HEIGHT * NPC_GRADIENT_ROW);
        float kmRectPosY = curPosY + (ROW_HEIGHT * KM_LABEL_ROW);
        float dayNightRectPosY = curPosY + (ROW_HEIGHT * DAY_NIGHT_ROW);
        float elevationRectPosY = curPosY + (ROW_HEIGHT * ELEVATION_ROW);

        GUIStyle stationStyle = new GUIStyle()
        {
            alignment = TextAnchor.MiddleCenter,
            normal = new GUIStyleState()
            {
                textColor = Color.white,
            },
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };

        Rect npcGradientRect = new Rect(graphRect.x, npcGradientPosY, graphRect.size.x, ROW_HEIGHT);


        for (int i = 0; i <= totalTicketChecks; i++)
        {
            float posX = graphRect.x + i * ticketCheckColWidth;
            StationSO station = trip.stationsDataArray[stationIndex];
            if (station.ticketsToCheckBeforeSpawn == ticketChecks)
            {
                GUIContent stationGUI = new GUIContent(station.stationName);
                Vector2 stationLabelSize = stationStyle.CalcSize(stationGUI);

                Rect stationRect = new Rect(posX - (stationLabelSize.x * 0.5f), stationRectPosY, stationLabelSize.x, stationLabelSize.y);

                Rect stationLabelRect = new Rect(stationRect.min, stationLabelSize);
                GUI.Label(stationLabelRect, stationGUI, stationStyle);

                stationIndex++;
                ticketChecks = 0;
            }
            ticketChecks++;

            if (i <  totalTicketChecks)
            {
                Rect kmSliderRect = new Rect(posX, kmRectPosY, ticketCheckColWidth, ROW_HEIGHT);
                selectedKMValues[i] = EditorGUI.Slider(kmSliderRect, GUIContent.none, selectedKMValues[i], 10, 100);

                Rect dayNightSliderRect = new Rect(posX, dayNightRectPosY, ticketCheckColWidth, ROW_HEIGHT);
                selectedDayNightValues[i] = EditorGUI.Slider(dayNightSliderRect, GUIContent.none, selectedDayNightValues[i], 0, 1);

                Rect elevationXLabelRect = new Rect(posX, elevationRectPosY, ROW_HEIGHT, ROW_HEIGHT);
                Rect elevationYLabelRect = new Rect(posX, elevationRectPosY + ROW_HEIGHT, ROW_HEIGHT, ROW_HEIGHT);
                EditorGUI.LabelField(elevationXLabelRect, "X");
                EditorGUI.LabelField(elevationYLabelRect, "Y");

                Rect elevationSlideXRect = new Rect(posX + elevationXLabelRect.width, elevationRectPosY, ticketCheckColWidth - elevationXLabelRect.width, ROW_HEIGHT);
                Rect elevationSlideYRect = new Rect(posX + elevationYLabelRect.width, elevationRectPosY + ROW_HEIGHT, ticketCheckColWidth - elevationYLabelRect.width, ROW_HEIGHT);
                selectedElevationValues[i].x = EditorGUI.Slider(elevationSlideXRect, GUIContent.none, selectedElevationValues[i].x, 0, 500);
                selectedElevationValues[i].y = EditorGUI.Slider(elevationSlideYRect, GUIContent.none, selectedElevationValues[i].y, -20, 20);
            }
        }

        GUIStyle style = new GUIStyle()
        {
            alignment = TextAnchor.UpperLeft,
            normal = new GUIStyleState()
            {
                textColor = Color.white
            },
        };

        Rect kmLabelRect = new Rect(0, kmRectPosY, graphRect.x, ROW_HEIGHT);
        GUIContent kmContent = new GUIContent("KMPH");
        GetFontFromRect(ref style, kmLabelRect, kmContent);
        GUI.Label(kmLabelRect, kmContent, style);

        Rect dayNightLabelRect = new Rect(0, dayNightRectPosY, graphRect.x, ROW_HEIGHT);
        GUIContent dayNightContent = new GUIContent("Day/Night");
        GetFontFromRect(ref style, dayNightLabelRect, dayNightContent);
        GUI.Label(dayNightLabelRect, dayNightContent, style);

        Rect elevationLabelRect = new Rect(0, elevationRectPosY, graphRect.x, ROW_HEIGHT);
        GUIContent elevationContent = new GUIContent("Elevation");
        GetFontFromRect(ref style, elevationLabelRect, elevationContent);
        GUI.Label(elevationLabelRect, elevationContent, style);





    }
    private void GetFontFromRect(ref GUIStyle style, Rect rect, GUIContent content)
    {
        int fontSize = Mathf.FloorToInt(rect.height);
        while (fontSize > 1)
        {
            style.fontSize = fontSize;
            Vector2 size = style.CalcSize(content);

            if (size.x <= rect.width && size.y <= rect.height) break;

            fontSize--;
        }
        style.fontSize = fontSize;
    }
    private void DrawGraph()
    {
        if (trip == null || trip.stationsDataArray.Length == 0) return;

        Event e = Event.current;

        bool clickedBar = false;

        Vector2 vertLineTop = new Vector2();
        Vector2 vertLineBottom = new Vector2();
        Vector2 horLineLeft = new Vector2();
        Vector2 horLineRight = new Vector2();

        vertLineTop.y = graphRect.yMin;
        vertLineBottom.y = graphRect.yMax;
        horLineLeft.x = graphRect.xMin;
        horLineRight.x = graphRect.xMax;

        float totalTicketCheckXIncrement = (1 / (float)totalTicketChecks) * graphRect.width;

        EditorGUILayout.BeginVertical();
        {
            graphScroll = GUI.BeginScrollView(graphScrollRect, graphScroll, fullRect);
            {
                Handles.BeginGUI();
                {
                    Vector2 trainRectPos = new Vector2(graphRect.xMin, graphRect.yMin + ((MAIN_MIN - 1) * graphRowHeight));
                    Vector2 trainRectSize = new Vector2(graphRect.size.x,(MAIN_MAX - MAIN_MIN + 1) * graphRowHeight);
                    Rect trainRect = new Rect(trainRectPos, trainRectSize);
                    Handles.DrawSolidRectangleWithOutline(trainRect, Color.darkRed, Color.clear);

                    for (int i = 0; i <= totalTicketChecks;i++)
                    {
                        float posX = graphRect.x + i * totalTicketCheckXIncrement;
                        vertLineTop.x = posX;
                        vertLineBottom.x = posX;
                        Handles.DrawLine(vertLineTop, vertLineBottom);
                    }

                    for (int i = 0; i <= FAR_CLIP; i++)
                    {
                        float posY = graphRect.yMin + (i * graphRowHeight);
                        horLineLeft.y = posY;
                        horLineRight.y = posY;
                        Handles.DrawLine(horLineLeft, horLineRight);

                        if (i == 0) continue;

                        string depthString = (i).ToString();
                        
                        if (i == TRAIN_LINE_DEPTH)
                        {
                            depthString = "LINES";
                        }
                        else if (i == TRAIN_TRACKS_DEPTH)
                        {
                            depthString = "TRACKS";
                        }
                        Vector2 labelSize = depthLabel.CalcSize(new GUIContent(depthString));
                        Vector2 depthLabelPos = new Vector2(graphRect.xMin -  5, posY - (graphRowHeight - (labelSize.y * 0.5f)));
                        Handles.Label(depthLabelPos, depthString, depthLabel);
                    }

                    for (int i = 0; i < trip.particleAtlasArray.Length; i++)
                    {
                        ParticleAtlas particleAtlas = trip.particleAtlasArray[i];

                        if (particleAtlas.spriteData.Length == 0) continue;

                        for (int j = 0; j < particleAtlas.posData.Length; j++)
                        {
                            ParticlePosData posData = particleAtlas.posData[j];

                            float ticketCheckSize = posData.ticketCheckEnd - posData.ticketCheckStart;
                            float barWidth = ticketCheckSize * totalTicketCheckXIncrement;
                            float barHeight = Mathf.Max(graphRowHeight * posData.depthSize, graphRowHeight);
                            float barX = graphRect.xMin + posData.ticketCheckStart * totalTicketCheckXIncrement;

                            float graphDepth = (posData.depth - 1);
                            float barY = graphRect.yMin + graphDepth * graphRowHeight;
                            
                            Rect barRect = new Rect(barX, barY, barWidth, barHeight);

                            Handles.DrawSolidRectangleWithOutline(barRect, particleAtlasColors[i], Color.black);

                            GUID posDataGuid = new GUID(posData.prefabGUID);
                            TripEditorPrefab posDataPrefab = (TripEditorPrefab)AssetDatabase.LoadAssetByGUID(posDataGuid, typeof(TripEditorPrefab));

                            EditorGUI.LabelField(barRect, posDataPrefab.name, GUIStyle.none);
                            
                            if (e.type == EventType.MouseDown && barRect.Contains(e.mousePosition))
                            {
                                selectedParticleAtlas = trip.particleAtlasArray[i];
                                selectedPosDataIndex = j;
                                adjustingBar = true;

                                clickedBar = true;

                                int mouseTicketXPos = (int)((e.mousePosition.x - graphRect.xMin) / totalTicketCheckXIncrement);
                                int mouseDepthYPos = (int)((e.mousePosition.y - graphRect.yMin) / graphRowHeight);

                                dragOffsetStartX = mouseTicketXPos - posData.ticketCheckStart;
                                dragOffsetStartY = mouseDepthYPos - (int)posData.depth;

                                dragOffsetWidth = (int)(mouseTicketXPos - ticketCheckSize);
                                dragOffsetHeight = (int)(mouseDepthYPos - posData.depthSize);

                                isAdjustingMetersStart = e.mousePosition.x < barRect.center.x;
                                isAdjustingDepthSize = e.mousePosition.y > barRect.center.y && particleAtlas.particleType == ParticleType.Zone;

                                selectedParticleCount = posData.particleCount;
                                selectedRandScale = posData.randScale;
                                selectedWidthType = posData.widthType;
                                selectedElevation = posData.elevate;

                                selectedPrefab = posDataPrefab;
                                Repaint();
                            }

                            if (selectedParticleAtlas != trip.particleAtlasArray[i] || selectedPosDataIndex != j) continue;

                            Handles.DrawSolidRectangleWithOutline(barRect, Color.clear, Color.green);

                            if (e.type == EventType.MouseDrag && adjustingBar)
                            {
                                int mouseTicketXPos = (int)((e.mousePosition.x - graphRect.xMin) / totalTicketCheckXIncrement);
                                int mouseDepthYPos = (int)((e.mousePosition.y - graphRect.yMin) / graphRowHeight);

                                if (isAdjustingMetersStart)
                                {
                                    int curTicketStart = mouseTicketXPos - dragOffsetStartX;
                                    posData.ticketCheckStart = Mathf.Clamp(curTicketStart, 0, posData.ticketCheckEnd - 1);
                                }
                                else
                                {
                                    int curTicketEnd = posData.ticketCheckStart + (mouseTicketXPos - dragOffsetWidth);
                                    posData.ticketCheckEnd = Mathf.Clamp(curTicketEnd, posData.ticketCheckStart + 1 , totalTicketChecks);
                                }
                                if (isAdjustingDepthSize)
                                {
                                    int curDepthSize = mouseDepthYPos - dragOffsetHeight;
                                    posData.depthSize = Mathf.Clamp(curDepthSize, 1, (int)FAR_CLIP - posData.depth);
                                }
                                else
                                {
                                    int curDepth = mouseDepthYPos - dragOffsetStartY;
                                    posData.depth = Mathf.Clamp(curDepth, 1, (int)FAR_CLIP);
                                }
                                Repaint();
                            }

                            particleAtlas.posData[j] = posData;

                            if (e.type == EventType.MouseUp && adjustingBar)
                            {
                                adjustingBar = false;
                                ApplyPrefabToPosData(ref posData);
                                SavePrevDepthIndices();
                            }
                        }
                    }
                }

                if (e.type == EventType.MouseDown && !clickedBar)
                {
                    selectedPosDataIndex = -1;
                    selectedParticleAtlas = null;
                }
                Handles.EndGUI();
            }
            GUI.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawNineSliceBar(ParticleAtlas particleAtlas, ParticlePosData posData, float barHeight, float barWidth)
    {
        float totalPixelHeight = 0;
        float totalPixelWidth = 0;

        for (int i = 0; i < 9; i++)
        {
            ParticleSpritesData spriteData = particleAtlas.spriteData[posData.spriteIndex + i];
            float spritePixelWidth = spriteData.uvSizeAndPos.x * particleAtlas.atlas.texture.width;
            float spritePixelheight = spriteData.uvSizeAndPos.y * particleAtlas.atlas.texture.height;
            totalPixelWidth += spritePixelWidth;
            totalPixelHeight += spritePixelheight;
        }

        int sliceIndex = 0;
        float sliceYPos = barHeight;
        for (int y = 0; y < 3; y++)
        {
            float sliceXPos = 0;
            float scaledHeight = 0;
            for (int x = 0; x < 3; x++)
            {
                ParticleSpritesData spriteData = particleAtlas.spriteData[posData.spriteIndex + sliceIndex];
                float spritePixelWidth = spriteData.uvSizeAndPos.x * particleAtlas.atlas.texture.width;
                float spritePixelheight = spriteData.uvSizeAndPos.y * particleAtlas.atlas.texture.height;

                float scaledWidth = (spritePixelWidth / totalPixelWidth) * barWidth * 3;
                scaledHeight = (spritePixelheight / totalPixelHeight) * barHeight * 3;

                Vector2 uvPos = new Vector2(spriteData.uvSizeAndPos.z, spriteData.uvSizeAndPos.w);
                Vector2 uvSize = new Vector2(spriteData.uvSizeAndPos.x, spriteData.uvSizeAndPos.y);
                Rect uvRect = new Rect(uvPos, uvSize);

                Rect spriteRect = new Rect(sliceXPos, sliceYPos - scaledHeight, scaledWidth, scaledHeight);
                GUI.DrawTextureWithTexCoords(spriteRect, particleAtlas.atlas.texture, uvRect);
                sliceIndex++;
                sliceXPos += scaledWidth;
            }
            sliceYPos -= scaledHeight;
        }
    }
    private void ApplyPrefabToPosData(ref ParticlePosData posData)
    {
        if (selectedPrefab == null) return;

        atlasToParticleAtlasDict = new Dictionary<AtlasSO, ParticleAtlas>();
        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlasKeyValue = trip.particleAtlasArray[i];
            atlasToParticleAtlasDict.Add(particleAtlasKeyValue.atlas, particleAtlasKeyValue);
        }

        if (atlasToParticleAtlasDict.TryGetValue(selectedPrefab.mainRenderer.atlas, out ParticleAtlas particleAtlas))
        {
            selectedParticleAtlas = particleAtlas;
        }
        
        if (posData.preScrollers == null || posData.preScrollers.Length == 0)
        {
            posData.preScrollers = new EdgeScroller[1];
        }
        posData.preScrollers[0].spriteData = new EdgeSpriteData[selectedPrefab.preScrollers.Length];

        float posXOffset = 0;
        for (int i = 0; i < selectedPrefab.preScrollers.Length; i++)
        {
            AtlasRenderer atlasRenderer = selectedPrefab.preScrollers[i];
            posData.preScrollers[0].spriteData[i] = new EdgeSpriteData()
            {
                spriteIndex = (uint)atlasRenderer.spriteIndex,
                offset = new Vector4(atlasRenderer.transform.localPosition.x, atlasRenderer.transform.localPosition.y, atlasRenderer.transform.localPosition.z),
                scale = new Vector4(atlasRenderer.width, atlasRenderer.height),
            };
            if (posXOffset > atlasRenderer.transform.localPosition.x)
            {
                posXOffset = atlasRenderer.transform.localPosition.x;
            }
        }

        posData.posX = -posXOffset;
        if (selectedPosDataIndex >= selectedParticleAtlas.posData.Length)
        {
            selectedPosDataIndex = selectedParticleAtlas.posData.Length - 1;
        }
        
        selectedParticleAtlas.posData[selectedPosDataIndex] = posData;
        Repaint();

        string assetPath = AssetDatabase.GetAssetPath(selectedPrefab);
        posData.prefabGUID = AssetDatabase.AssetPathToGUID(assetPath);
        posData.quadCount = (uint)(posData.particleCount * posData.spritesPerParticle);
        posData.elevate = selectedElevation;

        AtlasRenderer mainAtlasRenderer = selectedPrefab.GetComponent<AtlasRenderer>();

        if (mainAtlasRenderer.rendererType == AtlasRendererType.SliceWorld)
        {
            posData.widthType = ParticleWidthType.Sliced;
        }
        else
        {
            posData.widthType = selectedWidthType;
        }


        switch (posData.widthType)
        {
            case ParticleWidthType.Tiled:
            case ParticleWidthType.Simple:
            {
                posData.spritesPerParticle = 1;
            }
            break;

            case ParticleWidthType.Sliced:
            {
                posData.spritesPerParticle = 9;
            }
            break;
        }

        switch (posData.depth)
        {
            case TRAIN_LINE_DEPTH:
            {
                posData.posY = TRAIN_LINE_HEIGHT;
            }
            break;
            case TRAIN_TRACKS_DEPTH:
            {
                posData.posY = TRAIN_WHEEL_HEIGHT;
            }
            break;
            default:
            {
                posData.posY = selectedPrefab.transform.position.y;
            }
            break;
        }

        switch (selectedParticleAtlas.particleType)
        {
            case ParticleType.Scroll:
            {
                posData.particleCount = 2;

                posData.scaleX = mainAtlasRenderer.width;
                posData.scaleY = mainAtlasRenderer.height;

                switch (posData.widthType)
                {
                    case ParticleWidthType.Simple:
                    {
                        posData.spriteIndex = mainAtlasRenderer.spriteIndex;

                        ParticleSpritesData newSpriteData = selectedParticleAtlas.spriteData[posData.spriteIndex];
                        SimpleSprite sprite = selectedParticleAtlas.atlas.simpleSprites[posData.spriteIndex];
                        newSpriteData.uvSizeAndPos = sprite.uvSizeAndPos;
                        newSpriteData.worldPivotAndSize = mainAtlasRenderer.worldPivotAndSize;

                        posData.quadScales = new Vector4[] { new Vector4(1, posData.scaleY, 0, 0) };

                        selectedParticleAtlas.spriteData[posData.spriteIndex] = newSpriteData;
                    }
                    break;
                    case ParticleWidthType.Tiled:
                    {
                        posData.spriteIndex = mainAtlasRenderer.spriteIndex;

                        ParticleSpritesData newSpriteData = selectedParticleAtlas.spriteData[posData.spriteIndex];
                        SimpleSprite sprite = selectedParticleAtlas.atlas.simpleSprites[posData.spriteIndex];
                        newSpriteData.uvSizeAndPos = sprite.uvSizeAndPos;
                        newSpriteData.worldPivotAndSize = mainAtlasRenderer.worldPivotAndSize;

                        posData.scaleX = spawnData.bounds.size.x / newSpriteData.worldPivotAndSize.z;
                        posData.quadScales = new Vector4[] { new Vector4(posData.scaleX, posData.scaleY, 0, 0) };

                        selectedParticleAtlas.spriteData[posData.spriteIndex] = newSpriteData;
                    }
                    break;
                    case ParticleWidthType.Sliced:
                    {
                        posData.spriteIndex = selectedParticleAtlas.atlas.simpleSprites.Length + (mainAtlasRenderer.spriteIndex * 9);

                        ParticleSpritesData leftSprite = selectedParticleAtlas.spriteData[posData.spriteIndex + 3];
                        ParticleSpritesData centerSprite = selectedParticleAtlas.spriteData[posData.spriteIndex + 4];
                        ParticleSpritesData rightSprite = selectedParticleAtlas.spriteData[posData.spriteIndex + 5];
                        posData.scaleX = (spawnData.bounds.size.x - leftSprite.worldPivotAndSize.z - rightSprite.worldPivotAndSize.z) / centerSprite.worldPivotAndSize.z;

                        SliceSprite sliceSprite = mainAtlasRenderer.atlas.slicedSprites[mainAtlasRenderer.spriteIndex];
                        posData.quadScales = GetQuadScalesNineSlice(posData.scaleX, posData.scaleY, sliceSprite);

                        for (int k = 0; k < posData.spritesPerParticle; k++)
                        {
                            ParticleSpritesData particleSpriteData = selectedParticleAtlas.spriteData[posData.spriteIndex + k];
                            ParticleSpritesData newSpriteData = new ParticleSpritesData();
                            newSpriteData.uvSizeAndPos = sliceSprite.uvSizeAndPos[k];
                            newSpriteData.worldPivotAndSize = mainAtlasRenderer.worldPivotsAndSizes[k];
                            selectedParticleAtlas.spriteData[posData.spriteIndex + k] = newSpriteData;
                        }
                    }
                    break;
                }
            }
            break;

            case ParticleType.Zone:
            {
                posData.spriteIndex = 0;
                posData.particleCount = selectedParticleCount;

                switch (posData.widthType)
                {
                    case ParticleWidthType.Simple:
                    {
                        posData.quadScales = new Vector4[] { new Vector4(posData.scaleX, posData.scaleY, 0, 0) };

                        for (int k = 0; k < selectedParticleAtlas.spriteData.Length; k++)
                        {
                            ParticleSpritesData newSpriteData = selectedParticleAtlas.spriteData[k];
                            SimpleSprite sprite = selectedParticleAtlas.atlas.simpleSprites[k];
                            newSpriteData.uvSizeAndPos = sprite.uvSizeAndPos;
                            float pivotX = sprite.uvPivot.x * sprite.worldSize.x;
                            float pivotY = sprite.uvPivot.y * sprite.worldSize.y;
                            newSpriteData.worldPivotAndSize = new Vector4(pivotX, pivotY, sprite.worldSize.x, sprite.worldSize.y);
                            selectedParticleAtlas.spriteData[k] = newSpriteData;
                        }
                    }
                    break;

                    case ParticleWidthType.Tiled:
                    {
                        posData.randScale = selectedRandScale;

                        List<Vector4> quadScales_list = new List<Vector4>();
                        for (int k = 0; k < posData.particleCount; k++)
                        {
                            float randScaleX = UnityEngine.Random.Range(-posData.randScale, posData.randScale);
                            float randScaleY = UnityEngine.Random.Range(-posData.randScale, posData.randScale);
                            randScaleX = Mathf.Max(posData.scaleX + randScaleX, 1);
                            randScaleY = Mathf.Max(posData.scaleY + randScaleY, 1);
                            quadScales_list.Add(new Vector4(randScaleX, randScaleY, 0, 0));
                        }
                        posData.quadScales = quadScales_list.ToArray();

                        for (int k = 0; k < selectedParticleAtlas.spriteData.Length; k++)
                        {
                            ParticleSpritesData newSpriteData = selectedParticleAtlas.spriteData[k];
                            SimpleSprite sprite = selectedParticleAtlas.atlas.simpleSprites[k];
                            newSpriteData.uvSizeAndPos = sprite.uvSizeAndPos;
                            float pivotX = sprite.uvPivot.x * sprite.worldSize.x;
                            float pivotY = sprite.uvPivot.y * sprite.worldSize.y;
                            newSpriteData.worldPivotAndSize = new Vector4(pivotX, pivotY, sprite.worldSize.x, sprite.worldSize.y);
                            selectedParticleAtlas.spriteData[k] = newSpriteData;
                        }
                    }
                    break;

                    case ParticleWidthType.Sliced:
                    {
                        posData.randScale = selectedRandScale;
                        posData.scaleX = selectedPrefab.mainRenderer.width;
                        posData.scaleY = selectedPrefab.mainRenderer.height;
                        List<Vector4> quadScales_list = new List<Vector4>();
                        for (int k = 0; k < posData.particleCount; k++)
                        {
                            float randScaleX = UnityEngine.Random.Range(-posData.randScale, posData.randScale);
                            float randScaleY = UnityEngine.Random.Range(-posData.randScale, posData.randScale);
                            randScaleX = Mathf.Max(posData.scaleX + randScaleX, 1);
                            randScaleY = Mathf.Max(posData.scaleY + randScaleY, 1);
                            int sliceSpriteIndex = (k % selectedParticleAtlas.atlas.slicedSprites.Length);
                            SliceSprite sliceSprite = selectedParticleAtlas.atlas.slicedSprites[sliceSpriteIndex];

                            quadScales_list.AddRange(GetQuadScalesNineSlice(randScaleX, randScaleY, sliceSprite));
                        }
                        posData.quadScales = quadScales_list.ToArray();

                        for (int k = 0; k < selectedParticleAtlas.spriteData.Length; k += posData.spritesPerParticle)
                        {
                            int sliceSpriteIndex = Mathf.FloorToInt(k / 9);
                            SliceSprite sliceSprite = selectedParticleAtlas.atlas.slicedSprites[sliceSpriteIndex];
                            Vector4[] worldPivotsAndSizes = SetWorldPivotAndSizes(sliceSprite, 1, 1);

                            for (int l = 0; l < posData.spritesPerParticle; l++)
                            {
                                ParticleSpritesData particleSpriteData = selectedParticleAtlas.spriteData[k + l];
                                ParticleSpritesData newSpriteData = new ParticleSpritesData();
                                newSpriteData.uvSizeAndPos = sliceSprite.uvSizeAndPos[l];
                                newSpriteData.worldPivotAndSize = worldPivotsAndSizes[l];

                                selectedParticleAtlas.spriteData[k + l] = newSpriteData;
                            }
                        }
                    }
                    break;
                }
            }
            break;
        }

        selectedParticleAtlas.posData[selectedPosDataIndex] = posData;
        SaveOrderedPosData();  
    }
    private float GetScaledWidth(ParticleSpritesData spriteData, ParticleAtlas particleAtlas)
    {
        float spritePixelWidth = spriteData.uvSizeAndPos.x * particleAtlas.atlas.texture.width;
        float spritePixelHeight = spriteData.uvSizeAndPos.y * particleAtlas.atlas.texture.height;
        float scale = graphRowHeight / spritePixelHeight;
        return spritePixelWidth * scale;
    }
    private float GetScaledHeight(ParticleSpritesData spriteData, ParticleAtlas particleAtlas)
    {
        float spritePixelHeight = spriteData.uvSizeAndPos.y * particleAtlas.atlas.texture.height;
        float scale = graphRowHeight / spritePixelHeight;
        return spritePixelHeight * scale;
    }
    private void SaveOrderedPosData()
    {
        bool anyChanged = false;

        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas particleAtlas = trip.particleAtlasArray[i];

            ParticlePosData[] ordered =
                particleAtlas.posData
                .OrderBy(p => p.ticketCheckStart)
                .ThenBy(p => p.depth)
                .ToArray();

            particleAtlas.posData = ordered;
        }

        SetParticleIndices(ParticleType.Zone, ZONE_PARTICLE_COUNT);
        SetParticleIndices(ParticleType.Scroll, SCROLL_PARTICLE_COUNT);

        if (anyChanged) AssetDatabase.SaveAssets();
    }
    private void SavePrevDepthIndices()
    {
        Dictionary<int, ParticlePosData> latestDepthMap = new Dictionary<int, ParticlePosData>();
        List<(ParticleAtlas atlas, int index, ParticlePosData posData)> allPosData = new();

        for (int i = 0; i < trip.particleAtlasArray.Length; i++)
        {
            ParticleAtlas atlas = trip.particleAtlasArray[i];

            for (int j = 0; j < atlas.posData.Length; j++)
            {
                allPosData.Add((atlas, j, atlas.posData[j]));
            }
        }

        allPosData = allPosData
            .OrderBy(p => p.posData.ticketCheckStart)
            .ToList();

        HashSet<ParticleAtlas> dirtyAtlases = new HashSet<ParticleAtlas>();

        foreach (var entry in allPosData)
        {
            ParticlePosData posData = entry.posData;

            if (latestDepthMap.TryGetValue(posData.depth, out ParticlePosData prevPosData))
            {
                posData.prevDepthIndices = new Vector2Int(
                    prevPosData.minParticleIndex,
                    prevPosData.maxParticleIndex
                );
            }
            else
            {
                posData.prevDepthIndices = new Vector2Int(0, 0);
            }

            entry.atlas.posData[entry.index] = posData;
            
            latestDepthMap[posData.depth] = posData;

            dirtyAtlases.Add(entry.atlas);
        }

        foreach (ParticleAtlas atlas in dirtyAtlases)
        {
            EditorUtility.SetDirty(atlas);
        }

        AssetDatabase.SaveAssets();
    }
    private void SetParticleIndices(ParticleType particleType, int maxParticleCount)
    {
        int minParticle = 0;
        int maxParticle = -1;

        for (int i = 0; i < totalTicketChecks; i++)
        {
            for (int j = 0; j < trip.particleAtlasArray.Length; j++)
            {
                ParticleAtlas particleAtlas = trip.particleAtlasArray[j];

                if (particleAtlas.particleType != particleType) continue;

                for (int k = 0; k < particleAtlas.posData.Length; k++)
                {
                    ParticlePosData posData = particleAtlas.posData[k];

                    if (posData.ticketCheckStart != i) continue;

                    maxParticle += (int)posData.particleCount;
                    maxParticle %= maxParticleCount;

                    posData.minParticleIndex = minParticle;
                    posData.maxParticleIndex = maxParticle;

                    particleAtlas.posData[k] = posData;
                    minParticle = maxParticle + 1;
                    minParticle %= maxParticleCount;
                }
            }
        }
    }
}