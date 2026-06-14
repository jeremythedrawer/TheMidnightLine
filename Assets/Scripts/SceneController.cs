using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Scenes;
using static AtlasUI;
public class SceneController : MonoBehaviour
{
    public SceneData sceneData;
    public static Transform Transform;
    public static Notepad Notepad;
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

    public static Notepad GetNotepad(Transform transform)
    {
        Notepad.transform.SetParent(transform, true);
        Notepad.transform.localPosition = NotepadInactivePos;
        return Notepad;
    }
}
