using UnityEngine;

namespace Game.Notes.Taps
{
    public class EachLine : TapBasedNote
    {
        private TapOrLineTransform _tapOrLineTransform = new();

        private void Update()
        {
            if (!ChartPlayer.Instance.isPlaying)
                return;

            GetTapOrLineTransform(ref _tapOrLineTransform);

            var shown = _tapOrLineTransform.Shown && ChartPlayer.Instance.GetTime() < timing + 300;

            if (shown)
            {
                NoteContentRoot.SetActive(true);
            }
            else
            {
                NoteContentRoot.SetActive(false);
                return;
            }

            transform.localScale = (NoteGenerator.GetInstance.originCircleScale +
                                    (1 - NoteGenerator.GetInstance.originCircleScale) *
                                    _tapOrLineTransform.PositionInLane)
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