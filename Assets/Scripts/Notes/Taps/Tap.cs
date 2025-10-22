using LitMotion;
using Unity.Mathematics;
using UnityEngine;

namespace Notes.Taps
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

            if (!headJudged && ChartPlayer.Instance.time > timing + ChartPlayer.Instance.tapJudgeSettings.lateGoodTiming)
            {
                headJudged = true;
                judgeState = JudgeState.Miss;

                PlayJudgeAnimation();

                SimulatedSensor.OnTap -= Judge;
            }

            if (ChartPlayer.Instance.time > timing - 2 * EmergingDuration &&
                ChartPlayer.Instance.time < timing - 1 * EmergingDuration && !_emerging)
            {
                _emerging = true;

                transform.position = Vector3.zero;

                LMotion.Create(0, 1f, EmergingDuration / 1000f).WithEase(Ease.OutSine)
                    .Bind(x =>
                    {
                        tapSpriteRenderer.color = new Color(1, 1, 1, x);
                        lineSpriteRenderer.color = new Color(1, 1, 1, x);
                    });
                LMotion.Create(0, 1f, EmergingDuration / 1000f).WithEase(Ease.Linear)
                    .Bind(x => tapTransform.localScale = x * Vector3.one);
            }

            if (ChartPlayer.Instance.time > timing - 1 * EmergingDuration && _emerging && !_moving)
            {
                _emerging = false;
                _moving = true;
            }

            if (ChartPlayer.Instance.time > timing + 100)
            {
                _moving = false;
                transform.position = NoteGenerator.Instance.outOfScreenPosition;
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
            transform.position = NoteGenerator.Instance.outOfScreenPosition;
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

            var deltaTiming = timing - ChartPlayer.Instance.time;

            var judgeSettings = ChartPlayer.Instance.tapJudgeSettings;

            var state = GetJudgeState(deltaTiming, judgeSettings);

            headJudged = state.judged;

            if (!headJudged)
                return;

            judgeState = state.Item1;
            
            isFast = state.isFast;

            PlayJudgeAnimation();

            SimulatedSensor.OnTap -= Judge;
        }
    }
}