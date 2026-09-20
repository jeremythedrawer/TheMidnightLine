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
    public CarriageMap carriageMap;

    public NotepadData notepadData;
    public CameraData camData;
    public ActionData actionData;
    public TrainData trainData;

    [Header("Generated")]
    public Notepad notepad;

    public Vector3 inactiveCarriageMapLocalPos;
    public Vector3 activeCarriageMapLocalPos;

    public float carriageMapMoveClock;

    public CancellationTokenSource ctsCarriageMapMove;

    private void OnEnable()
    {
        actionData.onGiveNotepad += CreateNotepad;
        
        actionData.onShowCarriageMap += MoveCarriageMapToActivePosition;
        
        actionData.onHideCarriageMap += MoveCarriageMapToInacivePosition;

        actionData.onFocus += FadeToFocus;
        actionData.onUnfocus += FadeOut;
    }
    private void OnDisable()
    {
        actionData.onGiveNotepad -= CreateNotepad;
        
        actionData.onShowCarriageMap -= MoveCarriageMapToActivePosition;
        
        actionData.onHideCarriageMap -= MoveCarriageMapToInacivePosition;
        
        actionData.onFocus -= FadeToFocus;
        actionData.onUnfocus -= FadeOut;
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

    private void CreateNotepad()
    {
        notepad = Instantiate(notepadData.notepadPrefab, transform);
        notepad.Init();
    }
    private void FadeToFocus()
    {
        fadeBlack.FadeIn(value: 1, time: 0.25f, alpha: 0.25f, fadeBlackZPos: trainData.depthSections.carriageSeat + 1, usePassengerStencil: true);
    }
    private void FadeOut()
    {
        fadeBlack.FadeOut(time: 0.5f);
    }
    private void MoveCarriageMapToActivePosition()
    {
        ctsCarriageMapMove?.Cancel();
        ctsCarriageMapMove?.Dispose();
        ctsCarriageMapMove = new CancellationTokenSource();

        MovingCarriageMapToActivePosition().Forget();

        fadeBlack.FadeIn(value: 1, time: CARRIAGE_MAP_MOVE_TIME, uvPosX: 0.27f, alpha: 0.075f, fadeBlackZPos: carriageMap.transform.localPosition.z + 0.5f);
    }

    private void MoveCarriageMapToInacivePosition()
    {
        ctsCarriageMapMove?.Cancel();
        ctsCarriageMapMove?.Dispose();
        ctsCarriageMapMove = new CancellationTokenSource();

        MovingCarriageMapToInactivePosition().Forget();

        fadeBlack.FadeOut(time: CARRIAGE_MAP_MOVE_TIME);
    }
    private async UniTask MovingCarriageMapToActivePosition()
    {
        try
        {
            while (carriageMapMoveClock < CARRIAGE_MAP_MOVE_TIME)
            {
                float t = carriageMapMoveClock / CARRIAGE_MAP_MOVE_TIME;

                Vector3 curLocalPos = Vector3.Lerp(inactiveCarriageMapLocalPos, activeCarriageMapLocalPos, t);
                carriageMap.transform.localPosition = curLocalPos;
                carriageMapMoveClock += Time.deltaTime;
                await UniTask.Yield(ctsCarriageMapMove.Token);
            }
        }
        catch(OperationCanceledException)
        {

        }
    }

    private async UniTask MovingCarriageMapToInactivePosition()
    {
        try
        {
            while (carriageMapMoveClock > 0)
            {
                float t = carriageMapMoveClock / CARRIAGE_MAP_MOVE_TIME;
                Vector3 curLocalPos = Vector3.Lerp(inactiveCarriageMapLocalPos, activeCarriageMapLocalPos, t);
                carriageMap.transform.localPosition = curLocalPos;
                carriageMapMoveClock -= Time.deltaTime;
                await UniTask.Yield(ctsCarriageMapMove.Token);
            }
        }
        catch (OperationCanceledException)
        {

        }
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
        DrawNotepadGUI();
        DrawCarriageMapGUI();
    }
    private void DrawNotepadGUI()
    {
        Vector3 activeNotepadWorldPos = camUIController.transform.TransformPoint(camUIController.notepadData.activeLocalPos);
        Vector3 inactiveNotepadWorldPos = camUIController.transform.TransformPoint(camUIController.notepadData.inactiveLocalPos);
        Vector3 hoverNotepadWorldPos = camUIController.transform.TransformPoint(camUIController.notepadData.hoverLocalPos);

        EditorGUI.BeginChangeCheck();

        Vector3 newActiveNotepadWorldPos = Handles.PositionHandle(activeNotepadWorldPos, camUIController.transform.rotation);
        Vector3 newInactiveNotepadWorldPos = Handles.PositionHandle(inactiveNotepadWorldPos, camUIController.transform.rotation);
        Vector3 newHoverNotepadWorldPos = Handles.PositionHandle(hoverNotepadWorldPos, camUIController.transform.rotation);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(camUIController, "Move Active Position");
            camUIController.notepadData.activeLocalPos = camUIController.transform.InverseTransformPoint(newActiveNotepadWorldPos);
            camUIController.notepadData.inactiveLocalPos = camUIController.transform.InverseTransformPoint(newInactiveNotepadWorldPos);
            camUIController.notepadData.hoverLocalPos = camUIController.transform.InverseTransformPoint(newHoverNotepadWorldPos);

            EditorUtility.SetDirty(camUIController);
        }


        Handles.color = Color.yellow;
        Handles.RectangleHandleCap(0, camUIController.transform.position, camUIController.transform.rotation, camUIController.camData.bounds.extents, EventType.Repaint);

        Handles.color = Color.cyan;
        Bounds paperBounds = camUIController.notepadData.frontPagePrefab.paperRenderer.bounds;

        Vector3 activeNotepadHandleRectPos = new Vector3();
        activeNotepadHandleRectPos.x = activeNotepadWorldPos.x;
        activeNotepadHandleRectPos.y = activeNotepadWorldPos.y - paperBounds.extents.y;
        activeNotepadHandleRectPos.z = activeNotepadWorldPos.z;
        Handles.RectangleHandleCap(1, activeNotepadHandleRectPos, Quaternion.identity, paperBounds.extents, EventType.Repaint);

        Vector3 inactiveNotepadHandleRectPos = new Vector3();
        inactiveNotepadHandleRectPos.x = inactiveNotepadWorldPos.x;
        inactiveNotepadHandleRectPos.y = inactiveNotepadWorldPos.y - paperBounds.extents.y;
        inactiveNotepadHandleRectPos.z = inactiveNotepadWorldPos.z;
        Handles.RectangleHandleCap(1, inactiveNotepadHandleRectPos, Quaternion.identity, paperBounds.extents, EventType.Repaint);


        Vector2 hoverNotepadHandleRectSize = new Vector2();
        hoverNotepadHandleRectSize.x = paperBounds.extents.x;
        hoverNotepadHandleRectSize.y = ((hoverNotepadWorldPos.y - inactiveNotepadWorldPos.y) * 0.5f);

        Vector3 hoverNotepadHandleRectPos = new Vector3();
        hoverNotepadHandleRectPos.x = hoverNotepadWorldPos.x;
        hoverNotepadHandleRectPos.y = inactiveNotepadWorldPos.y + hoverNotepadHandleRectSize.y;
        hoverNotepadHandleRectPos.z = hoverNotepadWorldPos.z;
        Handles.RectangleHandleCap(1, hoverNotepadHandleRectPos, Quaternion.identity, hoverNotepadHandleRectSize, EventType.Repaint);

        GUIStyle notepadHandleStyle = new GUIStyle();
        notepadHandleStyle.alignment = TextAnchor.LowerCenter;
        notepadHandleStyle.normal.textColor = Handles.color;

        GUIContent activeNotepadHandleContent = new GUIContent("Notepad Active");
        Handles.Label(activeNotepadHandleRectPos, activeNotepadHandleContent, notepadHandleStyle);

        GUIContent inactiveNotepadHandleContent = new GUIContent("Notepad Inactive");
        Handles.Label(inactiveNotepadHandleRectPos, inactiveNotepadHandleContent, notepadHandleStyle);

        GUIContent hoverHandleContent = new GUIContent("Notepad Hover");
        Handles.Label(hoverNotepadHandleRectPos, hoverHandleContent, notepadHandleStyle);
    }

    private void DrawCarriageMapGUI()
    {
        Vector3 activeWorldPos = camUIController.transform.TransformPoint(camUIController.activeCarriageMapLocalPos);
        Vector3 inactiveWorldPos = camUIController.transform.TransformPoint(camUIController.inactiveCarriageMapLocalPos);

        EditorGUI.BeginChangeCheck();

        Vector3 newActiveWorldPos = Handles.PositionHandle(activeWorldPos, camUIController.transform.rotation);
        Vector3 newInactiveWorldPos = Handles.PositionHandle(inactiveWorldPos, camUIController.transform.rotation);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(camUIController, "Move Active Position");
            camUIController.activeCarriageMapLocalPos = camUIController.transform.InverseTransformPoint(newActiveWorldPos);
            camUIController.inactiveCarriageMapLocalPos = camUIController.transform.InverseTransformPoint(newInactiveWorldPos);

            EditorUtility.SetDirty(camUIController);
        }

        Handles.color = Color.magenta;
        Bounds bounds = camUIController.carriageMap.backgroundRenderer.bounds;

        Vector3 activeHandleRectPos = new Vector3();
        activeHandleRectPos.x = activeWorldPos.x;
        activeHandleRectPos.y = activeWorldPos.y + bounds.extents.y;
        activeHandleRectPos.z = activeWorldPos.z;
        Handles.RectangleHandleCap(1, activeHandleRectPos, Quaternion.identity, bounds.extents, EventType.Repaint);

        Vector3 inactiveHandleRectPos = new Vector3();
        inactiveHandleRectPos.x = inactiveWorldPos.x;
        inactiveHandleRectPos.y = inactiveWorldPos.y + bounds.extents.y;
        inactiveHandleRectPos.z = inactiveWorldPos.z;
        Handles.RectangleHandleCap(1, inactiveHandleRectPos, Quaternion.identity, bounds.extents, EventType.Repaint);

        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.LowerCenter;
        style.normal.textColor = Handles.color;

        GUIContent activeContent = new GUIContent("Carriage Map Active");
        Handles.Label(activeHandleRectPos, activeContent, style);

        GUIContent inactiveContent = new GUIContent("Carriage Map Inactive");
        Handles.Label(inactiveHandleRectPos, inactiveContent, style);
    }
}
#endif
