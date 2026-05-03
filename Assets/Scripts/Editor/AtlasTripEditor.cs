using UnityEditor;
using UnityEngine;
using static Atlas;
using static AtlasSpawn;
public class AtlasTripEditor : EditorWindow
{
    const float HEADER_COL_WIDTH = 300;
    const float HEADER_COL_HEIGHT = 20;
    const float PADDING = 100;
    const float BAR_PADDING = 10;
    const float STATION_RECT_SIZE = 20;
    const float STATION_RECT_Y_OFFSET = 25;

    public TripSO trip;

    private int selectedIndex_zone;
    private int selectedIndex;
    private int dragOffsetZoneMetersLength;
    private int dragOffsetZoneMetersStart;

    private int selectedIndex_station;

    private bool isAdjustingMetersStart;

    private Vector2 scroll;

    [MenuItem("Tools/Atlas Trip Editor")]
    private static void Open()
    {
        GetWindow<AtlasTripEditor>("Atlas Trip Editor");
    }

    private void Update()
    {
        Repaint();
    }
    private void OnGUI()
    {
        GUILayoutOption[] GUIWidth = { GUILayout.Width(HEADER_COL_WIDTH), GUILayout.Height(HEADER_COL_HEIGHT) };

        EditorGUILayout.BeginHorizontal();
        trip = (TripSO)EditorGUILayout.ObjectField("Trip", trip, typeof(TripSO), allowSceneObjects: false, GUIWidth);
        EditorGUILayout.EndHorizontal();

        if (trip == null) return;

        EditorGUILayout.BeginHorizontal();
        DrawGraph();
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
                Color zoneColor = (selectedIndex_station == i) ? Color.orangeRed : Color.lawnGreen;
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
            if (i > MAIN_MIN && i < MAIN_MAX) continue;
            float curYPos = graphRect.yMin + (i * sectionHeight);
            Vector2 p1 = new Vector2(graphRect.xMin, curYPos);
            Vector2 p2 = new Vector2(graphRect.xMax, curYPos);
            Handles.DrawLine(p1, p2);

            Vector2 depthLabelPos = new Vector2(graphRect.xMin - 50, curYPos + (sectionHeight * 0.5f));
            Handles.Label(depthLabelPos, i.ToString());
        }

        for (int i = 0; i < trip.zoneAreas.Length; i++)
        {
            ZoneArea zoneSpawnerData = trip.zoneAreas[i];

            for (int j = 0; j < zoneSpawnerData.zoneSprites.Length; j++)
            {
                ref ZoneAtlas zoneAtlas = ref zoneSpawnerData.zoneSprites[j];

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
                    selectedIndex_station = -1;
                    selectedIndex = i;
                    selectedIndex_zone = j;
                    int mouseMeters = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.totalTicketsToCheck);
                    dragOffsetZoneMetersStart = mouseMeters - zoneAtlas.ticketCheckStart;
                    dragOffsetZoneMetersLength = mouseMeters - (int)ticketCheckSize;
                    isAdjustingMetersStart = e.mousePosition.x < zoneBarRect.center.x;
                }
                if (selectedIndex == i && selectedIndex_zone == j && e.type == EventType.MouseDrag)
                {
                    int mouseT = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.totalTicketsToCheck);
                    if (isAdjustingMetersStart)
                    {
                        zoneAtlas.ticketCheckStart = mouseT - dragOffsetZoneMetersStart;
                    }
                    else
                    {
                        zoneAtlas.ticketCheckEnd = zoneAtlas.ticketCheckStart + (mouseT - dragOffsetZoneMetersLength);
                    }


