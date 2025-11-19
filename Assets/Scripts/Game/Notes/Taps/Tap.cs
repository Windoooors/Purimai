using LitMotion;
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

        private bool _emerging;
        private bool _moving;

        private void Update()
        {
            if (!ChartPlayer.Instance.isPlaying)
                return;

            if (headJudged)
                return;

            if (!headJudged && ChartPlayer.Instance.time >
                timing + ChartPlayer.Instance.tapJudgeSettings.lateGoodTiming + ChartPlayer.Instance.judgeDelay)
            {
                headJudged = true;
                judgeState = JudgeState.Miss;

                Scoreboard.DeductedScore += isBreak ? -2500 : -500;

                if (isBreak)
                    Scoreboard.BreakCount.Count(JudgeState.Miss);
                else
                    Scoreboard.TapCount.Count(JudgeState.Miss);

                Scoreboard.ResetCombo();

                tapSpriteRenderer.enabled = false;
                PlayJudgeAnimation();

                SimulatedSensor.OnTap -= Judge;

                NoteContentRoot.SetActive(false);
            }

            if (ChartPlayer.Instance.time > timing - 2 * EmergingDuration &&
                ChartPlayer.Instance.time < timing - 1 * EmergingDuration && !_emerging)
            {
                NoteContentRoot.SetActive(true);

                _emerging = true;

                lineSpriteRenderer.enabled = true;
                tapSpriteRenderer.enabled = true;

                transform.position = Vector3.zero;

                LMotion.Create(0, 1f, EmergingDuration / 1000f / (IsAdxFlowSpeedStyle ? 2 : 1))
                    .WithDelay(IsAdxFlowSpeedStyle ? EmergingDuration / 1000f / 2 : 0)
                    .WithEase(Ease.OutSine)
                    .Bind(x =>
                    {
                        tapSpriteRenderer.color = new Color(1, 1, 1, x);
                        lineSpriteRenderer.color = new Color(1, 1, 1, x);
                    });
                LMotion.Create(0, 1f, EmergingDuration / 1000f / (IsAdxFlowSpeedStyle ? 2 : 1))
                    .WithDelay(IsAdxFlowSpeedStyle ? EmergingDuration / 1000f / 2 : 0)
                    .WithEase(Ease.Linear)
                    .Bind(x => tapTransform.localScale = x * Vector3.one);
            }

            if (ChartPlayer.Instance.time > timing - 1 * EmergingDuration && _emerging && !_moving)
            {
                _emerging = false;
                _moving = true;
            }

            if (ChartPlayer.Instance.time > timing + 1000)
            {
                _moving = false;
                lineSpriteRenderer.enabled = false;
                tapSpriteRenderer.enabled = false;
            }

            if (_moving)
            {
                tapTransform.localScale = Vector3.one;
                tapTransform.position += Speed * Time.deltaTime * transform.up;

                lineTransform.localScale += LineExpansionSpeed * Time.deltaTime * Vector3.one;
            }

            if (!isNoSpinningStarHead && (isStarHead || isBreak))
                tapTransform.Rotate(new Vector3(0, 0,
                    isStarHead ? -180 * Time.deltaTime * rotateSpeed : 400 * Time.deltaTime));
        }

        protected override void LateStart()
        {
            transform.position = Vector3.zero;
            tapTransform.localScale = Vector3.zero;
            tapTransform.position *= NoteGenerator.Instance.originCircleScale;
            tapSpriteRenderer.color = new Color(1, 1, 1, 0);
            tapSpriteRenderer.enabled = false;
            lineSpriteRenderer.enabled = false;

            Scoreboard.TotalScore += isBreak ? 2500 : 500;
            Scoreboard.TotalScoreWithExtraScore += isBreak ? 2600 : 500;
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

            var noteGenerator = NoteGenerator.Instance;

            if (indexInLane != 0 && !noteGenerator.LaneList[lane - 1][indexInLane - 1].headJudged)
                return;

            var deltaTiming = timing - ChartPlayer.Instance.time + ChartPlayer.Instance.judgeDelay;

            var judgeSettings = ChartPlayer.Instance.tapJudgeSettings;

            var state = GetJudgeState(deltaTiming, judgeSettings);

            headJudged = state.judged;

            if (!headJudged)
                return;

            judgeState = state.Item1;

            isFast = state.isFast;

            var score = isBreak
                ? judgeState switch
                {
                    JudgeState.CriticalPerfect => 2600,
                    JudgeState.SemiCriticalPerfect => 2550,
                    JudgeState.Perfect => 2500,
                    JudgeState.Great => 2000,
                    JudgeState.SemiGreat => 1500,
                    JudgeState.QuarterGreat => 1250,
                    JudgeState.Good => 1000,
                    _ => 0
                }
                : judgeState switch
                {
                    JudgeState.CriticalPerfect or JudgeState.SemiCriticalPerfect or JudgeState.Perfect => 500,
                    JudgeState.Great or JudgeState.SemiGreat or JudgeState.QuarterGreat => 400,
                    JudgeState.Good => 250,
                    _ => 0
                };

            Scoreboard.Score += score;

            Scoreboard.DeductedScore += isBreak ? score - 2500 : score - 500;

            if (isBreak)
                Scoreboard.BreakCount.Count(judgeState);
            else
                Scoreboard.TapCount.Count(judgeState);

            Scoreboard.Combo++;

            PlayJudgeAnimation();

            tapSpriteRenderer.enabled = false;

            SimulatedSensor.OnTap -= Judge;

            NoteContentRoot.SetActive(false);
        }
    }
}