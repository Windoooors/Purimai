using UnityEngine;

namespace Game.Notes.Taps
{
    public class EachLine : TapBasedNote
    {
        private void Update()
        {
            if (!ChartPlayer.Instance.isPlaying)
                return;

            var tapOrLineTransform = GetTapOrLineTransform();

            var shown = tapOrLineTransform.Shown && ChartPlayer.Instance.GetTime() < timing + 300;

            if (shown)
            {
                NoteContentRoot.SetActive(true);
            }
            else
            {
                NoteContentRoot.SetActive(false);
                return;
            }

            transform.localScale = (NoteGenerator.Instance.originCircleScale +
                                    (1 - NoteGenerator.Instance.originCircleScale) *
                                    tapOrLineTransform.PositionInLane)
                                   * Vector3.one;
        }

        protected override void LateStart()
        {
            var laneIndex = lane - 1;
            transform.eulerAngles =
                Lanes.Instance.startPoints[laneIndex].rotation.eulerAngles + new Vector3(0, 0, 22.5f);
        }
    }
}