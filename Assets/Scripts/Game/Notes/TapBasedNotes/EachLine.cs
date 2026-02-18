using UnityEngine;

namespace Game.Notes.TapBasedNotes
{
    public class EachLine : TapBasedNote
    {
        private TapOrLineTransform _tapOrLineTransform = new();

        public override void ManualUpdate()
        {
            if (!ChartPlayer.Instance.isPlaying)
                return;

            GetTapOrLineTransform(ref _tapOrLineTransform);

            var shown = _tapOrLineTransform.Shown && ChartPlayer.Instance.GetTime() < timing + 300;

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

        private bool _haveShown;

        protected override void LateStart()
        {
            var laneIndex = lane - 1;
            transform.eulerAngles =
                Lanes.Instance.startPoints[laneIndex].rotation.eulerAngles + new Vector3(0, 0, 22.5f);
        }
    }
}