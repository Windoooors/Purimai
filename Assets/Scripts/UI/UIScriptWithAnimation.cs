using System.Collections.Generic;
using LitMotion;
using UnityEngine;

namespace UI
{
    public class UIScriptWithAnimation : MonoBehaviour
    {
        private readonly List<MotionHandle> _motionHandles = new();

        protected void ClearMotionHandles()
        {
            foreach (var motionHandle in _motionHandles) motionHandle.TryCancel();

            _motionHandles.Clear();
        }

        protected void AddMotionHandle(MotionHandle motionHandle, bool clearList = true)
        {
            if (clearList)
                ClearMotionHandles();

            _motionHandles.Add(motionHandle);
        }
    }
}