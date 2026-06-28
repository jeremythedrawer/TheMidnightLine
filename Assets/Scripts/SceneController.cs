using UnityEngine;
using static Scenes;
using static AtlasUI;
public class SceneController : MonoBehaviour
{
    public SceneData sceneData;
    public static Transform Transform;

    public static Notepad Notepad;
    public static ColorPicker ColorPicker;
    public static NPCPicker NPCPicker;
    public static SpyBrain Spy;

    public static InputManager InputManager;
    private void Awake()
    {
        sceneData.activeSceneType = SceneType.Trip;
        DontDestroyOnLoad(this);
        Transform = transform;
    }

    public static void KeepNotepad(Notepad notepad)
    {
        notepad.transform.SetParent(Transform, true);
        Notepad = notepad;
    }
    public static void SetColorPicker(ColorPicker colorPicker)
    {
        ColorPicker = colorPicker;
    }

    public static void SetNPCPicker(NPCPicker npcPicker)
    {
        NPCPicker = npcPicker;
    }

    public static void SetSpyBrain(SpyBrain spy)
    {
        Spy = spy;
    }

    public static void KeepInputManager(InputManager inputManager)
    {
        inputManager.transform.SetParent(Transform, true);
        InputManager = inputManager;
    }
    public static Notepad GetNotepad(Transform transform)
    {
        Notepad.transform.SetParent(transform, true);
        Notepad.transform.localPosition = NotepadInactivePos;
        return Notepad;
    }
    public static ColorPicker GetColorPicker()
    {
        return ColorPicker;
    }
    public static NPCPicker GetNPCPicker()
    {
        return NPCPicker;
    }
    public static InputManager GetInputManager()
    {
        return InputManager;
    }

    public static SpyBrain GetSpy()
    {
        return Spy;
    }
}
