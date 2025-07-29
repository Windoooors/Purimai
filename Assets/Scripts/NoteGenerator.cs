using System;
using System.Collections.Generic;
using System.Linq;
using ChartManagement;
using Notes;
using Notes.Slides;
using Notes.Taps;
using Unity.Mathematics;
using UnityEngine;

public class NoteGenerator : MonoBehaviour
{
    public static NoteGenerator Instance;

    public TextAsset chartFile;

    public Tap[] tapPrefabs;
    public Hold[] holdPrefabs;
    public EachLine[] eachLinePrefabs;
    public SlidePrefabDataObject slidePrefabs;

    public Sprite slideEachSprite;
    public Sprite[] wifiSlideEachSprites;

    public float originCircleScale = 0.253f;

    public readonly List<TapBasedNote>[] LaneList =
    {
        new(), new(), new(), new(),
        new(), new(), new(), new()
    };

    public readonly List<SlideBasedNote> SlideList = new();

    private GameObject _noteParent;

    private void Awake()
    {
        Application.targetFrameRate = 120;

        Instance = this;
    }


    private void Start()
    {
        _noteParent = new GameObject("Notes");
        GenerateNotes();
    }

    private void GenerateNotes()
    {
        var noteDataObjects = ChartLoader.Instance.Parse(chartFile.text);

        var order = 0;

        foreach (var noteDataObject in noteDataObjects)
        {
            var isEach = noteDataObject.TapDataObjects.Length + noteDataObject.HoldDataObjects.Length > 1;
            var isSlideEach = noteDataObject.SlideDataObjects.Length > 1;

            GenerateTaps(noteDataObject, isEach, order);
            GenerateHolds(noteDataObject, isEach, order);
            GenerateSlides(noteDataObject, isSlideEach, order);

            order--;

            if (isEach) GenerateEachLines(noteDataObject);
        }
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

            tapObjectInstance.tapSpriteRenderer.sortingOrder = order;

            order--;

            LaneList[laneIndex].Add(tapObjectInstance);

            tapObjectInstance.transform.parent = _noteParent.transform;
        }
    }

    private void GenerateEachLines(NoteDataObject noteDataObject)
    {
        var eachNoteList = new List<NoteDataObject.TapDataObjectBase>();
        eachNoteList.AddRange(noteDataObject.TapDataObjects);
        eachNoteList.AddRange(noteDataObject.HoldDataObjects);

        var lanes = eachNoteList.Select(x => x.Lane).ToArray();

        var biggerLane = math.max(lanes[0], lanes[1]);
        var smallerLane = math.min(lanes[0], lanes[1]);

        var interval = biggerLane - smallerLane;

        if (interval > 4)
        {
            (smallerLane, biggerLane) = (biggerLane, smallerLane);
            interval = biggerLane - smallerLane + 8;
        }

        if (lanes.Length > 2)
            interval = 4;

        var eachLine = interval switch
        {
            1 => Instantiate(eachLinePrefabs[0]),
            2 => Instantiate(eachLinePrefabs[1]),
            3 => Instantiate(eachLinePrefabs[2]),
            4 => Instantiate(eachLinePrefabs[3]),
            _ => Instantiate(eachLinePrefabs[0])
        };

        eachLine.timing = noteDataObject.Timing;
        eachLine.lane = smallerLane;

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

            holdObjectInstance.transform.parent = _noteParent.transform;
        }
    }

    private void GenerateSlides(NoteDataObject noteDataObject, bool isSlideEach, int order)
    {
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
                slideBasedNoteObjectInstance.timing = noteDataObject.Timing;
                slideBasedNoteObjectInstance.slideType = slide.Type;

                slideBasedNoteObjectInstance.order = order;
                slideBasedNoteObjectInstance.isEach = isSlideEach;

                order--;

                SlideList.Add(slideBasedNoteObjectInstance);

                slideBasedNoteObjectInstance.transform.parent = _noteParent.transform;
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
}