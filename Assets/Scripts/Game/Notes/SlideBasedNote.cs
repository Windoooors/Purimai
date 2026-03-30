using System.Collections.Generic;
using NUnit.Framework;
using UI.Result;
using UnityEngine;

namespace Game.Notes
{
    public class SlideBasedNote : NoteBase
    {
        public List<IndividualSlideBase> individualSlides;
        
        [HideInInspector] public int timing;
        [HideInInspector] public int slideDuration;
        [HideInInspector] public int waitDuration;

        [HideInInspector] public bool isEach;
        [HideInInspector] public int order;
        
        [HideInInspector] public bool suddenlyAppears;
        
        protected readonly List<Segment> UniversalSegments = new();

        private bool _concealed;

        private bool _haveShown;

        private JudgeManager.JudgeAction _holdJudgeAction;

        private bool _isFast;
        
        private JudgeState _judgeState;
        private JudgeManager.JudgeAction _leaveJudgeAction;

        private SpriteRenderer[] _slideArrowSpriteRenderers;

        private bool _slidedHalf;

        private SlideTransform _slideTransform = new();
        private bool _starMovingStarted;

        private bool _waitingStarted;

        protected bool IsClockwise;

        protected GameObject SlideContentRoot;

        protected bool Slided;
        protected int[] SlideJudgeDisplaySpriteIndexes;

        protected int SlideJudgeTiming;
        
        private class SlideTransform
        {
            public float ArrowAlpha;

            public bool Shown;
            public float StarAlpha;
            public float StarPosition;
        }
        
        private void Start()
        {
            var tapJudgeSettings = ChartPlayer.Instance.tapJudgeSettings;
            var slideJudgeSettings = ChartPlayer.Instance.slideJudgeSettings;

            JudgeManager.Instance.RegisterHold(timing - tapJudgeSettings.fastGoodTiming - 100,
                timing + slideDuration + waitDuration + 100 + slideJudgeSettings.lateGoodTiming, OnHoldSlidePath,
                out _holdJudgeAction);
            JudgeManager.Instance.RegisterLeave(timing - tapJudgeSettings.fastGoodTiming - 100,
                timing + slideDuration + waitDuration + 100 + slideJudgeSettings.lateGoodTiming, OnLeaveSlidePath,
                out _leaveJudgeAction);

            Scoreboard.SlideCount.TotalCount++;
            
            
            individualSlides.ForEach(x =>
            {
                x.OnStart();
                x.slide = this;
            });
        }
        
        public override void ManualUpdate()
        {
            
        }

        public override void AddAutoPlayKeyFrame()
        {
            
        }
    }
}