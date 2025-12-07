using UnityEngine;

namespace Game.Notes.Taps
{
    public class EachLine : TapBasedNote
    {
        private bool _emerging;
        private bool _moving;

        private void Update()
        {
            if (!ChartPlayer.Instance.isPlaying)
                return;

            if (ChartPlayer.Instance.time >= timing - (IsAdxFlowSpeedStyle ? 1.5f : 2f) * EmergingDuration &&
                ChartPlayer.Instance.time < timing - 1 * EmergingDuration && !_emerging)
            {
                NoteContentRoot.SetActive(true);
                _emerging = true;
                lineSpriteRenderer.color = Color.white;
            }

            if (ChartPlayer.Instance.time >= timing - 1 * EmergingDuration && _emerging && !_moving)
            {
                _emerging = false;
                _moving = true;
            }

            if (ChartPlayer.Instance.time > timing + 100)
            {
                _moving = false;
                lineSpriteRenderer.enabled = false;

                NoteContentRoot.SetActive(false);
            }

            if (_moving) lineTransform.localScale += LineExpansionSpeed * Time.deltaTime * Vector3.one;
        }

        protected override void LateStart()
        {
            var laneIndex = lane - 1;
            transform.eulerAngles =
                Lanes.Instance.startPoints[laneIndex].rotation.eulerAngles + new Vector3(0, 0, 22.5f);
        }
    }
}