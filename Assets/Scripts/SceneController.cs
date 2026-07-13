using UnityEngine;
using static Scenes;
using static AtlasUI;
using UnityEngine.SceneManagement;
public class SceneController : MonoBehaviour
{
    public SceneData sceneData;
    public static Transform Transform;

    public static Notepad Notepad;
    public static ColorPicker ClueColorPicker;
    public static ColorPicker MainColorPicker;
    public static NPCPicker NPCPicker;
    public static UnlockPicker UnlockPicker;
    public static SpyBrain Spy;

    public static InputManager InputManager;

    private void Start()
    {
        Scenes.SetScene(sceneData, SceneType.Start, sceneIndex: 1);
        sceneData.activeSceneType = SceneType.Start;
        Transform = transform;
    }
    private void OnApplicationQuit()
    {
        sceneData.sceneLoaded = false;
    }
    public static void KeepNotepad(Notepad notepad)
    {
        notepad.transform.SetParent(Transform, true);
        Notepad = notepad;
    }
    public static void SetClueColorPicker(ColorPicker colorPicker)
    {
        ClueColorPicker = colorPicker;
    }
    public static void SetMainColorPicker(ColorPicker mainColorPicker)
    {
        MainColorPicker = mainColorPicker;
    }
    public static void SetNPCPicker(NPCPicker npcPicker)
    {
        NPCPicker = npcPicker;
    }

    public static void SetSpyBrain(SpyBrain spy)
    {
        Spy = spy;
    }
    public static void SetUnlockPicker(UnlockPicker unlockPicker)
    {
        UnlockPicker = unlockPicker;
    }
    public static void SetInputManager(InputManager inputManager)
    {
        InputManager = inputManager;
    }
    public static Notepad GetNotepad(Transform transform)
    {
        Notepad.transform.SetParent(transform, true);
        Notepad.transform.localPosition = NotepadInactiveLocalPos;
        return Notepad;
    }
    public static ColorPicker GetClueColorPicker()
    {
        return ClueColorPicker;
    }
    public static ColorPicker GetMainColorPicker()
    {
        return MainColorPicker;
    }
    public static NPCPicker GetNPCPicker()
    {
        return NPCPicker;
    }
    public static InputManager GetInputManager()
    {
        return InputManager;
    }
    public static UnlockPicker GetUnlockPicker()
    {
        return UnlockPicker;
    }
    public static SpyBrain GetSpy()
    {
        return Spy;
    }
}
