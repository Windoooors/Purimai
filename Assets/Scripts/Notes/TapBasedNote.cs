using System;
using UnityEngine;

namespace Notes
{
    [Serializable]
    public class JudgeSettings
    {
        public int criticalPerfectTiming;
        public int semiCriticalPerfectTiming;
        public int perfectTiming;
        public int greatTiming;
        public int semiGreatTiming;
        public int quarterGreatTiming;
        public int goodTiming;
    }

    public abstract class TapBasedNote : MonoBehaviour
    {
        protected const float TimeOnScreenWithBasicSpeed = 2.04166667f;
        public int timing;

        public SpriteRenderer lineSpriteRenderer;
        public Transform lineTransform;
        public int lane;

        public JudgeSettings judgeSettings;

        protected int EmergingDuration;
        protected float LineExpansionSpeed;

        protected float Speed;

        private void Start()
        {
            var laneIndex = lane - 1;
            var endPoint = Lanes.Instance.endPoints[laneIndex];
            var startPoint = Lanes.Instance.startPoints[laneIndex];

            var distance = (endPoint.position - startPoint.position).magnitude;
            var speed = distance / TimeOnScreenWithBasicSpeed * ChartPlayer.Instance.flowSpeed;

            LineExpansionSpeed = (1 - NoteGenerator.Instance.originCircleScale) / TimeOnScreenWithBasicSpeed *
                                 ChartPlayer.Instance.flowSpeed;

            var emergingDuration = TimeOnScreenWithBasicSpeed / ChartPlayer.Instance.flowSpeed;

            Speed = speed;
            EmergingDuration = (int)(emergingDuration * 1000);

            transform.position = Vector3.zero;
            transform.rotation = Lanes.Instance.startPoints[laneIndex].rotation;

            lineSpriteRenderer.color = new Color(1, 1, 1, 0);
            lineTransform.localScale = NoteGenerator.Instance.originCircleScale * Vector3.one;

            LateStart();
        }

        protected virtual void LateStart()
        {
        }
    }
}