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
    public static ColorPicker NPCColorPicker;

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
    private void OnEnable()
    {
        SpyBrain.OnAfterOutcomeSequence += SetSceneTypeToStart;
    }
    private void OnDisable()
    {
        SpyBrain.OnAfterOutcomeSequence -= SetSceneTypeToStart;
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
    private void SetSceneTypeToStart()
    {
        sceneData.activeSceneType = SceneType.Start;
    }
    public static void SetClueColorPicker(ColorPicker colorPicker)
    {
        ClueColorPicker = colorPicker;
    }
    public static void SetNPCColorPicker(ColorPicker colorPicker)
    {
        NPCColorPicker = colorPicker;
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
    public static Notepad GetAndParentNotepad(Transform newTransform)
    {
        Notepad.transform.SetParent(newTransform, true);
        return Notepad;
    }
    public static Notepad GetNotepad()
    {
        return Notepad;
    }
    public static ColorPicker GetClueColorPicker()
    {
        return ClueColorPicker;
    }
    public static ColorPicker GetNPCColorPicker()
    {
        return NPCColorPicker;
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
