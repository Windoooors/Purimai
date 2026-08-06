using System;
using System.Collections.Generic;
using System.Linq;
using Game.ChartManagement;
using Game.Notes.TapBasedNotes;
using UI.Result;
using UI.Settings;
using Unity.Mathematics;
using UnityEngine;
using Touch = Game.Notes.TouchBasedNotes.Touch;

namespace Game.Notes
{
    public abstract class TouchBasedNote : NoteBase
    {
        public string sensorId;
        
        private static readonly Dictionary<float, float> TouchFlowSpeed = new()
        {
            { 1f, 175f },
            { 1.25f, 183f },
            { 1.5f, 200f },
            { 1.75f, 212f },
            { 2, 225f },
            { 2.25f, 237f },
            { 2.5f, 250f },
            { 2.75f, 262f },
            { 3f, 275f },
            { 3.25f, 283f },
            { 3.5f, 300f },
            { 3.75f, 312f },
            { 4f, 325f },
            { 4.25f, 337f },
            { 4.5f, 350f },
            { 4.75f, 375f },
            { 5f, 400f },
            { 5.25f, 425f },
            { 5.5f, 450f },
            { 5.75f, 475f },
            { 6f, 500f },
            { 6.25f, 525f },
            { 6.5f, 550f },
            { 6.75f, 575f },
            { 7f, 600f },
            { 7.25f, 625f },
            { 7.5f, 650f },
            { 7.75f, 675f },
            { 8f, 700f },
            { 8.25f, 725f },
            { 8.5f, 750f },
            { 8.75f, 775f },
            { 9f, 800f },
            { 9.25f, 825f },
            { 9.5f, 850f },
            { 9.75f, 875f },
            { 10f, 900f },
            { 49f, 5000f }
        };

        public (bool isEach, bool isOverlapped) TouchBorderInformation;
        public int timing;
        public bool withFireworks;
        public bool headJudged;
        public JudgeState judgeState;

        public int indexInLane;
        public bool isFast;

        public bool isEach;
        protected GameObject NoteContentRoot;
        protected int TouchOnScreenTime;
        
        private Animator _judgeDisplayAnimator;
        protected Animator OffsetDisplayAnimator;
        private Animator _fireworksDisplayAnimator;
        
        public static (JudgeState, bool isFast, bool judged) GetJudgeState(float deltaTiming, bool isEx,
            JudgeSettings judgeSettings)
        {
            if (deltaTiming > judgeSettings.criticalPerfectTiming)
                return (JudgeState.Miss, false, false);

            if (deltaTiming < -judgeSettings.lateGoodTiming)
                return (JudgeState.Miss, false, false);

            var fast = deltaTiming > 0;

            var reversedDeltaTiming = -deltaTiming;

            if (isEx)
                return (JudgeState.CriticalPerfect, fast, true);

            var state = JudgeState.Miss;

            if ((reversedDeltaTiming <= judgeSettings.fastGoodTiming && reversedDeltaTiming > judgeSettings.quarterGreatTiming &&
                 fast)
                || (reversedDeltaTiming <= judgeSettings.lateGoodTiming &&
                    reversedDeltaTiming > judgeSettings.quarterGreatTiming && !fast))
                state = JudgeState.Good;
            if (reversedDeltaTiming <= judgeSettings.quarterGreatTiming && reversedDeltaTiming > judgeSettings.semiGreatTiming)
                state = JudgeState.QuarterGreat;
            if (reversedDeltaTiming <= judgeSettings.semiGreatTiming && reversedDeltaTiming > judgeSettings.greatTiming)
                state = JudgeState.SemiGreat;
            if (reversedDeltaTiming <= judgeSettings.greatTiming && reversedDeltaTiming > judgeSettings.perfectTiming)
                state = JudgeState.Great;
            if (reversedDeltaTiming <= judgeSettings.perfectTiming &&
                reversedDeltaTiming > judgeSettings.semiCriticalPerfectTiming)
                state = JudgeState.Perfect;
            if (reversedDeltaTiming <= judgeSettings.semiCriticalPerfectTiming &&
                reversedDeltaTiming > judgeSettings.criticalPerfectTiming)
                state = JudgeState.SemiCriticalPerfect;
            if (Mathf.Abs(deltaTiming) <= judgeSettings.criticalPerfectTiming)
                state = JudgeState.CriticalPerfect;

            return (state, fast, true);
        }
        
        public static int GetTouchOnScreenTime() {
            return (int)(4 / (TouchFlowSpeed[ChartPlayer.Instance.touchFlowSpeed] / 60) * 1000);
        }

        private void Start()
        {
            TouchOnScreenTime = GetTouchOnScreenTime();

            LateStart();

            NoteContentRoot = new GameObject("NoteContent");
            NoteContentRoot.transform.SetParent(transform);

            var children = transform.GetComponentsInChildren<Transform>();

            foreach (var child in children) child.parent = NoteContentRoot.transform;

            emergingTime = timing - TouchOnScreenTime - TouchOnScreenTime / 4;

            var animatorIndexBase = sensorId.ToCharArray()[0] switch
            {
                'A' => 8,
                'B' => 16,
                'D' => 24,
                'E' => 32,
                'C' => 40,
                _ => 0
            };

            _fireworksDisplayAnimator = ChartPlayer.Instance.fireworksAnimator;

            int.TryParse(sensorId.ToCharArray()[^1].ToString(), out var sensorLane);

            if (sensorLane != 0)
                sensorLane--;
            
            var animatorIndex = animatorIndexBase + sensorLane;
            
            _judgeDisplayAnimator = JudgeDisplayManager.Instance.judgeDisplayAnimators[animatorIndex];
            OffsetDisplayAnimator = JudgeDisplayManager.Instance.offsetDisplayAnimators[animatorIndex];
            
            NoteContentRoot.SetActive(false);
        }

