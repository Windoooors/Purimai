using UnityEngine;

namespace Game.Notes.TapBasedNotes
{
    public class EachLine : TapBasedNote
    {
        private bool _haveShown;
        private TapOrLineTransform _tapOrLineTransform = new();

        public override void ManualUpdate()
        {
            if (!ChartPlayer.Instance.isPlaying)
                return;

            GetTapOrLineTransform(ref _tapOrLineTransform);

            var shown = _tapOrLineTransform.Shown && ChartPlayer.Instance.TimeInMilliseconds < timing + 300;

            if (shown)
            {
                _haveShown = true;
                
                NoteContentRoot.SetActive(true);
            }
            else
            {
                if (_haveShown)
                    enabled = false;

                NoteContentRoot.SetActive(false);
                return;
            }

            transform.localScale = (NoteGenerator.Instance.originCircleScale +
                                    (1 - NoteGenerator.Instance.originCircleScale) *
                                    _tapOrLineTransform.PositionInLane)
                                   * Vector3.one;
        }

        public override void AddAutoPlayKeyFrame()
        {
        }

        protected override void LateStart()
        {
            var laneIndex = lane - 1;
            transform.eulerAngles =
                Lanes.Instance.startPoints[laneIndex].rotation.eulerAngles + new Vector3(0, 0, 22.5f);
        }
    }
}