using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Scenes;
public class SceneController : MonoBehaviour
{
    public SceneData sceneData;
    private void Awake()
    {
        sceneData.activeSceneType = SceneType.Trip;
        DontDestroyOnLoad(this);
    }
}
