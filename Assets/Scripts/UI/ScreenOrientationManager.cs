using System;
using System.Collections;
using UnityEngine;
using Logger = Logging.Logger;

namespace UI
{
    public class ScreenOrientationManager : MonoBehaviour
    {
        private static ScreenOrientationManager _instance;
        private ScreenOrientation _lastLandscapeOrientation = ScreenOrientation.LandscapeLeft;

        private ScreenOrientation _lastScreenOrientation = ScreenOrientation.AutoRotation;

        public Action ScreenChanged;
        public static ScreenOrientationManager Instance => _instance ?? FindAnyObjectByType<ScreenOrientationManager>();

        private void Awake()
        {
            _instance = this;
        }

        private void Update()
        {
            if (Screen.orientation != _lastScreenOrientation)
            {
                ScreenChanged();
                if (_lastScreenOrientation is ScreenOrientation.LandscapeLeft or ScreenOrientation.LandscapeRight)
                    _lastLandscapeOrientation = _lastScreenOrientation;
            }

            _lastScreenOrientation = Screen.orientation;
        }

        public void EnablePortrait()
        {
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;

            Screen.orientation = ScreenOrientation.AutoRotation;

            Logger.LogInfo("Screen rotation set to portrait-only.");
        }

        public void DisablePortrait()
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;

            Screen.orientation = _lastLandscapeOrientation;

            StartCoroutine(WaitAndEnableAutoRotation());

            Logger.LogInfo("Screen rotation set to horizontal-only.");

            return;

            IEnumerator WaitAndEnableAutoRotation()
            {
                yield return new WaitForSeconds(0.5f);
                Screen.orientation = ScreenOrientation.AutoRotation;
            }
        }
    }
}