                    EditorUtility.SetDirty(trip);
                    Repaint();
                }

                if (e.type == EventType.MouseUp)
                {
                    switch (zoneAtlas.zoneType)
                    {
                        case ZoneSpriteType.Simple:
                        {
                            zoneAtlas.zoneUVSizeAndPosArray = new Vector4[zoneAtlas.atlas.simpleSprites.Length];
                            zoneAtlas.zoneWorldPivotsAndSizesArray = new Vector4[zoneAtlas.atlas.simpleSprites.Length];
                            for (int k = 0; k < zoneAtlas.atlas.simpleSprites.Length; k++)
                            {
                                SimpleSprite sprite = zoneAtlas.atlas.simpleSprites[k];
                                zoneAtlas.zoneUVSizeAndPosArray[k] = sprite.uvSizeAndPos;

                                Vector4 pivotAndSize = new Vector4(0, 0, sprite.worldSize.x, sprite.worldSize.y);
                                zoneAtlas.zoneWorldPivotsAndSizesArray[k] = pivotAndSize;
                            }
                        }
                        break;

                        case ZoneSpriteType.Sliced:
                        {
                            int sliceArraySize = zoneAtlas.atlas.slicedSprites.Length * 9;
                            zoneAtlas.zoneUVSizeAndPosArray = new Vector4[sliceArraySize];
                            zoneAtlas.zoneWorldPivotsAndSizesArray = new Vector4[sliceArraySize];
                            zoneAtlas.zoneSliceOffsetsAndSizes = new Vector4[sliceArraySize];
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
                                    zoneAtlas.zoneUVSizeAndPosArray[index] = zoneAtlas.atlas.slicedSprites[k].uvSizeAndPos[l];
                                    zoneAtlas.zoneWorldPivotsAndSizesArray[index] = worldPivotsAndSizes[l];
                                    zoneAtlas.zoneSliceOffsetsAndSizes[index] = sliceOffsetsAndSizes[l];
                                    index++;
                                }
                            }
                        }
                        break;
                    }

                    if (selectedIndex_zone == i)
                    {
                        selectedIndex_zone = -1;
                        e.Use();
                    }
                }
                zoneAtlas.ticketCheckStart = Mathf.Clamp(zoneAtlas.ticketCheckStart, 0, zoneAtlas.ticketCheckEnd - 1);
                zoneAtlas.ticketCheckEnd = Mathf.Clamp(zoneAtlas.ticketCheckEnd, zoneAtlas.ticketCheckStart + 1, trip.totalTicketsToCheck);
            }
        }

        for (int i = 0; i < trip.scrollSprites.Length; i++)
        {
            ref ScrollSprite scrollSprite = ref trip.scrollSprites[i];

            if (scrollSprite.atlas == null) continue;

            float ticketCheckSize = (float)(scrollSprite.ticketCheckEnd -  scrollSprite.ticketCheckStart);
            float barWidth = (ticketCheckSize / (float)trip.totalTicketsToCheck) * graphRect.width;
            float barX = graphRect.xMin + ((float)scrollSprite.ticketCheckStart / (float)trip.totalTicketsToCheck) * graphRect.width;

            float barY = graphRect.yMin + ((float)scrollSprite.depth * sectionHeight);

            Rect scrollBarRect = new Rect(barX, barY, barWidth, sectionHeight);

            SimpleSprite sprite;
            if (scrollSprite.scrollType == ScrollSpriteType.Sliced)
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

            GUI.BeginGroup(scrollBarRect);

            for (float x = 0; x < barWidth; x += scaledWidth)
            {
                Rect r = new Rect(x, 0, scaledWidth, scaledHeight);
                GUI.DrawTextureWithTexCoords(r, scrollSprite.atlas.texture, scrollUVRect);
            }

            GUI.EndGroup();

            if (e.type == EventType.MouseDown && scrollBarRect.Contains(e.mousePosition))
            {
                selectedIndex_station = -1;
                selectedIndex = i;
                int mouseStart = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.totalTicketsToCheck);
                dragOffsetZoneMetersStart = mouseStart - scrollSprite.ticketCheckStart;
                dragOffsetZoneMetersLength = mouseStart - (int)ticketCheckSize;
                isAdjustingMetersStart = e.mousePosition.x < scrollBarRect.center.x;
            }

            if (selectedIndex == i && e.type == EventType.MouseDrag)
            {
                int mouseT = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.totalTicketsToCheck);
                if (isAdjustingMetersStart)
                {
                    scrollSprite.ticketCheckStart = mouseT - dragOffsetZoneMetersStart;
                }
                else
                {
                    scrollSprite.ticketCheckEnd = scrollSprite.ticketCheckStart + (mouseT - dragOffsetZoneMetersLength);
                }


                EditorUtility.SetDirty(trip);
                Repaint();
            }

            if (e.type == EventType.MouseUp)
            {
                if (selectedIndex_zone == i)
                {
                    selectedIndex_zone = -1;
                    e.Use();
                }
            }
            scrollSprite.ticketCheckStart = Mathf.Clamp(scrollSprite.ticketCheckStart, 0, scrollSprite.ticketCheckEnd - 1);
            scrollSprite.ticketCheckEnd = Mathf.Clamp(scrollSprite.ticketCheckEnd, scrollSprite.ticketCheckStart + 1, trip.totalTicketsToCheck);
        }

        Handles.EndGUI();

        GUI.EndScrollView();
        EditorGUILayout.EndVertical();
    }
}

