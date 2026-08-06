using System;
using System.Collections.Generic;
using System.Linq;
using Game.ChartManagement;
using Game.Notes;
using Game.Notes.NormalIndividualSlides;
using Game.Notes.TapBasedNotes;
using UI.Settings;
using UnityEngine;
using UnityEngine.Serialization;
using Touch = Game.Notes.TouchBasedNotes.Touch;

namespace Game
{
    public class NoteGenerator : MonoBehaviour
    {
        private const int GlobalCueSoundOffset = 0;
        private static NoteGenerator _instance;

        public Tap[] tapPrefabs;
        public Hold[] holdPrefabs;
        public EachLine[] eachLinePrefabs;

        public GameObject breakSlideArrowPrefab;
        public GameObject slideArrowPrefab;

        [FormerlySerializedAs("starSprite")] public Sprite eachStarSprite;
        public Sprite breakStarSprite;
        public Sprite starSprite;
        public Sprite slideEachSprite;
        public Sprite slideSprite;
        public Sprite slideBreakSprite;
        public Sprite[] wifiSlideEachSprites;
        public Sprite[] wifiSlideSprites;
        public Sprite[] wifiSlideBreakSprites;
        public Sprite[] touchOverlapBorderSprites;

        public Touch[] touchPrefabs;

        public NormalSlide normalSlidePrefab;
        public WifiSlide wifiSlidePrefab;
        public IndividualSlidePrefabDataObject individualSlidePrefabs;

        public SlideJudgeDisplayDataObject[] slideJudgeDisplaySprites;

        public float originCircleScale = 0.250f;

        public float endingTime;

        public readonly List<TapBasedNote>[] LaneList =
        {
            new(), new(), new(), new(),
            new(), new(), new(), new()
        };

        public readonly List<NoteBase> notesList = new();

        public readonly Dictionary<string, List<TouchBasedNote>> TouchLanes = new()
        {
            { "A1", new List<TouchBasedNote>() }, { "A2", new List<TouchBasedNote>() },
            { "A3", new List<TouchBasedNote>() }, { "A4", new List<TouchBasedNote>() },
            { "A5", new List<TouchBasedNote>() }, { "A6", new List<TouchBasedNote>() },
            { "A7", new List<TouchBasedNote>() }, { "A8", new List<TouchBasedNote>() },
            { "B1", new List<TouchBasedNote>() }, { "B2", new List<TouchBasedNote>() },
            { "B3", new List<TouchBasedNote>() }, { "B4", new List<TouchBasedNote>() },
            { "B5", new List<TouchBasedNote>() }, { "B6", new List<TouchBasedNote>() },
            { "B7", new List<TouchBasedNote>() }, { "B8", new List<TouchBasedNote>() },
            { "C", new List<TouchBasedNote>() }, { "D1", new List<TouchBasedNote>() },
            { "D2", new List<TouchBasedNote>() }, { "D3", new List<TouchBasedNote>() },
            { "D4", new List<TouchBasedNote>() }, { "D5", new List<TouchBasedNote>() },
            { "D6", new List<TouchBasedNote>() }, { "D7", new List<TouchBasedNote>() },
            { "D8", new List<TouchBasedNote>() }, { "E1", new List<TouchBasedNote>() },
            { "E2", new List<TouchBasedNote>() }, { "E3", new List<TouchBasedNote>() },
            { "E4", new List<TouchBasedNote>() }, { "E5", new List<TouchBasedNote>() },
            { "E6", new List<TouchBasedNote>() }, { "E7", new List<TouchBasedNote>() },
            { "E8", new List<TouchBasedNote>() }
        };

        private bool _flipHorizontally;

        private bool _flipVertically;

        private GameObject _noteParent;

        private int _slideOrder;

        public static NoteGenerator Instance => _instance == null
            ? FindObjectsByType<NoteGenerator>(FindObjectsInactive.Include, FindObjectsSortMode.None)[^1]
            : _instance;

        public List<int> CriticalTimeList { get; private set; }

        private void Awake()
        {
            _noteParent = new GameObject("Notes");

            _flipHorizontally = SettingsPool.GetValue("flip_horizontally") == 1;
            _flipVertically = SettingsPool.GetValue("flip_vertically") == 1;

            _instance = this;
        }

