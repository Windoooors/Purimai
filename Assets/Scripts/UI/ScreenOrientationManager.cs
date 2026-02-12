using System;
using UnityEngine;

namespace UI
{
    public class ScreenOrientationManager : MonoBehaviour
    {
        private static ScreenOrientationManager _instance;

        private ScreenOrientation _lastScreenOrientation = ScreenOrientation.AutoRotation;

        public Action ScreenChanged;
        public static ScreenOrientationManager Instance => _instance ?? FindAnyObjectByType<ScreenOrientationManager>();

        private void Awake()
        {
            _instance = this;
        }

        private void Update()
        {
            if (Screen.orientation != _lastScreenOrientation) ScreenChanged();

            _lastScreenOrientation = Screen.orientation;
        }

        public void EnablePortrait()
        {
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;

            Screen.orientation = ScreenOrientation.AutoRotation;
        }

        public void DisablePortrait()
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;

            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}