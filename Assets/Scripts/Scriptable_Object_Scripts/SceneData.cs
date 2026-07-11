using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Scenes;

[CreateAssetMenu(fileName = "SceneData", menuName = "Midnight Line SOs / Scene Data")]
public class SceneData : ScriptableObject
{
    public Scene demoScene;
    public Scene scoreScene;
    public SceneType activeSceneType;

    public bool sceneLoaded;
}
