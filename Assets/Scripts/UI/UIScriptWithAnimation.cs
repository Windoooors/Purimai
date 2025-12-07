using System.Collections.Generic;
using LitMotion;
using UnityEngine;

namespace UI
{
    public class UIScriptWithAnimation : MonoBehaviour
    {
        private readonly List<MotionHandle> _motionHandles = new();

        public void ClearMotion(bool tryComplete = false)
        {
            foreach (var motionHandle in _motionHandles)
                if (tryComplete) motionHandle.TryComplete();
                else motionHandle.TryCancel();

            _motionHandles.Clear();
        }

        protected void AddMotionHandle(MotionHandle motionHandle, bool clearList = true)
        {
            if (clearList)
                ClearMotion();

            _motionHandles.Add(motionHandle);
        }
    }
}