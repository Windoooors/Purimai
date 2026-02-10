using UI.Result;
using UnityEngine;

namespace Game.Notes.Taps
{
    public class Tap : TapBasedNote
    {
        public bool isStarHead;
        public bool isNoSpinningStarHead;
        public bool isBreak;

        public float rotateSpeed;

        public SpriteRenderer tapSpriteRenderer;
        public Transform tapTransform;

        private TapOrLineTransform _tapOrLineTransform = new();

        private void Update()
        {
            if (!ChartPlayer.Instance.isPlaying)
                return;

            if (headJudged)
                return;

            GetTapOrLineTransform(ref _tapOrLineTransform);

            if (!_tapOrLineTransform.Shown)
            {
                NoteContentRoot.SetActive(false);
                return;
            }

            if (_tapOrLineTransform.Shown && !headJudged && !NoteContentRoot.activeSelf)
                NoteContentRoot.SetActive(true);

            if (!headJudged && ChartPlayer.Instance.GetTime() >
                timing + ChartPlayer.Instance.tapJudgeSettings.lateGoodTiming + ChartPlayer.Instance.judgeDelay)
            {
                headJudged = true;
                judgeState = JudgeState.Miss;

                if (isBreak)
                    Scoreboard.BreakCount.Count(JudgeState.Miss);
                else
                    Scoreboard.TapCount.Count(JudgeState.Miss);

                Scoreboard.ResetCombo();

                PlayJudgeAnimation();

                SimulatedSensor.OnTap -= Judge;

                NoteContentRoot.SetActive(false);
            }

            tapTransform.position = Lanes.Instance.startPoints[lane - 1].position +
                                    (Lanes.Instance.endPoints[lane - 1].position -
                                     Lanes.Instance.startPoints[lane - 1].position) *
                                    _tapOrLineTransform.PositionInLane;
            tapTransform.localScale = _tapOrLineTransform.Scale;

            var color = new Color(1, 1, 1, _tapOrLineTransform.Alpha);
            tapSpriteRenderer.color = color;
            lineSpriteRenderer.color = color;

            lineTransform.localScale = (NoteGenerator.GetInstance.originCircleScale +
                                        (1 - NoteGenerator.GetInstance.originCircleScale) *
                                        _tapOrLineTransform.PositionInLane)
                                       * Vector3.one;

            if (!isNoSpinningStarHead && (isStarHead || isBreak))
                tapTransform.Rotate(new Vector3(0, 0,
                    isStarHead ? -180 * Time.deltaTime * rotateSpeed : 400 * Time.deltaTime));
        }

        protected override void LateStart()
        {
            transform.position = Vector3.zero;
            tapTransform.localScale = Vector3.zero;
            tapTransform.position *= NoteGenerator.GetInstance.originCircleScale;
            tapSpriteRenderer.color = new Color(1, 1, 1, 0);

            if (isBreak)
                Scoreboard.BreakCount.TotalCount++;
            else
                Scoreboard.TapCount.TotalCount++;
        }

        public override void RegisterTapEvent()
        {
            SimulatedSensor.OnTap += Judge;
        }

        private void Judge(object sender, TouchEventArgs e)
        {
            var parsed = int.TryParse(e.SensorId.Replace("A", ""), out var touchedLane);
            if (!parsed)
                return;

            if (touchedLane != lane)
                return;

            var noteGenerator = NoteGenerator.GetInstance;

            if (indexInLane != 0 && !noteGenerator.LaneList[lane - 1][indexInLane - 1].headJudged)
                return;

            var deltaTiming = timing - ChartPlayer.Instance.GetTime(true) + ChartPlayer.Instance.judgeDelay;

            var judgeSettings = ChartPlayer.Instance.tapJudgeSettings;

            var state = GetJudgeState(deltaTiming, judgeSettings);

            headJudged = state.judged;

            if (!headJudged)
                return;

            judgeState = state.Item1;

            isFast = state.isFast;

            if (isBreak)
                Scoreboard.BreakCount.Count(judgeState);
            else
                Scoreboard.TapCount.Count(judgeState);

            Scoreboard.Combo++;

            PlayJudgeAnimation();

            PlayJudgeSound(isBreak, judgeState);

            tapSpriteRenderer.enabled = false;

            SimulatedSensor.OnTap -= Judge;

            NoteContentRoot.SetActive(false);
        }
    }
}