using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scenes
{
    const float BLACK_SCENE_TIME = 3f;

    public static event Action OnLoadScore;
    public static event Action OnLoadTrip0;
    public static event Action OnLoadTrip1;
    public static event Action OnLoadTrip2;

    public enum SceneType
    { 
        Start,
        Trip,
        Score,
    }
    public static void SetScene(SceneData sceneData, SceneType sceneType, int sceneIndex)
    {
        SettingScene(sceneData, sceneType, sceneIndex).Forget();
    }
    private static async UniTask SettingScene(SceneData sceneData, SceneType sceneType, int sceneIndex)
    {
        await UniTask.WaitForSeconds(BLACK_SCENE_TIME);
        sceneData.sceneLoaded = false;

        for (int i = 1; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            Scene scene = SceneManager.GetSceneByBuildIndex(i);
            if (scene.isLoaded)
            {
                AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(scene);
                while (!asyncUnload.isDone) await UniTask.Yield();
                break;
            }
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);
        while (!asyncLoad.isDone) await UniTask.Yield();

        Scene newScene = SceneManager.GetSceneByBuildIndex(sceneIndex);
        
        SceneManager.SetActiveScene(newScene);

        sceneData.activeSceneType = sceneType;
        sceneData.sceneLoaded = true;

        switch (sceneType)
        { 
            case SceneType.Start:
            {

            }
            break;
            case SceneType.Trip:
            {
                OnLoadTrip0?.Invoke();
                OnLoadTrip1?.Invoke();
                OnLoadTrip2?.Invoke();
            }
            break;

            case SceneType.Score:
            {
                OnLoadScore?.Invoke();
            }
            break;
        }
    }
}