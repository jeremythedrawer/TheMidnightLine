using UnityEngine;
using Cysharp.Threading.Tasks;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
#endif

using static AtlasUI;
public class CamUIController : MonoBehaviour
{
    public FadeBlack fadeBlack;

    public NotepadData notepadData;
    public CameraData camData;
    

    [Header("Generated")]
    public Notepad notepad;

    private void OnEnable()
    {
        HenchmanBrain.OnGiveNotepad += CreateNotepad;
    }
    private void OnDisable()
    {
        HenchmanBrain.OnGiveNotepad -= CreateNotepad;
    }
    public void Start()
    {
        Init();
    }
    private void Init()
    {
        fadeBlack.SetAlpha(0);
        fadeBlack.FadeOut();
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
}

#if UNITY_EDITOR
[CustomEditor(typeof (CamUIController))]
public class CamUIControllerEditor : Editor
{
    private void OnSceneGUI()
    {
        CamUIController camUIController = (CamUIController)target;

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
}
#endif