        public void GenerateNotes(string chartString, float firstNoteTime, bool largeTouches)
        {
            var noteDataObjects = ChartLoader.Instance.Parse(chartString, firstNoteTime);

            MirrorNotes(noteDataObjects);

            var order = 0;

            _slideOrder = 0;

            var audioOffset = SettingsPool.GetValue("audio_delay") / 1000f;

            var criticalTimeHashSet = new HashSet<int>();

            foreach (var noteDataObject in noteDataObjects)
            {
                if (noteDataObject.HoldDataObjects.Length + noteDataObject.TapDataObjects.Length + 
                   noteDataObject.TouchDataObjects.Length + noteDataObject.TouchHoldDataObjects.Length
                   >= 1)
                    criticalTimeHashSet.Add((int)((noteDataObject.TimingInSeconds - audioOffset) * 1000) +
                                            GlobalCueSoundOffset);

                foreach (var hold in noteDataObject.HoldDataObjects)
                    criticalTimeHashSet.Add(
                        (int)((noteDataObject.TimingInSeconds + hold.HoldDurationInSeconds - audioOffset) * 1000) +
                        GlobalCueSoundOffset);
                
                foreach (var hold in noteDataObject.TouchHoldDataObjects)
                    criticalTimeHashSet.Add(
                        (int)((noteDataObject.TimingInSeconds + hold.HoldDurationInSeconds - audioOffset) * 1000) +
                        GlobalCueSoundOffset);

                var isEach = noteDataObject.TapDataObjects.Length + noteDataObject.HoldDataObjects.Length +
                    noteDataObject.TouchDataObjects.Length + noteDataObject.TouchHoldDataObjects.Length > 1;

                GenerateTouches(noteDataObject, isEach, order, largeTouches);
                GenerateTaps(noteDataObject, isEach, order);
                GenerateHolds(noteDataObject, isEach, order);
                GenerateSlides(noteDataObject);

                order-=10;

                if (isEach) GenerateEachLines(noteDataObject);
            }

            foreach (var lane in LaneList)
                for (var i = lane.Count - 1; i >= 0; i--)
                    lane[i].RegisterTapEvent();

            foreach (var touchLane in TouchLanes.Values)
            {
                for (var i = touchLane.Count - 1; i >= 0; i--)
                    touchLane[i].RegisterTapEvent();

                for (var i = 0; i < touchLane.Count; i++)
                {
                    if (i + 1 >= touchLane.Count)
                        break;

                    if (touchLane[i + 1].timing - TouchBasedNote.GetTouchOnScreenTime() / 4 <
                        touchLane[i].timing + ChartPlayer.Instance.touchJudgeSettings.lateGoodTiming)
                        touchLane[i].TouchBorderInformation = (touchLane[i + 1].isEach, true);
                }
            }

            CriticalTimeList = criticalTimeHashSet.ToList();
            CriticalTimeList.Sort();

            Filter(CriticalTimeList);

            return;

            void Filter(List<int> list)
            {
                if (list == null || list.Count <= 1) return;

                var writeIndex = 1;

                for (var readIndex = 1; readIndex < list.Count; readIndex++)
                    if (list[readIndex] - list[writeIndex - 1] >= 2)
                    {
                        list[writeIndex] = list[readIndex];
                        writeIndex++;
                    }

                if (writeIndex < list.Count) list.RemoveRange(writeIndex, list.Count - writeIndex);
            }
        }

