using System.Collections.Generic;
using System.Linq;
using UI.Result;
using UnityEngine;

namespace Game.Notes.TouchBasedNotes
{
    public class Touch : TouchBasedNote
    {
        private const float Step = 0.463f;

        public Transform[] touchTransforms;
        public SpriteRenderer[] touchSpriteRenderers;
        public float scale = 1;

        public SpriteRenderer dotSpriteRenderer;

        public SpriteRenderer justBorder;
        public SpriteRenderer overlapIndicatingBorder;
        public SpriteRenderer overlapLargeIndicatingBorder;

        public List<Touch> touchGroup;

        private JudgeManager.JudgeAction _judgeAction;

        private bool _judgedByTouchGroup;
        private TouchTransform _touchTransform;

        public (bool isEach, bool isOverlapped) LargeTouchBorderInformation { get; set; }
        public (bool isEach, bool isOverlapped) TouchBorderInformation { get; set; }

        public override void AddAutoPlayKeyFrame()
        {
            var list = AutoPlayer.KeyFrameManager.GetKeyFrames(sensorId);

            list.Add(new AutoPlayKeyFrame(AutoPlayKeyFrame.Type.PressDown, timing));
        }

        private float GetTouchGroupJudgedProportion()
        {
            return touchGroup.Count(x => x.headJudged) / (float)touchGroup.Count;
        }

        protected override void LateStart()
        {
            _touchTransform = new TouchTransform();

            foreach (var touchTransform in touchTransforms) touchTransform.Translate(Vector3.down * Step * scale);

            foreach (var touchSpriteRenderer in touchSpriteRenderers) touchSpriteRenderer.color = new Color(1, 1, 1, 0);

            overlapIndicatingBorder.color = new Color(1, 1, 1, 0);
            overlapLargeIndicatingBorder.color = new Color(1, 1, 1, 0);
            justBorder.color = new Color(1, 1, 1, 0);

            Scoreboard.TapCount.TotalCount++;

            if (TouchBorderInformation.isOverlapped)
            {
                overlapIndicatingBorder.color = new Color(1, 1, 1, 1);

                overlapIndicatingBorder.sprite = TouchBorderInformation.isEach
                    ? NoteGenerator.Instance.touchOverlapBorderSprites[1]
                    : NoteGenerator.Instance.touchOverlapBorderSprites[0];
            }

            if (LargeTouchBorderInformation.isOverlapped)
            {
                overlapLargeIndicatingBorder.color = new Color(1, 1, 1, 1);

                overlapLargeIndicatingBorder.sprite = LargeTouchBorderInformation.isEach
                    ? NoteGenerator.Instance.touchOverlapBorderSprites[3]
                    : NoteGenerator.Instance.touchOverlapBorderSprites[2];
            }
        }

        public void SetOrder(int order)
        {
            foreach (var touchSpriteRenderer in touchSpriteRenderers) touchSpriteRenderer.sortingOrder += order;
            justBorder.sortingOrder += order;
            overlapIndicatingBorder.sortingOrder += order;
            overlapLargeIndicatingBorder.sortingOrder += order;

            dotSpriteRenderer.sortingOrder += order;
        }

        public override void ManualUpdate()
        {
            if (!ChartPlayer.Instance.isPlaying)
                return;

            GetTouchTransform(ref _touchTransform);

            if (headJudged)
            {
                enabled = false;
                return;
            }

            if (!_touchTransform.Shown)
            {
                NoteContentRoot.SetActive(false);
                return;
            }

            if (_touchTransform.Shown && !headJudged && !NoteContentRoot.activeSelf)
                NoteContentRoot.SetActive(true);

            if (!headJudged && ChartPlayer.Instance.TimeInMilliseconds >
                timing + ChartPlayer.Instance.touchJudgeSettings.lateGoodTiming + ChartPlayer.Instance.judgeDelay)
            {
                headJudged = true;
                judgeState = JudgeState.Miss;

                Scoreboard.TapCount.Count(judgeState);

                Scoreboard.ResetCombo();

                PlayJudgeAnimation();

                _judgeAction.Enabled = false;

                NoteContentRoot.SetActive(false);
            }

            foreach (var touchTransform in touchTransforms)
            {
                var angleRad = touchTransform.eulerAngles.z * Mathf.Deg2Rad;
                var direction = new Vector3(-Mathf.Sin(angleRad), Mathf.Cos(angleRad), 0);

                touchTransform.position = Vector3.Lerp(transform.position - direction * Step, transform.position,
                    Mathf.Pow(_touchTransform.Position, 4));
            }

            if (_touchTransform.ShowBorder) justBorder.color = new Color(1, 1, 1, 1);

            var color = new Color(1, 1, 1, _touchTransform.Alpha);
            foreach (var touchSpriteRenderer in touchSpriteRenderers) touchSpriteRenderer.color = color;
        }

