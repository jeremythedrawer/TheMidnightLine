using UnityEditor;
using UnityEngine;
using static AtlasSpawn;
public class AtlasTripViewer : EditorWindow
{
    public TripSO trip;
    public TrainStatsSO trainStats;
    const float HEADER_COL_WIDTH = 300;
    const float HEADER_COL_HEIGHT = 20;
    const float padding = 100;
    const float zonePadding = 10;

    private int selectedZoneIndex;
    private int dragOffsetZoneMetersLength;
    private int dragOffsetZoneMetersStart;

    private int selectedStationIndex;
    private int dragOffsetStation;
    [MenuItem("Tools/Atlas Trip Viewer")]
    private static void Open()
    {
        GetWindow<AtlasTripViewer>("Atlas Trip Viewer");
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
        trainStats = (TrainStatsSO)EditorGUILayout.ObjectField("Train Stats", trainStats, typeof(TrainStatsSO), allowSceneObjects: false, GUIWidth);
        EditorGUILayout.EndHorizontal();

        if (trip == null) return;

        EditorGUILayout.BeginHorizontal();
        DrawGraph();
        EditorGUILayout.EndHorizontal();

    }
    private void DrawGraph()
    {
        Vector2 graphSize = new Vector2(position.width - padding * 2, position.height - padding * 2 - HEADER_COL_HEIGHT);
        Vector2 graphPos = new Vector2(padding, padding + HEADER_COL_HEIGHT);
        Rect graphRect = new Rect(graphPos, graphSize);
        Handles.BeginGUI();
        Handles.DrawSolidRectangleWithOutline(graphRect, Color.clear, Color.white);

        for (int i = 0; i < ZONE_SPAWNER_COUNT - 1; i++)
        {
            float t = (i + 1) / (float)ZONE_SPAWNER_COUNT;
            float curHeight = graphRect.yMin + t * graphRect.height;

            Vector2 p1 = new Vector2(graphRect.xMin, curHeight);
            Vector2 p2 = new Vector2(graphRect.xMax, curHeight);
            Handles.DrawLine(p1, p2);
        }

        GUIStyle spawnerLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.UpperLeft, normal = { textColor = Color.white } };
        for (int i = 0; i < ZONE_SPAWNER_COUNT; i++)
        {
            float yOffset = (1 / (float)ZONE_SPAWNER_COUNT) * graphRect.height * 0.5f;
            float t = i / (float)ZONE_SPAWNER_COUNT;
            float curX = graphRect.xMin - (padding * 0.5f);
            float curY = (graphRect.yMin + t * graphRect.height) + yOffset;
            Rect spawnerLabelRect = new Rect(curX, curY, 20, 200);
            GUI.Label(spawnerLabelRect, ((AtlasSpawn.ZoneSpawner)i).ToString()[0].ToString(), spawnerLabelStyle);
        }

        GUIStyle zoneLabelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.UpperLeft, normal = { textColor = Color.black } };
        Event e = Event.current;
        for (int i = 0; i < trip.zones.Length; i++)
        {
            Zone zone = trip.zones[i];

            float zoneWidth = ((float)zone.metersLength / (float)trip.tripMeters) * graphRect.width;
            float zoneHeight = (graphRect.height / (float)ZONE_SPAWNER_COUNT) - (zonePadding * 2);

            float zoneX = graphRect.xMin + ((float)zone.metersStart / (float)trip.tripMeters) * graphRect.width;
            float zoneY = (graphRect.yMin + ((float)zone.spawnerArea / ZONE_SPAWNER_COUNT) * graphRect.height) + zonePadding;

            Rect zoneRect = new Rect(zoneX, zoneY, zoneWidth, zoneHeight);

            Color zoneColor = (selectedZoneIndex == i) ? Color.hotPink : Color.lightSeaGreen;
            Handles.DrawSolidRectangleWithOutline(zoneRect, zoneColor, Color.black);
            Rect zoneLabelRect = new Rect(zoneRect.xMin, zoneRect.yMin, 200, 20);
            GUI.Label(zoneLabelRect, zone.atlas.particleType.ToString(), zoneLabelStyle);

            if (e.type == EventType.MouseDown && zoneRect.Contains(e.mousePosition))
            {
                selectedStationIndex = -1;
                selectedZoneIndex = i;
                int mouseMeters = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.tripMeters);
                dragOffsetZoneMetersStart = mouseMeters - zone.metersStart;
                dragOffsetZoneMetersLength = mouseMeters - zone.metersLength;

            }
            if (selectedZoneIndex == i && e.type == EventType.MouseDrag)
            {
                int mouseMeters = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.tripMeters);
                if (e.mousePosition.x < zoneRect.center.x)
                {
                    zone.metersStart = mouseMeters - dragOffsetZoneMetersStart;
                    zone.metersStart = Mathf.Clamp(zone.metersStart, 0, trip.tripMeters - zone.metersLength);
                }
                else
                {
                    zone.metersLength = mouseMeters - dragOffsetZoneMetersLength;
                    zone.metersLength = Mathf.Clamp(zone.metersLength, 0, trip.tripMeters - zone.metersStart);
                }

                trip.zones[i] = zone;
                EditorUtility.SetDirty(trip);
                Repaint();
            }

            if (e.type == EventType.MouseUp && selectedZoneIndex == i)
            {
                selectedZoneIndex = -1;
                e.Use();
            }
        }

        for (int i = 0; i < trip.stations.Length; i++)
        {
            StationSO station = trip.stations[i];
            float rectSize = 20;
            float posX = graphRect.xMin + ((float)station.metersPosition / (float)trip.tripMeters) * graphRect.width;
            float posY = graphRect.yMin - 20;
            Rect stationRect = new Rect(posX, posY, rectSize, rectSize);
            Color zoneColor = (selectedStationIndex == i) ? Color.orangeRed : Color.lawnGreen;
            Handles.DrawSolidRectangleWithOutline(stationRect, zoneColor, Color.black);

            Rect stationLabelRect = new Rect(stationRect.xMin, stationRect.yMin, 200, 20);
            GUI.Label(stationLabelRect, station.station_prefab.name, zoneLabelStyle);
            if (e.type == EventType.MouseDown && stationRect.Contains(e.mousePosition))
            {
                selectedZoneIndex = -1;
                selectedStationIndex = i;
                int mouseMeters = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.tripMeters);
                dragOffsetStation = mouseMeters - station.metersPosition;
            }
            if (selectedStationIndex == i && e.type == EventType.MouseDrag)
            {
                int mouseMeters = (int)(((e.mousePosition.x - graphRect.xMin) / graphRect.width) * trip.tripMeters);
                station.metersPosition = mouseMeters - dragOffsetStation;
                station.metersPosition = Mathf.Clamp(station.metersPosition, 0, trip.tripMeters);

                EditorUtility.SetDirty(trip);
                Repaint();
            }

            if (e.type == EventType.MouseUp && selectedStationIndex == i)
            {
                selectedStationIndex = -1;
                e.Use();
            }
        }
        float metersTravelledGraphPosX = graphRect.xMin + ((trainStats.metersTravelled / trip.tripMeters) * graphRect.width);
        Vector2 meterTravelledTop = new Vector2(metersTravelledGraphPosX, graphRect.yMin);
        Vector2 meterTravelledBottom = new Vector2(metersTravelledGraphPosX, graphRect.yMax);
        Handles.color = Color.yellow;
        Handles.DrawLine(meterTravelledTop, meterTravelledBottom);

        Handles.EndGUI();
    }
}