        private void MirrorNotes(NoteDataObject[] noteDataObjects)
        {
            foreach (var note in noteDataObjects)
            {
                foreach (var noteTapDataObject in note.TapDataObjects)
                    noteTapDataObject.Lane = GetModifiedLane(noteTapDataObject.Lane);

                foreach (var noteHoldDataObject in note.HoldDataObjects)
                    noteHoldDataObject.Lane = GetModifiedLane(noteHoldDataObject.Lane);

                foreach (var noteSlideDataObject in note.SlideDataObjects)
                foreach (var individualSlideDataObject in noteSlideDataObject.IndividualSlides)
                {
                    if (_flipHorizontally)
                        individualSlideDataObject.Type = individualSlideDataObject.Type switch
                        {
                            NoteDataObject.SlideType.RotateLeft => NoteDataObject.SlideType.RotateRight,
                            NoteDataObject.SlideType.RotateRight => NoteDataObject.SlideType.RotateLeft,
                            NoteDataObject.SlideType.Z =>
                                NoteDataObject.SlideType.S,
                            NoteDataObject.SlideType.S =>
                                NoteDataObject.SlideType.Z,
                            NoteDataObject.SlideType.P =>
                                NoteDataObject.SlideType.Q,
                            NoteDataObject.SlideType.Q =>
                                NoteDataObject.SlideType.P,
                            NoteDataObject.SlideType.BigP => NoteDataObject.SlideType
                                .BigQ,
                            NoteDataObject.SlideType.BigQ => NoteDataObject.SlideType
                                .BigP,
                            _ => individualSlideDataObject.Type
                        };

                    if (_flipVertically)
                        individualSlideDataObject.Type = individualSlideDataObject.Type switch
                        {
                            NoteDataObject.SlideType.Z =>
                                NoteDataObject.SlideType.S,
                            NoteDataObject.SlideType.S =>
                                NoteDataObject.SlideType.Z,
                            NoteDataObject.SlideType.P =>
                                NoteDataObject.SlideType.Q,
                            NoteDataObject.SlideType.Q =>
                                NoteDataObject.SlideType.P,
                            NoteDataObject.SlideType.BigP => NoteDataObject.SlideType
                                .BigQ,
                            NoteDataObject.SlideType.BigQ => NoteDataObject.SlideType
                                .BigP,
                            _ => individualSlideDataObject.Type
                        };

                    individualSlideDataObject.From = GetModifiedLane(individualSlideDataObject.From);
                    for (var i = 0; i < individualSlideDataObject.To.Length; i++)
                        individualSlideDataObject.To[i] = GetModifiedLane(individualSlideDataObject.To[i]);
                }
            }

            return;

            int GetModifiedLane(int inputLane)
            {
                var result = inputLane;

                if (_flipHorizontally)
                    result = GetHorizontallyFlippedLane(result);
                if (_flipVertically)
                    result = GetVerticallyFlippedLane(result);

                return result;
            }
        }

        private void GenerateTouches(NoteDataObject noteDataObject, bool isEach, int order, bool isLargeTouch)
        {
            var generatedTouchList = new List<Touch>();
            
            foreach (var touch in noteDataObject.TouchDataObjects)
            {
                var touchPrefab = (isEach, isLargeTouch) switch
                {
                    (false, false) => touchPrefabs[0],
                    (true, false) => touchPrefabs[1],
                    (false, true) => touchPrefabs[2],
                    (true, true) => touchPrefabs[3]
                };

                var touchObjectInstance = Instantiate(touchPrefab, _noteParent.transform);
                touchObjectInstance.isEach = isEach;
                notesList.Add(touchObjectInstance);
                
                generatedTouchList.Add(touchObjectInstance);

                touchObjectInstance.timing = noteDataObject.Timing;
                touchObjectInstance.sensorId = touch.Sensor;
                touchObjectInstance.withFireworks = touch.WithFireworks;

                var sensor = TouchPoint.TouchPoints.FirstOrDefault(x => x.sensorName == touch.Sensor);

                if (sensor != null)
                    touchObjectInstance.transform.position = sensor.transform.position;

                touchObjectInstance.SetOrder(order);

                order--;

                TouchLanes[touch.Sensor].Add(touchObjectInstance);

                touchObjectInstance.indexInLane = TouchLanes[touch.Sensor].Count - 1;

                if (noteDataObject.Timing > endingTime)
                    endingTime = noteDataObject.Timing;
            }

            var groups = TouchBasedNote.GetAllConnectedGroups(generatedTouchList.ToArray());
            
            groups.ForEach(x =>
            {
                x.ForEach(y => y.touchGroup = x);
            });
        }

