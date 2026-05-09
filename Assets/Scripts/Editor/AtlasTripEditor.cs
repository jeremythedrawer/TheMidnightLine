using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Atlas;
using static AtlasSpawn;
using static AtlasRendering;
using System;

public class AtlasTripEditor : EditorWindow
{
    const float HEADER_COL_WIDTH = 200;
    const float HEADER_COL_HEIGHT = 20;
    const float PADDING = 100;
    const float STATION_RECT_Y_OFFSET = 25;

    static GUILayoutOption[] headerGUI;
    static GUILayoutOption[] horizontalGUI;
    static GUILayoutOption[] inputGUI;
    static GUILayoutOption[] buttonGUI;

    static GUIStyle stationLabel;
    static GUIStyle depthLabel;
    static GUIStyle particleAvailCountLabel;

    static Rect fullRect;
    static Rect scrollViewRect;

    static Rect graphRect;

    static Vector2 graphPos;
    static Vector2 graphSize;
    static Vector2 scroll;

    static int[] particleAvailCount_array;

    static int totalTicketChecks;
    static int selectedStationIndex;
    static int selectedPosDataIndex;
    static int selectedParticlAtlasIndex;
    static int dragOffsetWidth;
    static int dragOffsetHeight;
    static int dragOffsetStartX;
    static int dragOffsetStartY;

    static float stationRectPosY;
    static float graphRowHeight;

    private bool isAdjustingMetersStart;
    private bool isAdjustingDepthSize;

    public TripSO trip;
    public ParticleAtlas selectedParticleAtlas;
    public int selectedSpriteIndex;
    public int selectedParticleCount;

    public ParticleWidthType selectedScrollSpriteType;

    [MenuItem("Tools/Atlas Trip Editor")]
    private static void Open()
    {
        AtlasTripEditor tripEditor = GetWindow<AtlasTripEditor>("Atlas Trip Editor");
        inputGUI = new GUILayoutOption[] { GUILayout.Width(300) };
        buttonGUI = new GUILayoutOption[] { GUILayout.Width(150) };
        tripEditor.Init();

    }