        public virtual void RegisterTapEvent()
        {
        }

        protected void PlayJudgeAnimation()
        {
            if (judgeState is not JudgeState.CriticalPerfect and not JudgeState.Miss)
            {
                var settings = SettingsPool.GetValue("fast_late_display_level");

                switch (settings)
                {
                    case 0:
                        break;
                    case 1:
                        if (judgeState is not JudgeState.SemiCriticalPerfect and not JudgeState.Perfect)
                        {
                            OffsetDisplayAnimator.SetTrigger(isFast ? "ShowFast" : "ShowLate");
                            if (isFast)
                                Scoreboard.FastCount++;
                            else
                                Scoreboard.LateCount++;
                        }

                        break;
                    case 2:
                        OffsetDisplayAnimator.SetTrigger(isFast ? "ShowFast" : "ShowLate");
                        if (isFast)
                            Scoreboard.FastCount++;
                        else
                            Scoreboard.LateCount++;
                        break;
                }
            }

            switch (judgeState)
            {
                case JudgeState.Perfect or JudgeState.CriticalPerfect or JudgeState.SemiCriticalPerfect:
                    _judgeDisplayAnimator.SetTrigger("ShowPerfect"); break;
                case JudgeState.Great or JudgeState.SemiGreat or JudgeState.QuarterGreat:
                    _judgeDisplayAnimator.SetTrigger("ShowGreat"); break;
                case JudgeState.Good:
                    _judgeDisplayAnimator.SetTrigger("ShowGood"); break;
                case JudgeState.Miss:
                    _judgeDisplayAnimator.SetTrigger("ShowMiss"); break;
            }

            if (withFireworks)
            {
                _fireworksDisplayAnimator.transform.position = transform.position;
                _fireworksDisplayAnimator.SetTrigger("ShowFireworks");
            }

            if (judgeState == JudgeState.Miss) return;
            if (sensorId.StartsWith("A"))
                AreaARipple.AreaARipples.Find(x => x.sensorId == sensorId).CancelAnimation();
            ChartPlayer.Instance.judgeCircleGlowAnimator.SetTrigger("ShowGlow");
        }

        protected void PlayJudgeSound(JudgeState state)
        {
            if (state == JudgeState.Miss)
                return;
            
            if (withFireworks)
                SfxManager.Instance.PlayTouchFireworksSound();
            else
                SfxManager.Instance.PlayTouchSound();
        }

        protected virtual void LateStart()
        {
        }

        protected void GetTouchTransform(ref TouchTransform result)
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
        
        public static List<List<Touch>> GetAllConnectedGroups(
            Touch[] input)
        {
            var allGroups = new List<List<Touch>>();
        
            if (input == null || input.Length == 0)
                return allGroups;
            
            var globalVisited = new HashSet<Touch>();

            foreach (var item in input)
            {
                if (item == null || globalVisited.Contains(item))
                    continue;
                
                var currentGroup = new List<Touch>();
                var queue = new Queue<Touch>();

                queue.Enqueue(item);
                globalVisited.Add(item);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    currentGroup.Add(current);

                    var adjacentTouches = GetAdjacentTouches(input, current);

                    if (adjacentTouches == null) continue;

                    foreach (var adj in adjacentTouches)
                    {
                        if (adj != null && !globalVisited.Contains(adj))
                        {
                            globalVisited.Add(adj);
                            queue.Enqueue(adj);
                        }
                    }
                }

                allGroups.Add(currentGroup);
            }

            return allGroups;
        }
        
        private static Touch[] GetAdjacentTouches(Touch[] input, Touch target)
        {
            int.TryParse(target.sensorId.ToCharArray()[^1].ToString(), out var index);
            
            var nextIndex = index + 1;
            var lastIndex = index - 1;
            
            nextIndex = (nextIndex > 8) ? 1 : nextIndex;
            lastIndex = (lastIndex < 1) ? 8 : lastIndex;

            var relevantSensorTypes = target.sensorId.ToCharArray()[0] switch
            {
                'A' => new [] { "B" + index, "D" + nextIndex, "D" + index, "E" + nextIndex, "E" + index },
                'B' => new []
                    { "B" + nextIndex, "B" + lastIndex, "A" + index, "E" + nextIndex, "E" + index, "C" },
                'C' => new [] { "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8" },
                'D' => new [] { "E" + index, "A" + index, "A" + lastIndex },
                'E' => new [] { "D" + index, "A" + index, "A" + lastIndex, "B" + index, "B" + lastIndex },
                _ => throw new ArgumentOutOfRangeException()
            };

            return input.Where(x => relevantSensorTypes.Any(y => y == x.sensorId)).ToArray();
        }

        protected class TouchTransform
        {
            public float Alpha;
            public float Position;
            public bool ShowBorder;
            public bool Shown;
        }
    }
}