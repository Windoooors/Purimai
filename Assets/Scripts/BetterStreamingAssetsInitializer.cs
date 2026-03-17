using UnityEngine;

public static class BetterStreamingAssetsInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnBeforeSceneLoad()
    {
        BetterStreamingAssets.Initialize();
    }
}