    private void OnGUI()
    {
        Init();
        GUISetUp();
        DrawFirstHeader();
        DrawSecondHeader();
        DrawThirdHeader();
        DrawGraph();
    }
    public void Init()
    {
        fullRect = new Rect(0, 0, position.width, 4250);

        graphPos = new Vector2(PADDING, 100 + HEADER_COL_HEIGHT);
        graphSize = new Vector2(fullRect.width - PADDING * 2, fullRect.height);

        fullRect.height += PADDING * 4;

        scrollViewRect = new Rect(0, graphPos.y + 50, position.width, position.height);
        graphRect = new Rect(graphPos, graphSize);
        
        stationRectPosY = graphPos.y - STATION_RECT_Y_OFFSET;
        graphRowHeight = graphRect.height / FAR_CLIP;

        horizontalGUI = new GUILayoutOption[] { GUILayout.Height(HEADER_COL_HEIGHT) };

        stationLabel = new GUIStyle();
        stationLabel.alignment = TextAnchor.MiddleCenter;
        stationLabel.normal.textColor = Color.white;
        stationLabel.fontSize = 16;
        stationLabel.fontStyle = FontStyle.Bold;

        depthLabel = new GUIStyle();
        depthLabel.alignment = TextAnchor.MiddleRight;
        depthLabel.normal.textColor = Color.white;
        depthLabel.fontSize = 16;

        particleAvailCountLabel = new GUIStyle();
        particleAvailCountLabel.alignment = TextAnchor.LowerLeft;
        particleAvailCountLabel.normal.textColor = Color.white;
        particleAvailCountLabel.fontSize = 16;
        particleAvailCountLabel.fontStyle = FontStyle.Normal;
    }
    private void GUISetUp()
    {
        if (trip == null) return;
        totalTicketChecks = 0;
        for (int i = 0; i < trip.stationsDataArray.Length; i++)
        {
            StationSO station = trip.stationsDataArray[i];
            totalTicketChecks += station.ticketsToCheckBeforeSpawn;
        }

        particleAvailCount_array = new int[totalTicketChecks];

        Array.Fill(particleAvailCount_array, ZONE_PARTICLE_COUNT);

        for (int i = 0; i < particleAvailCount_array.Length; i++)
        {
            for (int j = 0; j < trip.particleAtlasArray.Length; j++)
            {
                ParticleAtlas particleAtlas = trip.particleAtlasArray[j];

                for (int k = 0; k < particleAtlas.posData.Length; k++)
                {
                    ParticlePosData posData = particleAtlas.posData[k];

                    if (posData.ticketCheckStart <= i && posData.ticketCheckEnd > i)
                    {
                        particleAvailCount_array[i] -= posData.particleCount;
                    }
                }
            }
        }
    }
    private void DrawFirstHeader()
    {
        EditorGUILayout.BeginHorizontal(horizontalGUI);
        {
            EditorGUI.BeginChangeCheck();            
            trip = (TripSO)EditorGUILayout.ObjectField("Trip", trip, typeof(TripSO), allowSceneObjects: false, inputGUI);
        }
        EditorGUILayout.EndHorizontal();

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
    }
    private void DrawSecondHeader()
    {
        if (trip == null) return;

        EditorGUILayout.BeginHorizontal(horizontalGUI);
        {
            EditorGUILayout.LabelField("Atlas Options");
            string label = selectedParticleAtlas == null ? "Select Atlas" : selectedParticleAtlas.name;
            GUIContent particleAtlasSelectedContent = new GUIContent(label);

            bool selectedDropDown = EditorGUILayout.DropdownButton(particleAtlasSelectedContent, FocusType.Passive, headerGUI);

            if (selectedDropDown)
            {
                GenericMenu particleAtlasMenu = new GenericMenu();

                for (int i = 0; i < trip.particleAtlasArray.Length; i++)
                {
                    ParticleAtlas particleAtlas = trip.particleAtlasArray[i];
                    GUIContent particleAtlasContent = new GUIContent(particleAtlas.name);
                    GenericMenu.MenuFunction menuFunc = () => { selectedParticleAtlas = particleAtlas; };

                    particleAtlasMenu.AddItem(particleAtlasContent, selectedParticleAtlas == particleAtlas, menuFunc);
                    particleAtlasMenu.ShowAsContext();
                }
            }

            GUILayout.Space(10);

            if (selectedParticleAtlas != null)
            {
                bool addParticleData = GUILayout.Button("Add Particle", buttonGUI);
                if (addParticleData)
                {
                    List<ParticlePosData> particleDataList = selectedParticleAtlas.posData.ToList();
                    ParticlePosData newParticleData = new ParticlePosData();
                    newParticleData.ticketCheckStart = 0;
                    newParticleData.ticketCheckEnd = 1;
                    newParticleData.depth = 1;
                    newParticleData.depthSize = 1;
                    particleDataList.Add(newParticleData);
                    selectedParticleAtlas.posData = particleDataList.ToArray();
                }

                bool removeParticleData = GUILayout.Button("Remove Particle", buttonGUI);
                if (removeParticleData)
                {
                    List<ParticlePosData> particleDataList = selectedParticleAtlas.posData.ToList();
                    particleDataList.RemoveAt(selectedPosDataIndex);
                    selectedParticleAtlas.posData = particleDataList.ToArray();
                    selectedPosDataIndex = Mathf.Clamp(selectedPosDataIndex - 1, 0, selectedParticleAtlas.posData.Length - 1);
                }
            }

            GUILayout.Space(10);


            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();
    }
    private void DrawThirdHeader()
    {
        if (trip == null || selectedParticleAtlas == null || selectedParticleAtlas.posData.Length == 0) return;
        EditorGUILayout.BeginHorizontal(horizontalGUI);
        {
            ParticlePosData selectedPosData = selectedParticleAtlas.posData[selectedPosDataIndex];

            selectedParticleCount = EditorGUILayout.IntSlider(selectedParticleCount, 1, particleAvailCount_array[selectedPosData.ticketCheckStart], headerGUI);
            GUILayout.FlexibleSpace();
        }
        EditorGUILayout.EndHorizontal();
    }
    private void DrawGraph()
    {
        if (trip == null || trip.stationsDataArray.Length == 0) return;

        Event e = Event.current;

        Vector2 vertLineTop = new Vector2();
        Vector2 vertLineBottom = new Vector2();
        Vector2 horLineLeft = new Vector2();
        Vector2 horLineRight = new Vector2();

        vertLineTop.y = graphRect.yMin;
        vertLineBottom.y = graphRect.yMax;
        horLineLeft.x = graphRect.xMin;
        horLineRight.x = graphRect.xMax;

        float totalTicketCheckXIncrement = (1 / (float)totalTicketChecks) * graphSize.x;
        EditorGUILayout.BeginVertical();
        {
            Handles.BeginGUI();
            {
                int stationIndex = 0;
                int ticketChecks = 0;
                for (int i = 0; i <= totalTicketChecks; i++)
                {
                    float posX = graphPos.x + i * totalTicketCheckXIncrement;

                    if (i < totalTicketChecks)
                    {
                        string particleAvailCount_string = particleAvailCount_array[i].ToString();
                        Vector2 particleAvailLabelSize = particleAvailCountLabel.CalcSize(new GUIContent(particleAvailCount_string));
                        Rect particleAvailLabelRect = new Rect(posX - (particleAvailLabelSize.x * 0.5f), vertLineTop.y + particleAvailLabelSize.y, particleAvailLabelSize.x, particleAvailLabelSize.y);
                        GUI.Label(particleAvailLabelRect, particleAvailCount_string, particleAvailCountLabel);
                    }

                    StationSO station = trip.stationsDataArray[stationIndex];
                    if (station.ticketsToCheckBeforeSpawn == ticketChecks)
                    {

                        Vector2 stationLabelSize = stationLabel.CalcSize(new GUIContent(station.stationName));
                        Rect stationRect = new Rect(posX - (stationLabelSize.x * 0.5f), stationRectPosY, stationLabelSize.x, stationLabelSize.y);
                        Handles.DrawSolidRectangleWithOutline(stationRect, Color.black, Color.white);

                        Rect stationLabelRect = new Rect(stationRect.min, stationLabelSize);
                        GUI.Label(stationLabelRect, station.stationName, stationLabel);
                        
                        stationIndex++;
                        ticketChecks = 0;
                    }
                    ticketChecks++;
                }
            }
            Handles.EndGUI();

        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        {
            scroll = GUI.BeginScrollView(scrollViewRect, scroll, fullRect);
            {
                Handles.BeginGUI();
                {
                    Vector2 trainRectPos = new Vector2(graphRect.xMin, graphRect.yMin + ((MAIN_MIN - 1) * graphRowHeight));
                    Vector2 trainRectSize = new Vector2(graphRect.size.x,(MAIN_MAX - MAIN_MIN + 1) * graphRowHeight);
                    Rect trainRect = new Rect(trainRectPos, trainRectSize);
                    Handles.DrawSolidRectangleWithOutline(trainRect, Color.darkRed, Color.clear);

                    for (int i = 0; i <= totalTicketChecks;i++)
                    {
                        float posX = graphPos.x + i * totalTicketCheckXIncrement;
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

                        for (int j = 0; j < particleAtlas.posData.Length; j++)
                        {
                            ParticlePosData particleData = particleAtlas.posData[j];

                            float ticketCheckSize = particleData.ticketCheckEnd - particleData.ticketCheckStart;
                            float barWidth = ticketCheckSize * totalTicketCheckXIncrement;
                            float barHeight = Mathf.Max(graphRowHeight * particleData.depthSize, graphRowHeight);
                            float barX = graphRect.xMin + particleData.ticketCheckStart * totalTicketCheckXIncrement;


                            float graphDepth = (particleData.depth - 1);
                            float barY = graphRect.yMin + graphDepth * graphRowHeight;
                            
                            Rect barRect = new Rect(barX, barY, barWidth, barHeight);

                            ParticleSpriteData spriteData;

                            if (particleAtlas.particleType == ParticleType.Zone)
                            {
                                spriteData = particleAtlas.spriteData[j];
                            }
                            else
                            {
                                spriteData = particleAtlas.spriteData[particleData.spriteIndex];
                            }

                            float spritePixelWidth = spriteData.uvSizeAndPos.x * particleAtlas.atlas.texture.width;
                            float spritePixelHeight = spriteData.uvSizeAndPos.y * particleAtlas.atlas.texture.height;
                            float scale = graphRowHeight / spritePixelHeight;
                            float scaledWidth = spritePixelWidth * scale;
                            float scaledHeight = spritePixelHeight * scale;

                            GUI.BeginGroup(barRect);
                            {
                                int texSpriteIndex = 0;
                                for (float x = 0; x < barWidth; x += scaledWidth)
                                {
                                    for(float y = 0;  y < barHeight; y += scaledHeight)
                                    {
                                        ParticleSpriteData s = particleAtlas.spriteData[texSpriteIndex % particleAtlas.spriteData.Length];

                                        Vector2 uvPos = new Vector2(s.uvSizeAndPos.z, s.uvSizeAndPos.w);
                                        Vector2 uvSize = new Vector2(s.uvSizeAndPos.x, s.uvSizeAndPos.y);
                                        Rect uvRect = new Rect(uvPos, uvSize);

                                        Rect spriteRect = new Rect(x, y, scaledWidth, scaledHeight);
                                        GUI.DrawTextureWithTexCoords(spriteRect, particleAtlas.atlas.texture, uvRect);
                                        texSpriteIndex++;
                                    }
                                }
                            }
                            GUI.EndGroup();

                            if (e.type == EventType.MouseDown && barRect.Contains(e.mousePosition))
                            {
                                selectedParticlAtlasIndex = i;
                                selectedPosDataIndex = j;

                                int mouseTicketXPos = (int)((e.mousePosition.x - graphRect.xMin) / totalTicketCheckXIncrement);
                                int mouseDepthYPos = (int)((e.mousePosition.y - graphRect.yMin) / graphRowHeight);

                                dragOffsetStartX = mouseTicketXPos - particleData.ticketCheckStart;
                                dragOffsetStartY = mouseDepthYPos - (int)particleData.depth;

                                dragOffsetWidth = (int)(mouseTicketXPos - ticketCheckSize);
                                dragOffsetHeight = (int)(mouseDepthYPos - particleData.depthSize);

                                isAdjustingMetersStart = e.mousePosition.x < barRect.center.x;
                                isAdjustingDepthSize = e.mousePosition.y > barRect.center.y && particleAtlas.particleType == ParticleType.Zone;
                                
                                selectedSpriteIndex = particleData.spriteIndex;
                                selectedParticleAtlas = trip.particleAtlasArray[selectedParticlAtlasIndex];
                                selectedParticleCount = particleData.particleCount;

                            }

                            if (selectedParticlAtlasIndex != i || selectedPosDataIndex != j) continue;

                            Handles.DrawSolidRectangleWithOutline(barRect, Color.clear, Color.blueViolet);
                            
                            particleData.spriteIndex = selectedSpriteIndex;
                            particleData.particleCount = selectedParticleCount;

                            if (e.type == EventType.MouseDrag)
                            {
                                int mouseTicketXPos = (int)((e.mousePosition.x - graphRect.xMin) / totalTicketCheckXIncrement);
                                int mouseDepthYPos = (int)((e.mousePosition.y - graphRect.yMin) / graphRowHeight);

                                if (isAdjustingMetersStart)
                                {
                                    int curTicketStart = mouseTicketXPos - dragOffsetStartX;
                                    particleData.ticketCheckStart = Mathf.Clamp(curTicketStart, 0, particleData.ticketCheckEnd - 1);
                                }
                                else
                                {
                                    int curTicketEnd = particleData.ticketCheckStart + (mouseTicketXPos - dragOffsetWidth);
                                    particleData.ticketCheckEnd = Mathf.Clamp(curTicketEnd, particleData.ticketCheckStart + 1 , totalTicketChecks);
                                }
                                if (isAdjustingDepthSize)
                                {
                                    int curDepthSize = mouseDepthYPos - dragOffsetHeight;
                                    particleData.depthSize = Mathf.Clamp(curDepthSize, 1, (int)FAR_CLIP - particleData.depth);
                                }
                                else
                                {
                                    int curDepth = mouseDepthYPos - dragOffsetStartY;
                                    particleData.depth = Mathf.Clamp(curDepth, 1, (int)FAR_CLIP);
                                }

                                switch (particleData.depth)
                                {
                                    case TRAIN_LINE_DEPTH:
                                    {
                                        particleData.height = TRAIN_LINE_HEIGHT;
                                    }
                                    break;
                                    case TRAIN_TRACKS_DEPTH:
                                    {
                                        particleData.height = TRAIN_WHEEL_HEIGHT;
                                    }
                                    break;

                                    default:
                                    {
                                        particleData.height = 0;
                                    }
                                    break;
                                }

                            }

                            particleAtlas.posData[j] = particleData;

                            if (e.type == EventType.MouseUp)
                            {
                                particleAtlas.posData = particleAtlas.posData.OrderBy(p => p.ticketCheckStart).ToArray();
                                EditorUtility.SetDirty(particleAtlas);
                                AssetDatabase.SaveAssetIfDirty(particleAtlas);
                            }
                            Repaint();
                        }
                    }
                }
                Handles.EndGUI();
            }
            GUI.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }
}


[InitializeOnLoad]
public static class AtlasTripEditorBootstrap
{
    static AtlasTripEditorBootstrap()
    {
        EditorApplication.delayCall += OnReload;
    }

    private static void OnReload()
    {
        AtlasTripEditor window = EditorWindow.GetWindow<AtlasTripEditor>();
        window?.Init();
    }
}

/*   


       if (selectedScrollIndex == i)
       {
           Handles.DrawSolidRectangleWithOutline(scrollBarRect, Color.clear, Color.blueViolet);

           scrollSprite.particleType = selectedScrollSpriteType;
           scrollSprite.verticalPosType = selectedHeightPositionType;

           switch (selectedHeightPositionType)
           {
               case ParticleVerticalPositionType.WorldZero:
               {
                   scrollSprite.height = 0;
               }
               break;
               case ParticleVerticalPositionType.TrainLine:
               {
                   scrollSprite.height = TRAIN_LINE_HEIGHT;
               }
               break;
               case ParticleVerticalPositionType.TrainWheels:
               {
                   scrollSprite.height = TRAIN_WHEEL_HEIGHT;
               }
               break;
           }

           int maxIndex = scrollSprite.particleType == ParticleWidthType.Sliced ? scrollSprite.atlas.slicedSprites.Length - 1 : scrollSprite.atlas.simpleSprites.Length - 1;

           scrollSprite.spriteIndex = Mathf.Clamp(selectedScrollSpriteIndex, 0, maxIndex);
           scrollSprite.atlas = selectedAtlasSO;

           if (e.type == EventType.MouseDrag)
           {
               int mouseX = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.totalTicketsToCheck);
               if (isAdjustingMetersStart)
               {
                   scrollSprite.ticketCheckStart = mouseX - dragOffsetStartX;
               }
               else
               {
                   scrollSprite.ticketCheckEnd = scrollSprite.ticketCheckStart + (mouseX - dragOffsetWidth);
               }

               int mouseY = (int)(((e.mousePosition.y - graphRect.yMin) / graphRect.height) * FAR_CLIP);

               scrollSprite.depth = mouseY - dragOffsetStartY;
               Repaint();
           }
       }


       if (e.type == EventType.MouseUp)
       {
           var oldOrder = trip.scrollAtlasArray;

           var newOrder = trip.scrollAtlasArray
               .OrderBy(s => s.ticketCheckStart)
               .ThenBy(s => s.depth)
               .ToArray();

           // Check if order actually changed
           bool changed = !oldOrder.SequenceEqual(newOrder);

           if (changed)
           {
               trip.scrollAtlasArray = newOrder;
               selectedScrollIndex = -1;
           }

           e.Use();
           EditorUtility.SetDirty(trip);
           AssetDatabase.SaveAssets();
           AssetDatabase.Refresh();
       }
       scrollSprite.ticketCheckStart = Mathf.Clamp(scrollSprite.ticketCheckStart, 0, scrollSprite.ticketCheckEnd - 1);
       scrollSprite.ticketCheckEnd = Mathf.Clamp(scrollSprite.ticketCheckEnd, scrollSprite.ticketCheckStart + 1, trip.totalTicketsToCheck);
       scrollSprite.depth = (int)Mathf.Clamp(scrollSprite.depth, 0, FAR_CLIP);

   }

   Handles.EndGUI();
   GUI.EndScrollView();
   EditorGUILayout.EndVertical();
}
}
/*
       if (trip != null && selectedScrollIndex != -1 && trip.scrollAtlasArray.Length > 0)
       {
           if (selectedScrollIndex >= trip.scrollAtlasArray.Length - 1) selectedScrollIndex = trip.scrollAtlasArray.Length - 1;
           ParticleData selectedScrollSprite = trip.scrollAtlasArray[selectedScrollIndex];

           EditorGUILayout.BeginVertical();
           {
               EditorGUI.BeginChangeCheck();
               {
                   selectedAtlasSO =  (AtlasSO)EditorGUILayout.ObjectField("Change Atlas", selectedScrollSprite.atlas, typeof(AtlasSO), allowSceneObjects: false, GUIWidth);

               }
               if (selectedAtlasSO != null && EditorGUI.EndChangeCheck())
               {
                   if (selectedAtlasSO.simpleSprites.Length > 0)
                   {
                       selectedScrollSpriteType = ParticleWidthType.Simple;
                   }
                   else
                   {
                       selectedScrollSpriteType = ParticleWidthType.Sliced;
                   }
               }
           }
           EditorGUILayout.EndVertical();

           if (selectedAtlasSO != null)
           {
               EditorGUILayout.BeginVertical();
               {
                   int maxSpriteCount = selectedScrollSprite.particleType == ParticleWidthType.Sliced ? selectedAtlasSO.slicedSprites.Length : selectedAtlasSO.simpleSprites.Length;
                   selectedScrollSpriteIndex = (int)EditorGUILayout.IntSlider("Scroll Sprite Index", selectedScrollSpriteIndex, 0, maxSpriteCount - 1, GUIWidth);
               }
               EditorGUILayout.EndVertical();

               EditorGUILayout.BeginVertical();
               {
                   List<ParticleWidthType> validTypes = new List<ParticleWidthType>();

                   if (selectedAtlasSO.slicedSprites.Length > 0)
                   {
                       validTypes.Add(ParticleWidthType.Sliced);
                   }
                   if (selectedAtlasSO.simpleSprites.Length > 0)
                   {
                       validTypes.Add(ParticleWidthType.Simple);
                       validTypes.Add(ParticleWidthType.Tiled);
                   }
                   string[] options = validTypes.Select(v => v.ToString()).ToArray();
                   int currentIndex = validTypes.IndexOf(selectedScrollSpriteType);
                   if (currentIndex < 0) currentIndex = 0;
                   int newIndex = EditorGUILayout.Popup("Scroll Sprite Type", currentIndex, options);
                   selectedScrollSpriteType = validTypes[newIndex];
               }
               EditorGUILayout.EndVertical();
               EditorGUILayout.BeginVertical();
               {
                   selectedHeightPositionType = (ParticleVerticalPositionType)EditorGUILayout.EnumPopup("Height Position", selectedHeightPositionType, GUIWidth);
               }
               EditorGUILayout.EndVertical();
           }

           EditorGUILayout.BeginVertical();
           {
               bool addScrollSprite = GUILayout.Button("Add Scroll Sprite", GUIWidth);
               if (addScrollSprite)
               {
                   List<ParticleData> scrollSpriteList = trip.scrollAtlasArray.ToList();
                   scrollSpriteList.Add(selectedScrollSprite);
                   trip.scrollAtlasArray = scrollSpriteList.ToArray();
                   EditorUtility.SetDirty(trip);
                   Repaint();
               }
           }
           EditorGUILayout.EndVertical();
           EditorGUILayout.BeginVertical();
           {
               bool removeScrollSprite = GUILayout.Button("Remove Scroll Sprite", GUIWidth);
               if (removeScrollSprite)
               {
                   List<ParticleData> scrollSpriteList = trip.scrollAtlasArray.ToList();
                   scrollSpriteList.Remove(selectedScrollSprite);
                   trip.scrollAtlasArray = scrollSpriteList.ToArray();
                   selectedScrollIndex = Mathf.Max(0, selectedScrollIndex - 1);
                   EditorUtility.SetDirty(trip);
                   Repaint();
               }
           }
           EditorGUILayout.EndVertical();
       }
   }
   EditorGUILayout.EndHorizontal();

}
private void DrawGraph()
{
   EditorGUILayout.BeginVertical();

   Rect contentRect = new Rect(0, 0, position.width, 4250);
   Vector2 graphPos = new Vector2(PADDING, 50 + HEADER_COL_HEIGHT);
   Vector2 graphSize = new Vector2(contentRect.width - PADDING * 2, contentRect.height - graphPos.y);

   Handles.BeginGUI();
   GUIStyle zoneAreaLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.UpperLeft, normal = { textColor = Color.white } };
   GUIStyle stationLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.UpperLeft, normal = { textColor = Color.black } };

   Event e = Event.current;

   trip.totalTicketsToCheck = 0;
   for(int i = 0; i < trip.stationsDataArray.Length; i++)
   {
       StationSO station = trip.stationsDataArray[i];
       trip.totalTicketsToCheck += station.ticketsToCheckBeforeSpawn;
   }

   int stationIndex = 0;
   int ticketChecks = 0;
   for (int i = 0; i <= trip.totalTicketsToCheck; i++)
   {
       float t = (i) / (float)trip.totalTicketsToCheck;
       float posX = graphPos.x + t * graphSize.x;

       Vector2 p1 = new Vector2(posX, graphPos.y);
       Vector2 p2 = new Vector2(posX, graphPos.y + graphSize.y);
       Handles.DrawLine(p1, p2);

       StationSO station = trip.stationsDataArray[stationIndex];

       if (station.ticketsToCheckBeforeSpawn == ticketChecks)
       {
           float posY = graphPos.y - STATION_RECT_Y_OFFSET;
           Rect stationRect = new Rect(posX, posY, STATION_RECT_SIZE, STATION_RECT_SIZE);
           Color zoneColor = (selectedStationIndex == i) ? Color.orangeRed : Color.lawnGreen;
           Handles.DrawSolidRectangleWithOutline(stationRect, zoneColor, Color.black);

           Rect stationLabelRect = new Rect(stationRect.xMin, stationRect.yMin - HEADER_COL_HEIGHT, 200, HEADER_COL_HEIGHT);
           GUI.Label(stationLabelRect, station.station_prefab.name, stationLabelStyle);
           stationIndex++;

           ticketChecks = 0;
       }
       ticketChecks++;
   }

   Rect viewRect = new Rect(0, 0, position.width, position.height);
   scroll = GUI.BeginScrollView(viewRect, scroll, contentRect);
   Rect graphRect = new Rect(graphPos, graphSize);

   float sectionHeight = graphRect.height / (FAR_CLIP + 1);
   for (int i = 0; i <= FAR_CLIP; i++)
   {
       if (i > MAIN_MIN && i < MAIN_MAX && i != TRAIN_TRACKS_DEPTH && i != TRAIN_TRACKS_DEPTH + 1 && i != TRAIN_LINE_DEPTH) continue;

       float curYPos = graphRect.yMin + (i * sectionHeight);
       Vector2 p1 = new Vector2(graphRect.xMin, curYPos);
       Vector2 p2 = new Vector2(graphRect.xMax, curYPos);
       Handles.DrawLine(p1, p2);

       Vector2 depthLabelPos = new Vector2(graphRect.xMin - 50, curYPos + (sectionHeight * 0.5f));
       if (i == TRAIN_TRACKS_DEPTH)
       {
           Handles.Label(depthLabelPos, "TRACKS");
       }
       else if (i == TRAIN_LINE_DEPTH)
       {

           Handles.Label(depthLabelPos, "LINE");
       }
       else
       {
           Handles.Label(depthLabelPos, i.ToString());
       }
   }

   for (int i = 0; i < trip.particelAtlasArray.Length; i++)
   {
       ParticleAtlas zoneSpawnerData = trip.particelAtlasArray[i];

       for (int j = 0; j < zoneSpawnerData.zoneAtlases.Length; j++)
       {
           ref AtlasSpawn.ZoneSprite zoneAtlas = ref zoneSpawnerData.zoneAtlases[j];

           if (zoneAtlas.atlas == null) continue;

           float ticketCheckSize = (float)(zoneAtlas.ticketCheckEnd - zoneAtlas.ticketCheckStart);
           float barWidth = (ticketCheckSize / (float)trip.totalTicketsToCheck) * graphRect.width;
           float barX = graphRect.xMin + ((float)zoneAtlas.ticketCheckStart / (float)trip.totalTicketsToCheck) * graphRect.width;

           float barY = 0;
           switch (zoneSpawnerData.label)
           {
               case ZoneLabel.Foreground0:
               {
                   barY = graphRect.yMin + (FORE_MIN * sectionHeight);
               }
               break;
               case ZoneLabel.Middleground0:
               {
                   barY = graphRect.yMin + (MID_MIN * sectionHeight);
               }
               break;
               case ZoneLabel.Middleground1:
               {
                   barY = graphRect.yMin + ((MID_MIN + 1) * sectionHeight);
               }
               break;
               case ZoneLabel.Middleground2:
               {
                   barY = graphRect.yMin + ((MID_MIN + 2) * sectionHeight);
               }
               break;
               case ZoneLabel.Middleground3:
               {
                   barY = graphRect.yMin + ((MID_MIN + 3) * sectionHeight);
               }
               break;
               case ZoneLabel.Background0:
               {
                   barY = graphRect.yMin + ((BACK_MIN) * sectionHeight);
               }
               break;
               case ZoneLabel.Background1:
               {
                   barY = graphRect.yMin + ((BACK_MIN + 1) * sectionHeight);
               }
               break;
               case ZoneLabel.Background2:
               {
                   barY = graphRect.yMin + ((BACK_MIN + 2) * sectionHeight);
               }
               break;
           }

           Rect zoneBarRect = new Rect(barX, barY, barWidth, sectionHeight);

           SimpleSprite sampleSprite;
           if (zoneAtlas.zoneType == ZoneSpriteType.Simple)
           {
               sampleSprite = zoneAtlas.atlas.simpleSprites[0];
           }
           else
           {
               sampleSprite = zoneAtlas.atlas.slicedSprites[0].sprite;
           }

           Vector2 zoneUVPos = new Vector2(sampleSprite.uvSizeAndPos.z, sampleSprite.uvSizeAndPos.w);
           Vector2 zoneUVSize = new Vector2(sampleSprite.uvSizeAndPos.x, sampleSprite.uvSizeAndPos.y);
           Rect zoneUVRect = new Rect(zoneUVPos, zoneUVSize);

           float spritePixelWidth = sampleSprite.uvSizeAndPos.x * zoneAtlas.atlas.texture.width;
           float spritePixelHeight = sampleSprite.uvSizeAndPos.y * zoneAtlas.atlas.texture.height;
           float scale = zoneBarRect.height / spritePixelHeight;
           float scaledWidth = spritePixelWidth * scale;
           float scaledHeight = spritePixelHeight * scale;

           GUI.BeginGroup(zoneBarRect);

           for (float x = 0; x < barWidth; x += scaledWidth)
           {
               Rect r = new Rect(x, 0, scaledWidth, scaledHeight);
               GUI.DrawTextureWithTexCoords(r, zoneAtlas.atlas.texture, zoneUVRect);
           }

           GUI.EndGroup();

           if (e.type == EventType.MouseDown && zoneBarRect.Contains(e.mousePosition))
           {
               selectedStationIndex = -1;
               selectedParticleIndex = i;
               selectedScrollIndex = -1;
               selectedIndex_zone = j;
               int mouseMeters = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.totalTicketsToCheck);
               dragOffsetStartX = mouseMeters - zoneAtlas.ticketCheckStart;
               dragOffsetWidth = mouseMeters - (int)ticketCheckSize;
               isAdjustingMetersStart = e.mousePosition.x < zoneBarRect.center.x;
           }
           if (selectedParticleIndex == i && selectedIndex_zone == j)
           {
               Handles.DrawSolidRectangleWithOutline(zoneBarRect, Color.clear, Color.blueViolet);
               if (e.type == EventType.MouseDrag)
               {
                   int mouseT = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.totalTicketsToCheck);
                   if (isAdjustingMetersStart)
                   {
                       zoneAtlas.ticketCheckStart = mouseT - dragOffsetStartX;
                   }
                   else
                   {
                       zoneAtlas.ticketCheckEnd = zoneAtlas.ticketCheckStart + (mouseT - dragOffsetWidth);
                   }
                   EditorUtility.SetDirty(trip);
                   Repaint();
               }
           }

           if (e.type == EventType.MouseUp)
           {
               switch (zoneAtlas.zoneType)
               {
                   case ZoneSpriteType.Simple:
                   {
                       zoneAtlas.uvSizeAndPos = new Vector4[zoneAtlas.atlas.simpleSprites.Length];
                       zoneAtlas.worldPivotAndSize = new Vector4[zoneAtlas.atlas.simpleSprites.Length];
                       for (int k = 0; k < zoneAtlas.atlas.simpleSprites.Length; k++)
                       {
                           SimpleSprite sprite = zoneAtlas.atlas.simpleSprites[k];
                           zoneAtlas.uvSizeAndPos[k] = sprite.uvSizeAndPos;

                           Vector4 pivotAndSize = new Vector4(0, 0, sprite.worldSize.x, sprite.worldSize.y);
                           zoneAtlas.worldPivotAndSize[k] = pivotAndSize;
                       }
                   }
                   break;

                   case ZoneSpriteType.Sliced:
                   {
                       int sliceArraySize = zoneAtlas.atlas.slicedSprites.Length * 9;
                       zoneAtlas.uvSizeAndPos = new Vector4[sliceArraySize];
                       zoneAtlas.worldPivotAndSize = new Vector4[sliceArraySize];
                       zoneAtlas.sliceOffsetsAndSizes = new Vector4[sliceArraySize];
                       int index = 0;
                       for (int k = 0; k < zoneAtlas.atlas.slicedSprites.Length; k++)
                       {
                           SliceSprite slicedSprite = zoneAtlas.atlas.slicedSprites[k];

                           float centerWorldSliceWidth = slicedSprite.sprite.worldSize.x - slicedSprite.worldSlices.x - slicedSprite.worldSlices.y;
                           float centerWorldSliceHeight = slicedSprite.sprite.worldSize.y - slicedSprite.worldSlices.z - slicedSprite.worldSlices.w;

                           float rightColPos = slicedSprite.worldSlices.x + centerWorldSliceWidth;
                           float topRowPos = slicedSprite.worldSlices.z + centerWorldSliceHeight;

                           Vector4[] worldPivotsAndSizes = new Vector4[]
                           {
                               new Vector4(0, 0, slicedSprite.worldSlices.x, slicedSprite.worldSlices.z),
                               new Vector4(-slicedSprite.worldSlices.x, 0, centerWorldSliceWidth, slicedSprite.worldSlices.z),
                               new Vector4(-slicedSprite.worldSlices.x, 0, slicedSprite.worldSlices.y, slicedSprite.worldSlices.z),

                               new Vector4(0, -slicedSprite.worldSlices.z, slicedSprite.worldSlices.x, centerWorldSliceHeight),
                               new Vector4(-slicedSprite.worldSlices.x, -slicedSprite.worldSlices.z, centerWorldSliceWidth, centerWorldSliceHeight),
                               new Vector4(-slicedSprite.worldSlices.x, -slicedSprite.worldSlices.z, slicedSprite.worldSlices.y, centerWorldSliceHeight),

                               new Vector4(0, -slicedSprite.worldSlices.z, slicedSprite.worldSlices.x, slicedSprite.worldSlices.w),
                               new Vector4(-slicedSprite.worldSlices.x, -slicedSprite.worldSlices.z, centerWorldSliceWidth, slicedSprite.worldSlices.w),
                               new Vector4(-slicedSprite.worldSlices.x, -slicedSprite.worldSlices.z, slicedSprite.worldSlices.y, slicedSprite.worldSlices.w),
                           };
                           Vector4[] sliceOffsetsAndSizes = new Vector4[]
                           {
                               new Vector4(0, 0, 0, 0),
                               new Vector4(0, 0, centerWorldSliceWidth, 0),
                               new Vector4(centerWorldSliceWidth, 0, 0, 0),

                               new Vector4(0, 0, 0, centerWorldSliceHeight),
                               new Vector4(0, 0, centerWorldSliceWidth, centerWorldSliceHeight),
                               new Vector4(centerWorldSliceWidth, 0, 0, centerWorldSliceHeight),

                               new Vector4(0, centerWorldSliceHeight, 0, 0),
                               new Vector4(0, centerWorldSliceHeight, centerWorldSliceWidth, 0),
                               new Vector4(centerWorldSliceWidth, centerWorldSliceHeight, 0, 0)

                           };

                           for (int l = 0; l < 9; l++)
                           {
                               zoneAtlas.uvSizeAndPos[index] = zoneAtlas.atlas.slicedSprites[k].uvSizeAndPos[l];
                               zoneAtlas.worldPivotAndSize[index] = worldPivotsAndSizes[l];
                               zoneAtlas.sliceOffsetsAndSizes[index] = sliceOffsetsAndSizes[l];
                               index++;
                           }
                       }
                   }
                   break;
               }

               if (selectedIndex_zone == i)
               {
                   e.Use();
               }
           }
           zoneAtlas.ticketCheckStart = Mathf.Clamp(zoneAtlas.ticketCheckStart, 0, zoneAtlas.ticketCheckEnd - 1);
           zoneAtlas.ticketCheckEnd = Mathf.Clamp(zoneAtlas.ticketCheckEnd, zoneAtlas.ticketCheckStart + 1, trip.totalTicketsToCheck);
       }
   }

   for (int i = 0; i < trip.scrollAtlasArray.Length; i++)
   {
       ref ParticleData scrollSprite = ref trip.scrollAtlasArray[i];

       if (scrollSprite.atlas == null) continue;

       float ticketCheckSize = (float)(scrollSprite.ticketCheckEnd -  scrollSprite.ticketCheckStart);
       float barWidth = (ticketCheckSize / (float)trip.totalTicketsToCheck) * graphRect.width;
       float barX = graphRect.xMin + ((float)scrollSprite.ticketCheckStart / (float)trip.totalTicketsToCheck) * graphRect.width;

       float barY = graphRect.yMin + ((float)scrollSprite.depth * sectionHeight);

       Rect scrollBarRect = new Rect(barX, barY, barWidth, sectionHeight);

       SimpleSprite sprite;
       if (scrollSprite.particleType == ParticleWidthType.Sliced)
       {
           sprite = scrollSprite.atlas.slicedSprites[scrollSprite.spriteIndex].sprite;
       }
       else
       {
           sprite = scrollSprite.atlas.simpleSprites[scrollSprite.spriteIndex];
       }

       Vector2 scrollUVPos = new Vector2(sprite.uvSizeAndPos.z, sprite.uvSizeAndPos.w);
       Vector2 scrollUVSize = new Vector2(sprite.uvSizeAndPos.x, sprite.uvSizeAndPos.y);
       Rect scrollUVRect = new Rect(scrollUVPos, scrollUVSize);

       float spritePixelWidth = sprite.uvSizeAndPos.x * scrollSprite.atlas.texture.width;
       float spritePixelHeight = sprite.uvSizeAndPos.y * scrollSprite.atlas.texture.height;
       float scale = scrollBarRect.height / spritePixelHeight;
       float scaledWidth = spritePixelWidth * scale;
       float scaledHeight = spritePixelHeight * scale;

       EditorGUI.DrawRect(scrollBarRect, Color.white);
       GUI.BeginGroup(scrollBarRect);
       {
           for (float x = 0; x < barWidth; x += scaledWidth)
           {
               Rect r = new Rect(x, 0, scaledWidth, scaledHeight);
               GUI.DrawTextureWithTexCoords(r, scrollSprite.atlas.texture, scrollUVRect);
           }
       }
       GUI.EndGroup();

       if (e.type == EventType.MouseDown && scrollBarRect.Contains(e.mousePosition))
       {
           selectedStationIndex = -1;
           selectedScrollIndex = i;
           selectedParticleIndex = -1;
           selectedIndex_zone = -1;
           int mouseStartX = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.totalTicketsToCheck);
           int mouseStartY = (int)(((e.mousePosition.y - graphRect.yMin) / graphRect.height) * FAR_CLIP);
           dragOffsetStartX = mouseStartX - scrollSprite.ticketCheckStart;
           dragOffsetWidth = mouseStartX - (int)ticketCheckSize;
           dragOffsetStartY = mouseStartY - scrollSprite.depth;
           isAdjustingMetersStart = e.mousePosition.x < scrollBarRect.center.x;

           selectedScrollSpriteIndex = scrollSprite.spriteIndex;
           selectedScrollSpriteType = scrollSprite.particleType;
           selectedHeightPositionType = scrollSprite.verticalPosType;
           selectedAtlasSO = scrollSprite.atlas;
       }

       if (selectedScrollIndex == i)
       {
           Handles.DrawSolidRectangleWithOutline(scrollBarRect, Color.clear, Color.blueViolet);

           scrollSprite.particleType = selectedScrollSpriteType;
           scrollSprite.verticalPosType = selectedHeightPositionType;

           switch(selectedHeightPositionType)
           {
               case ParticleVerticalPositionType.WorldZero:
               {
                   scrollSprite.height = 0;
               }
               break;
               case ParticleVerticalPositionType.TrainLine:
               {
                   scrollSprite.height = TRAIN_LINE_HEIGHT;
               }
               break;
               case ParticleVerticalPositionType.TrainWheels:
               {
                   scrollSprite.height = TRAIN_WHEEL_HEIGHT;
               }
               break;
           }

           int maxIndex = scrollSprite.particleType == ParticleWidthType.Sliced ? scrollSprite.atlas.slicedSprites.Length - 1 : scrollSprite.atlas.simpleSprites.Length - 1;

           scrollSprite.spriteIndex = Mathf.Clamp(selectedScrollSpriteIndex, 0, maxIndex);
           scrollSprite.atlas = selectedAtlasSO;

           if (e.type == EventType.MouseDrag)
           {
               int mouseX = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.totalTicketsToCheck);
               if (isAdjustingMetersStart)
               {
                   scrollSprite.ticketCheckStart = mouseX - dragOffsetStartX;
               }
               else
               {
                   scrollSprite.ticketCheckEnd = scrollSprite.ticketCheckStart + (mouseX - dragOffsetWidth);
               }

               int mouseY = (int)(((e.mousePosition.y - graphRect.yMin) / graphRect.height) * FAR_CLIP);

               scrollSprite.depth = mouseY - dragOffsetStartY;
               Repaint();
           }
       }


       if (e.type == EventType.MouseUp)
       {
           var oldOrder = trip.scrollAtlasArray;

           var newOrder = trip.scrollAtlasArray
               .OrderBy(s => s.ticketCheckStart)
               .ThenBy(s => s.depth)
               .ToArray();

           // Check if order actually changed
           bool changed = !oldOrder.SequenceEqual(newOrder);

           if (changed)
           {
               trip.scrollAtlasArray = newOrder;
               selectedScrollIndex = -1;
           }

           e.Use();
           EditorUtility.SetDirty(trip);
           AssetDatabase.SaveAssets();
           AssetDatabase.Refresh();
       }
       scrollSprite.ticketCheckStart = Mathf.Clamp(scrollSprite.ticketCheckStart, 0, scrollSprite.ticketCheckEnd - 1);
       scrollSprite.ticketCheckEnd = Mathf.Clamp(scrollSprite.ticketCheckEnd, scrollSprite.ticketCheckStart + 1, trip.totalTicketsToCheck);
       scrollSprite.depth = (int)Mathf.Clamp(scrollSprite.depth, 0, FAR_CLIP);

   }

   Handles.EndGUI();
   GUI.EndScrollView();
   EditorGUILayout.EndVertical();
}
}

*/