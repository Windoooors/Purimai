using System;
using System.Collections.Generic;
using System.Linq;
using Game.ChartManagement;
using Game.Notes;
using Game.Notes.Slides;
using Game.Notes.Taps;
using UI.GameSettings;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{
    public class NoteGenerator : MonoBehaviour
    {
        public static NoteGenerator Instance;

        public Tap[] tapPrefabs;
        public Hold[] holdPrefabs;
        public EachLine[] eachLinePrefabs;
        public SlidePrefabDataObject slidePrefabs;

        public GameObject slideArrowPrefab;

        [FormerlySerializedAs("starSprite")] public Sprite eachStarSprite;
        public Sprite slideEachSprite;
        public Sprite slideSprite;
        public Sprite[] wifiSlideEachSprites;
        public Sprite[] wifiSlideSprites;

        public SlideJudgeDisplayDataObject[] slideJudgeDisplaySprites;

        public float originCircleScale = 0.250f;

        public float endingTime;

        public readonly List<TapBasedNote>[] LaneList =
        {
            new(), new(), new(), new(),
            new(), new(), new(), new()
        };

        private GameObject _noteParent;

        private int _slideOrder;

        public List<int> criticalTimeList { get; private set; }

        private void Awake()
        {
            _noteParent = new GameObject("Notes");

            Instance = this;
        }

        public void GenerateNotes(string chartString, float firstNoteTime)
        {
            var noteDataObjects = ChartLoader.Instance.Parse(chartString, firstNoteTime);

            var order = 0;

            _slideOrder = 0;

            var audioOffset = SettingsPool.GetValue("game.delay") / 1000f;

            var criticalTimeHashSet = new HashSet<int>();

            foreach (var noteDataObject in noteDataObjects)
            {
                criticalTimeHashSet.Add((int)((noteDataObject.TimingInSeconds - audioOffset) * 1000));

                foreach (var hold in noteDataObject.HoldDataObjects)
                    criticalTimeHashSet.Add(
                        (int)((noteDataObject.TimingInSeconds + hold.HoldDurationInSeconds - audioOffset) * 1000));

                var isEach = noteDataObject.TapDataObjects.Length + noteDataObject.HoldDataObjects.Length > 1;
                //var isSlideEach = noteDataObject.SlideDataObjects.Length > 1;

                GenerateTaps(noteDataObject, isEach, order);
                GenerateHolds(noteDataObject, isEach, order);
                GenerateSlides(noteDataObject);

                order--;

                if (isEach) GenerateEachLines(noteDataObject);
            }

            foreach (var lane in LaneList)
                for (var i = lane.Count - 1; i >= 0; i--)
                    lane[i].RegisterTapEvent();

            criticalTimeList = criticalTimeHashSet.ToList();
            criticalTimeList.Sort();
        }

        private void GenerateTaps(NoteDataObject noteDataObject, bool isEach, int order)
        {
            foreach (var tap in noteDataObject.TapDataObjects)
            {
                var laneIndex = tap.Lane - 1;

                var tapObjectInstance = (tap.IsBreak, tap.IsDoubleStarHead, tap.IsStarHead, isEach) switch
                {
                    (false, false, false, false) => Instantiate(tapPrefabs[0]),
                    (true, false, false, _) => Instantiate(tapPrefabs[1]),
                    (false, false, true, false) => Instantiate(tapPrefabs[2]),
                    (true, false, true, _) => Instantiate(tapPrefabs[3]),
                    (false, true, true, false) => Instantiate(tapPrefabs[4]),
                    (true, true, true, _) => Instantiate(tapPrefabs[5]),
                    (false, false, false, true) => Instantiate(tapPrefabs[6]),
                    (false, false, true, true) => Instantiate(tapPrefabs[7]),
                    (false, true, true, true) => Instantiate(tapPrefabs[8]),
                    (_, _, _, _) => Instantiate(tapPrefabs[0])
                };

                tapObjectInstance.timing = noteDataObject.Timing;
                tapObjectInstance.lane = tap.Lane;
                tapObjectInstance.isNoSpinningStarHead = tap.IsNoSpinningStarHead;
                tapObjectInstance.isStarHead = tap.IsStarHead;
                tapObjectInstance.isBreak = tap.IsBreak;
                tapObjectInstance.rotateSpeed = tap.RotateSpeed;

                tapObjectInstance.tapSpriteRenderer.sortingOrder = order;

                order--;

                LaneList[laneIndex].Add(tapObjectInstance);

                tapObjectInstance.indexInLane = LaneList[laneIndex].Count - 1;

                tapObjectInstance.transform.parent = _noteParent.transform;

                if (noteDataObject.Timing > endingTime)
                    endingTime = noteDataObject.Timing;
            }
        }

        private void GenerateEachLines(NoteDataObject noteDataObject)
        {
            var eachNoteList = new List<NoteDataObject.TapDataObjectBase>();
            eachNoteList.AddRange(noteDataObject.TapDataObjects);
            eachNoteList.AddRange(noteDataObject.HoldDataObjects);

            var lanes = eachNoteList.Select(x => x.Lane).ToList();

            lanes.Sort();
            var biggestLane = lanes[^1];
            var smallestLane = lanes[0];

            var interval = biggestLane - smallestLane;

            if (interval > 4)
            {
                (smallestLane, biggestLane) = (biggestLane, smallestLane);
                interval = biggestLane - smallestLane + 8;
            }

            var eachLine = interval switch
            {
                1 => Instantiate(eachLinePrefabs[0]),
                2 => Instantiate(eachLinePrefabs[1]),
                3 => Instantiate(eachLinePrefabs[2]),
                4 => Instantiate(eachLinePrefabs[3]),
                _ => Instantiate(eachLinePrefabs[0])
            };

            eachLine.timing = noteDataObject.Timing;
            eachLine.lane = smallestLane;

            eachLine.transform.parent = _noteParent.transform;
        }

        private void GenerateHolds(NoteDataObject noteDataObject, bool isEach, int order)
        {
            foreach (var hold in noteDataObject.HoldDataObjects)
            {
                var laneIndex = hold.Lane - 1;

                var holdObjectInstance = isEach ? Instantiate(holdPrefabs[1]) : Instantiate(holdPrefabs[0]);

                holdObjectInstance.timing = noteDataObject.Timing;
                holdObjectInstance.lane = hold.Lane;
                holdObjectInstance.duration = hold.HoldDuration;

                holdObjectInstance.holdSpriteRenderer.sortingOrder = order;

                order--;

                LaneList[laneIndex].Add(holdObjectInstance);

                holdObjectInstance.indexInLane = LaneList[laneIndex].Count - 1;

                holdObjectInstance.transform.parent = _noteParent.transform;

                if (noteDataObject.Timing + holdObjectInstance.duration > endingTime)
                    endingTime = noteDataObject.Timing + holdObjectInstance.duration;
            }
        }

        private void GenerateSlides(NoteDataObject noteDataObject)
        {
            var slidesGroupedByWaitDuration = new List<(int waitDuration, List<NoteDataObject.SlideDataObject>)>();

            foreach (var slide in noteDataObject.SlideDataObjects)
            {
                var findResult = slidesGroupedByWaitDuration.Find(x => x.waitDuration == slide.WaitDuration);

                if (findResult.Item2?.Count is 0 or null)
                {
                    slidesGroupedByWaitDuration.Add((slide.WaitDuration, new List<NoteDataObject.SlideDataObject>
                    {
                        slide
                    }));

                    continue;
                }

                findResult.Item2.Add(slide);
            }

            foreach (var slide in noteDataObject.SlideDataObjects)
            {
                var slideBasedNoteObjectInstance = slide.Type switch
                {
                    NoteDataObject.SlideDataObject.SlideType.RotateLeft
                        or NoteDataObject.SlideDataObject.SlideType.RotateRight
                        or NoteDataObject.SlideDataObject.SlideType.RotateMinorArc
                        => SlideGenerator.GenerateCycleSlide(
                            slide),

                    NoteDataObject.SlideDataObject.SlideType.P or NoteDataObject.SlideDataObject.SlideType.Q =>
                        SlideGenerator.GeneratePqSlide(slide),

                    NoteDataObject.SlideDataObject.SlideType.LittleV
                        => SlideGenerator.GenerateLittleVSlide(slide),

                    NoteDataObject.SlideDataObject.SlideType.Line
                        => SlideGenerator.GenerateLineSlide(slide),

                    NoteDataObject.SlideDataObject.SlideType.BigV => SlideGenerator.GenerateBigVSlide(slide),

                    NoteDataObject.SlideDataObject.SlideType.BigP or NoteDataObject.SlideDataObject.SlideType.BigQ =>
                        SlideGenerator.GenerateBigPqSlide(slide),

                    NoteDataObject.SlideDataObject.SlideType.Z or NoteDataObject.SlideDataObject.SlideType.S =>
                        SlideGenerator.GenerateZsSlide(slide),

                    NoteDataObject.SlideDataObject.SlideType.Wifi => SlideGenerator.GenerateWifiSlide(slide),

                    _ => null
                };

                if (slideBasedNoteObjectInstance)
                {
                    slideBasedNoteObjectInstance.order = -_slideOrder;
                    slideBasedNoteObjectInstance.timing = noteDataObject.Timing;
                    slideBasedNoteObjectInstance.slideType = slide.Type;
                    slideBasedNoteObjectInstance.isEach = (slidesGroupedByWaitDuration
                        .Find(x => x.waitDuration == slide.WaitDuration).Item2?.Count ?? 1) > 1;
                    slideBasedNoteObjectInstance.suddenlyAppears = slide.SuddenlyAppears;

                    _slideOrder -= slideBasedNoteObjectInstance.slideArrowCount;

                    slideBasedNoteObjectInstance.transform.parent = _noteParent.transform;

                    if (noteDataObject.Timing + slide.WaitDuration + slide.SlideDuration > endingTime)
                        endingTime = noteDataObject.Timing + slide.WaitDuration + slide.SlideDuration;
                }
            }
        }

        [Serializable]
        public class SlidePrefabDataObject
        {
            public CycleSlide[] cycleSlidePrefabs;
            public PqSlide[] pqSlidePrefabs;
            public LittleVSlide[] vSlidePrefabs;
            public LineSlide[] lineSlidePrefabs;
            public BigVSlide[] bigVSlidePrefabs;
            public BigPqSlide[] bigPqSlidePrefabs;
            public ZsSlide zsSlidePrefab;
            public WifiSlide wifiSlidePrefab;
        }

        [Serializable]
        public class SlideJudgeDisplayDataObject
        {
            public Sprite[] normalSlideJudgeSprites;
            public Sprite[] circleSlideJudgeSprites;
            public Sprite[] wifiSlideJudgeSprites;
        }
    }
}