using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scenes
{
    const float BLACK_SCENE_TIME = 3f;
    public enum SceneType
    { 
        Start,
        Trip,
        Score,
    }

    public static void SetScoreScene(SceneData sceneData)
    {
        SettingScoreScene(sceneData).Forget();
    }
    private static async UniTask SettingScoreScene(SceneData sceneData)
    {
        await UniTask.WaitForSeconds(BLACK_SCENE_TIME);

        sceneData.activeSceneType = SceneType.Score;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Score");

        while (!asyncLoad.isDone)
        {
            await UniTask.Yield();
        }
    }
}