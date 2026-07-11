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

    public static void SetScoreScene(SceneData sceneData)
    {
        SettingScoreScene(sceneData).Forget();
    }
    private static async UniTask SettingScoreScene(SceneData sceneData)
    {
        await UniTask.WaitForSeconds(BLACK_SCENE_TIME);

        sceneData.sceneLoaded = false;
        if (sceneData.demoScene.isLoaded)
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(1);
            while (!asyncUnload.isDone) await UniTask.Yield();
        }
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(2, LoadSceneMode.Additive);
        while (!asyncLoad.isDone) await UniTask.Yield();
        

        sceneData.scoreScene = SceneManager.GetSceneByBuildIndex(2);
        SceneManager.SetActiveScene(sceneData.scoreScene);

        sceneData.activeSceneType = SceneType.Score;
        sceneData.sceneLoaded = true;
        OnLoadScore?.Invoke();
    }
    public static void SetTripScene(SceneData sceneData)
    {
        SettingTripScene(sceneData).Forget();
    }
    private static async UniTask SettingTripScene(SceneData sceneData)
    {
        if (sceneData.demoScene.isLoaded)
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(1);
            while(!asyncUnload.isDone) await UniTask.Yield();
        }

        await UniTask.WaitForSeconds(BLACK_SCENE_TIME);
        sceneData.sceneLoaded = false;

        if (sceneData.scoreScene.isLoaded)
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(2);
            while (!asyncUnload.isDone) await UniTask.Yield();
        }
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
        while (!asyncLoad.isDone) await UniTask.Yield();

        sceneData.demoScene = SceneManager.GetSceneByBuildIndex(1);
        SceneManager.SetActiveScene(sceneData.demoScene);

        sceneData.activeSceneType = SceneType.Trip;
        sceneData.sceneLoaded = true;

        OnLoadTrip0?.Invoke();
        OnLoadTrip1?.Invoke();
        OnLoadTrip2?.Invoke();
    }
}