        private void GetTouchTransform(ref TouchTransform result)
        {
            if (result == null)
                return;

            var currentPosition = ChartPlayer.Instance.TimeInMilliseconds;

            var startEmergingTiming = timing - TouchOnScreenTime - TouchOnScreenTime / 4f;

            var startMovingTiming = timing - TouchOnScreenTime;

            if (currentPosition < startEmergingTiming - 100 ||
                currentPosition > timing + ChartPlayer.Instance.touchJudgeSettings.lateGoodTiming + 200 ||
                (indexInLane - 1 >= 0 && !NoteGenerator.Instance.TouchLanes[sensorId][indexInLane - 1].headJudged))
            {
                result.Shown = false;
                return;
            }

            if (currentPosition > startEmergingTiming && currentPosition < startMovingTiming)
            {
                var factor = (currentPosition - emergingTime) / (TouchOnScreenTime / 4f);

                result.Alpha = factor;
                result.Position = 0;
                result.Shown = true;

                return;
            }

            if (currentPosition >= startMovingTiming)
            {
                var factor = (currentPosition - startMovingTiming) / TouchOnScreenTime;

                result.Alpha = 1;
                result.Position = factor;
                result.Shown = true;

                if (currentPosition > timing)
                    result.ShowBorder = true;

                return;
            }

            result.Alpha = 0;
            result.Position = 0;
            result.Shown = false;
        }

        public override void RegisterTapEvent()
        {
            var judgeSettings = ChartPlayer.Instance.touchJudgeSettings;

            JudgeManager.Instance.RegisterTap(timing - 100 - judgeSettings.fastGoodTiming,
                timing + 100 + judgeSettings.lateGoodTiming, Judge, out _judgeAction
            );
        }

        private void Judge(object sender, TouchEventArgs e)
        {
            if (headJudged)
                return;

            if (e.SensorId != sensorId)
                return;

            var noteGenerator = NoteGenerator.Instance;

            if (indexInLane != 0 && !noteGenerator.TouchLanes[sensorId][indexInLane - 1].headJudged)
                return;

            var deltaTiming = timing - ChartPlayer.Instance.TimeInMilliseconds + ChartPlayer.Instance.judgeDelay;

            var judgeSettings = ChartPlayer.Instance.touchJudgeSettings;

            var state = GetJudgeState(deltaTiming, false, judgeSettings);

            headJudged = state.judged;

            if (!headJudged)
                return;

            judgeState = state.Item1;

            isFast = state.isFast;

            Scoreboard.TapCount.Count(judgeState);

            Scoreboard.Combo++;

            PlayJudgeAnimation();

            PlayJudgeSound(judgeState);

            foreach (var touchSpriteRenderer in touchSpriteRenderers) touchSpriteRenderer.enabled = false;

            _judgeAction.Enabled = false;

            NoteContentRoot.SetActive(false);

            if (GetTouchGroupJudgedProportion() > 0.5f && !_judgedByTouchGroup)
            {
                _judgedByTouchGroup = true;

                touchGroup.ForEach(x =>
                {
                    if (x == this)
                        return;

                    x._judgedByTouchGroup = true;
                    x.Judge(sender, new TouchEventArgs(x.sensorId));
                });
            }
        }

        private class TouchTransform
        {
            public float Alpha;
            public float Position;
            public bool ShowBorder;
            public bool Shown;
        }
    }
}