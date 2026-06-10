using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Scenes;

[CreateAssetMenu(fileName = "SceneData", menuName = "Midnight Line SOs / Scene Data")]
public class SceneData : ScriptableObject
{
    public SceneType activeSceneType;
}