        private void GenerateTaps(NoteDataObject noteDataObject, bool isEach, int order)
        {
            foreach (var tap in noteDataObject.TapDataObjects)
            {
                var laneIndex = tap.Lane - 1;

                var tapObjectInstance = (tap.IsBreak, tap.IsDoubleStarHead, tap.IsStarHead, isEach, tap.IsEx) switch
                {
                    (false, false, false, false, false) => Instantiate(tapPrefabs[0]),
                    (true, false, false, _, false) => Instantiate(tapPrefabs[1]),
                    (false, false, true, false, false) => Instantiate(tapPrefabs[2]),
                    (true, false, true, _, false) => Instantiate(tapPrefabs[3]),
                    (false, true, true, false, false) => Instantiate(tapPrefabs[4]),
                    (true, true, true, _, false) => Instantiate(tapPrefabs[5]),
                    (false, false, false, true, false) => Instantiate(tapPrefabs[6]),
                    (false, false, true, true, false) => Instantiate(tapPrefabs[7]),
                    (false, true, true, true, false) => Instantiate(tapPrefabs[8]),
                    (false, false, false, false, true) => Instantiate(tapPrefabs[9]),
                    (false, false, false, true, true) => Instantiate(tapPrefabs[10]),
                    (false, false, true, false, true) => Instantiate(tapPrefabs[11]),
                    (false, false, true, true, true) => Instantiate(tapPrefabs[12]),
                    (false, true, true, false, true) => Instantiate(tapPrefabs[13]),
                    (false, true, true, true, true) => Instantiate(tapPrefabs[14]),
                    (true, false, false, _, true) => Instantiate(tapPrefabs[15]),
                    (true, false, true, _, true) => Instantiate(tapPrefabs[16]),
                    (true, true, true, _, true) => Instantiate(tapPrefabs[17]),
                    (_, _, _, _, _) => Instantiate(tapPrefabs[0])
                };

                tapObjectInstance.isEach = isEach;

                notesList.Add(tapObjectInstance);

                tapObjectInstance.timing = noteDataObject.Timing;
                tapObjectInstance.lane = tap.Lane;
                tapObjectInstance.isNoSpinningStarHead = tap.IsNoSpinningStarHead;
                tapObjectInstance.isStarHead = tap.IsStarHead;
                tapObjectInstance.isBreak = tap.IsBreak;
                tapObjectInstance.rotateSpeed = tap.RotateSpeed;

                tapObjectInstance.tapSpriteRenderer.sortingOrder = order;
                if (tapObjectInstance.exSpriteRenderer)
                    tapObjectInstance.exSpriteRenderer.sortingOrder = order + 1;

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

            if (eachNoteList.Count == 0)
                return;

            var lanes = eachNoteList.Select(x => x.Lane).ToList();

            lanes.Sort();
            var biggestLane = lanes[^1];
            var smallestLane = lanes[0];

            var interval = biggestLane - smallestLane;

            if (interval == 0)
                return;

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

            notesList.Add(eachLine);

            eachLine.timing = noteDataObject.Timing;
            eachLine.lane = smallestLane;

            eachLine.transform.parent = _noteParent.transform;
        }

        private void GenerateHolds(NoteDataObject noteDataObject, bool isEach, int order)
        {
            foreach (var hold in noteDataObject.HoldDataObjects)
            {
                var laneIndex = hold.Lane - 1;

                var holdPrefab = (isEach, hold.IsBreak, hold.IsEx)
                    switch
                    {
                        (false, false, false) => holdPrefabs[0],
                        (true, false, false) => holdPrefabs[1],
                        (_, true, false) => holdPrefabs[2],
                        (false, false, true) => holdPrefabs[3],
                        (true, false, true) => holdPrefabs[4],
                        (_, true, true) => holdPrefabs[5]
                    };

                var holdObjectInstance = Instantiate(holdPrefab, _noteParent.transform);

                holdObjectInstance.isEach = isEach;

                holdObjectInstance.timing = noteDataObject.Timing;
                holdObjectInstance.lane = hold.Lane;
                holdObjectInstance.duration = hold.HoldDuration;

                holdObjectInstance.holdSpriteRenderer.sortingOrder = order;
                if (holdObjectInstance.exSpriteRenderer)
                    holdObjectInstance.exSpriteRenderer.sortingOrder = order + 1;

                order--;

                LaneList[laneIndex].Add(holdObjectInstance);

                notesList.Add(holdObjectInstance);

                holdObjectInstance.indexInLane = LaneList[laneIndex].Count - 1;
                
                if (noteDataObject.Timing + holdObjectInstance.duration > endingTime)
                    endingTime = noteDataObject.Timing + holdObjectInstance.duration;
            }
        }

        private void GenerateSlides(NoteDataObject noteDataObject)
        {
            var slidesGroupedByWaitDuration = new List<(int waitDuration, List<NoteDataObject.SlideDataObject>)>();
            foreach (var slide in noteDataObject.SlideDataObjects)
            {
                var findResult =
                    slidesGroupedByWaitDuration
                        .Find(x => x.waitDuration == slide.WaitDuration);

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
                var isWifi = slide.IndividualSlides.Length == 1 &&
                             slide.IndividualSlides[0].Type == NoteDataObject.SlideType.Wifi;

                SlideBasedNote slideBasedNoteObjectInstance =
                    isWifi ? SlideGenerator.GenerateWifiSlide(slide) : Instantiate(normalSlidePrefab);

                slideBasedNoteObjectInstance.transform.position = Vector3.zero;

                if (!slideBasedNoteObjectInstance)
                    continue;

                notesList.Add(slideBasedNoteObjectInstance);
                slideBasedNoteObjectInstance.transform.parent = _noteParent.transform;

                var isEach = (slidesGroupedByWaitDuration
                    .Find(x => x.waitDuration == slide.WaitDuration).Item2?.Count ?? 1) > 1;

                if (slideBasedNoteObjectInstance is NormalSlide normalSlide)
                    GenerateIndividualSlides(normalSlide, slide.IndividualSlides);

                slideBasedNoteObjectInstance.Initialize(slide, isEach, slide.IsBreak, noteDataObject.Timing,
                    ref _slideOrder);

                if (noteDataObject.Timing + slide.WaitDuration + slide.SlideDuration > endingTime)
                    endingTime = noteDataObject.Timing + slide.WaitDuration + slide.SlideDuration;
            }
        }

        private int GetVerticallyFlippedLane(int inputLane)
        {
            return inputLane switch
            {
                1 => 4,
                2 => 3,
                3 => 2,
                4 => 1,
                5 => 8,
                6 => 7,
                7 => 6,
                8 => 5,
                _ => inputLane
            };
        }

        private int GetHorizontallyFlippedLane(int inputLane)
        {
            return inputLane switch
            {
                1 => 8,
                2 => 7,
                3 => 6,
                4 => 5,
                5 => 4,
                6 => 3,
                7 => 2,
                8 => 1,
                _ => inputLane
            };
        }

        public static void GenerateIndividualSlides(NormalSlide normalSlide,
            NoteDataObject.IndividualSlideDataObject[] individualSlideData)
        {
            foreach (var individualSlide in individualSlideData)
            {
                var individualSlideInstance = individualSlide.Type switch
                {
                    NoteDataObject.SlideType.RotateLeft
                        or NoteDataObject.SlideType.RotateRight
                        or NoteDataObject.SlideType.RotateMinorArc
                        => SlideGenerator.GenerateCycleSlide(
                            individualSlide),

                    NoteDataObject.SlideType.P or NoteDataObject.SlideType.Q =>
                        SlideGenerator.GeneratePqSlide(individualSlide),

                    NoteDataObject.SlideType.LittleV
                        => SlideGenerator.GenerateLittleVSlide(individualSlide),

                    NoteDataObject.SlideType.Line
                        => SlideGenerator.GenerateLineSlide(individualSlide),

                    NoteDataObject.SlideType.BigV => SlideGenerator.GenerateBigVSlide(individualSlide),

                    NoteDataObject.SlideType.BigP or NoteDataObject.SlideType.BigQ =>
                        SlideGenerator.GenerateBigPqSlide(individualSlide),
                    NoteDataObject.SlideType.Z or NoteDataObject.SlideType.S =>
                        SlideGenerator.GenerateZsSlide(individualSlide),
                    _ => null
                };

                if (!individualSlideInstance)
                    continue;

                individualSlideInstance.transform.position = Vector3.zero;

                normalSlide.individualSlides.Add(individualSlideInstance);
                individualSlideInstance?.transform.SetParent(normalSlide.transform);
                individualSlideInstance.parentNormalSlide = normalSlide;
            }
        }

        [Serializable]
        public class IndividualSlidePrefabDataObject
        {
            public CycleSlide[] cycleSlidePrefabs;
            public PqSlide[] pqSlidePrefabs;
            public LittleVSlide[] vSlidePrefabs;
            public LineSlide[] lineSlidePrefabs;
            public BigVSlide[] bigVSlidePrefabs;
            public BigPqSlide[] bigPqSlidePrefabs;
            public ZsSlide zsSlidePrefab;
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