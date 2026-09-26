using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;




#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif

using static AtlasUI;
public class CamUIController : MonoBehaviour
{
    const float CARRIAGE_MAP_MOVE_TIME = 0.75f;

    public FadeBlack fadeBlack;

    public BottomPanel bottomPanel;

    public CameraData camData;
    public ActionData actionData;
    public TrainData trainData;
    public UIData uiData;

    [Header("Generated")]

    public float carriageMapMoveClock;

    public CancellationTokenSource ctsCarriageMapMove;

    private void OnEnable()
    {
    }
    private void OnDisable()
    {
    }
    public void Start()
    {
        Init();
    }
    private void Init()
    {
        fadeBlack.SetAlpha(value: 1);
        fadeBlack.FadeOut(time: 1);
    }
    public void WriteTitleText()
    {
        fadeBlack.AndTextBit(ColorBits.Invert);
        fadeBlack.WriteTitleText();
    }
    public void SetTitleAlpha(float alpha)
    {
        fadeBlack.SetTitleTextAlpha(alpha);
    }
    public void DissappearTitleAlpha()
    {
        fadeBlack.DissappearText(1f);
    }
    public void SetTitleText()
    {
        fadeBlack.SetTitleText();
    }
    private void FadeOut()
    {
        fadeBlack.FadeOut(time: 0.5f);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof (CamUIController))]
public class CamUIControllerEditor : Editor
{
    CamUIController camUIController;
    private void OnSceneGUI()
    {
        camUIController = (CamUIController)target;
        DrawBottomPanelGUI();
    }
    private void DrawBottomPanelGUI()
    {
        Vector3 activeBottomPanelWorldPos = camUIController.transform.TransformPoint(camUIController.uiData.activeBottomPanelLocalPos);
        Vector3 inactiveBottomPanelWorldPos = camUIController.transform.TransformPoint(camUIController.uiData.inactiveBottomPaneLocalPos);

        EditorGUI.BeginChangeCheck();

        Vector3 newActiveActiveBottomPanelWorldPos = Handles.PositionHandle(activeBottomPanelWorldPos, camUIController.transform.rotation);
        Vector3 newInactiveActiveBottomPanelWorldPos = Handles.PositionHandle(inactiveBottomPanelWorldPos, camUIController.transform.rotation);



        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(camUIController, "Move Bottom Panel Position");
            camUIController.uiData.activeBottomPanelLocalPos = camUIController.transform.InverseTransformPoint(newActiveActiveBottomPanelWorldPos);
            camUIController.uiData.inactiveBottomPaneLocalPos = camUIController.transform.InverseTransformPoint(newInactiveActiveBottomPanelWorldPos);

            EditorUtility.SetDirty(camUIController);
        }

        Handles.color = Color.yellow;

        Handles.RectangleHandleCap(0, camUIController.transform.position, camUIController.transform.rotation, camUIController.camData.bounds.extents, EventType.Repaint);

        Handles.color = Color.cyan;
        Bounds bottomPanel = camUIController.bottomPanel.atlasRenderer.bounds;

        Vector3 activeBottomPanelHandleRectPos = new Vector3();
        activeBottomPanelHandleRectPos.x = activeBottomPanelWorldPos.x;
        activeBottomPanelHandleRectPos.y = activeBottomPanelWorldPos.y + bottomPanel.extents.y;
        activeBottomPanelHandleRectPos.z = activeBottomPanelWorldPos.z;
        Handles.RectangleHandleCap(1, activeBottomPanelHandleRectPos, Quaternion.identity, bottomPanel.extents, EventType.Repaint);

        Vector3 inactiveBottomPanelHandleRectPos = new Vector3();
        inactiveBottomPanelHandleRectPos.x = inactiveBottomPanelWorldPos.x;
        inactiveBottomPanelHandleRectPos.y = inactiveBottomPanelWorldPos.y + bottomPanel.extents.y;
        inactiveBottomPanelHandleRectPos.z = inactiveBottomPanelWorldPos.z;
        Handles.RectangleHandleCap(1, inactiveBottomPanelHandleRectPos, Quaternion.identity, bottomPanel.extents, EventType.Repaint);

        GUIStyle notepadHandleStyle = new GUIStyle();
        notepadHandleStyle.alignment = TextAnchor.LowerCenter;
        notepadHandleStyle.normal.textColor = Handles.color;

        GUIContent activeNotepadHandleContent = new GUIContent("Bottom Panel Active");
        Handles.Label(activeBottomPanelHandleRectPos, activeNotepadHandleContent, notepadHandleStyle);

        GUIContent inactiveNotepadHandleContent = new GUIContent("Bottom Panel Inactive");
        Handles.Label(inactiveBottomPanelHandleRectPos, inactiveNotepadHandleContent, notepadHandleStyle);
    }
}
#endif
