#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Editor
{
    public static class PostProcessIOS
    {
        [PostProcessBuild(1000)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS)
                return;

            var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            var root = plist.root;

            root.SetBoolean("UIApplicationSupportsGameMode", true);
            root.SetBoolean("UIRequiresFullScreen", true);
            root.SetBoolean("UIFileSharingEnabled", true);
            root.SetBoolean("LSSupportsOpeningDocumentsInPlace", true);
            if (root.values.ContainsKey("UIApplicationSceneManifest"))
            {
                var sceneManifest = root["UIApplicationSceneManifest"].AsDict();
                sceneManifest.SetBoolean("UIApplicationSupportsMultipleScenes", false);
            }
            else
            {
                var sceneManifest = root.CreateDict("UIApplicationSceneManifest");
                sceneManifest.SetBoolean("UIApplicationSupportsMultipleScenes", false);
            }

            File.WriteAllText(plistPath, plist.WriteToString());

            Debug.Log("Info.plist Modification Completed.");
        }
    }
}
#endif