using UnityEngine;
using static Scenes;
using static AtlasUI;
using UnityEngine.SceneManagement;
public class SceneController : MonoBehaviour
{
    public SceneData sceneData;
    public static Transform Transform;

    public static Notepad Notepad;
    public static ColorPicker ColorPicker;
    public static NPCPicker NPCPicker;
    public static UnlockPicker UnlockPicker;
    public static SpyBrain Spy;

    public static InputManager InputManager;

    private void Awake()
    {
    }
    private void Start()
    {
        sceneData.demoScene = SceneManager.GetSceneByBuildIndex(1);
        sceneData.scoreScene = SceneManager.GetSceneByBuildIndex(2);

        Scenes.SetTripScene(sceneData);
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
    public static UnlockPicker GetUnlockPicker()
    {
        return UnlockPicker;
    }
    public static SpyBrain GetSpy()
    {
        return Spy;
    }
}
