using UnityEngine;

public static class BetterStreamingAssetsInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnBeforeSceneLoad()
    {
        BetterStreamingAssets.Initialize();
    }
}