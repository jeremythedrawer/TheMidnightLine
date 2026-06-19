using UnityEngine;
using static Scenes;
using static AtlasUI;
public class SceneController : MonoBehaviour
{
    public SceneData sceneData;
    public static Transform Transform;
    public static Notepad Notepad;
    public static ColorPicker ColorPicker;
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
    public static void KeepColorPicker(ColorPicker colorPicker)
    {
        colorPicker.transform.SetParent(Transform, true);
        ColorPicker = colorPicker;
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
    public static InputManager GetInputManager()
    {
        return InputManager;
    }
}
