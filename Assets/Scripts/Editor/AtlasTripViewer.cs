using UnityEditor;
using UnityEngine;
using static AtlasSpawn;
public class AtlasTripViewer : EditorWindow
{
    public TripSO trip;
    const float HEADER_COL_WIDTH = 300;
    const float HEADER_COL_HEIGHT = 20;
    const float padding = 50;
    const float zonePadding = 10;

    private int selectedZoneIndex;
    
    [MenuItem("Tools/Atlas Trip Viewer")]
    private static void Open()
    {
        GetWindow<AtlasTripViewer>("Atlas Trip Viewer");
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
        Vector2 graphSize = new Vector2(position.width - padding * 2, position.height - padding * 2 - HEADER_COL_HEIGHT);
        Vector2 graphPos = new Vector2(padding, padding + HEADER_COL_HEIGHT);
        Rect graphRect = new Rect(graphPos, graphSize);
        Handles.BeginGUI();
        Handles.DrawSolidRectangleWithOutline(graphRect, Color.clear, Color.white);

        for (int i = 0; i < SPAWNER_COUNT - 1; i++)
        {
            float t = (i + 1) / (float)SPAWNER_COUNT;
            float curHeight = graphRect.yMin + t * graphRect.height;

            Vector2 p1 = new Vector2(graphRect.xMin, curHeight);
            Vector2 p2 = new Vector2(graphRect.xMax, curHeight);
            Handles.DrawLine(p1, p2);
        }

        GUIStyle spawnerLabelStyle = new GUIStyle(EditorStyles.boldLabel){ alignment = TextAnchor.UpperLeft, normal = { textColor = Color.white } };
        for (int i = 0; i < SPAWNER_COUNT; i++)
        {
            float yOffset = (1 / (float)SPAWNER_COUNT) * graphRect.height * 0.5f;
            float t = i / (float)SPAWNER_COUNT;
            float curX = graphRect.xMin - (padding * 0.5f);
            float curY = (graphRect.yMin + t * graphRect.height) + yOffset;
            Rect spawnerLabelRect = new Rect(curX, curY, 20, 200);
            GUI.Label(spawnerLabelRect, ((Spawner)i).ToString()[0].ToString(), spawnerLabelStyle);
        }

        GUIStyle zoneLabelStyle = new GUIStyle(EditorStyles.boldLabel){ alignment = TextAnchor.UpperLeft, normal = { textColor = Color.black } };
        for (int i = 0; i < trip.zones.Length; i++)
        {
            Zone zone = trip.zones[i];

            float zoneWidth = ((float)zone.metersLength / (float)trip.tripMeters) * graphRect.width;
            float zoneHeight = (graphRect.height / (float)SPAWNER_COUNT) - (zonePadding * 2);

            float zoneX = graphRect.xMin + ((float)zone.metersStart / (float)trip.tripMeters) * graphRect.width;
            float zoneY = (graphRect.yMin + ((float)zone.spawner / SPAWNER_COUNT) * graphRect.height) + zonePadding;

            Rect zoneRect = new Rect(zoneX, zoneY, zoneWidth, zoneHeight);
            Handles.DrawSolidRectangleWithOutline(zoneRect, Color.green, Color.black);


            Rect zoneLabelRect = new Rect(zoneRect.xMin, zoneRect.yMin, 200, 20);
            GUI.Label(zoneLabelRect, zone.atlas.particleType.ToString(), zoneLabelStyle);
            
        }

        Handles.EndGUI();
    }
}
