using System.Linq;
using ChartManagement;
using Notes;
using Notes.Slides;
using UnityEngine;

public class SlideGenerator : MonoBehaviour
{
    public static SlideBasedNote GenerateCycleSlide(NoteDataObject.SlideDataObject slide)
    {
        var cycleSlidePrefabs = NoteGenerator.Instance.slidePrefabs.cycleSlidePrefabs;

        var fromLane = slide.From;
        var toLane = slide.To[0];

        var cycleInterval = CycleSlide.GetCycleInterval(fromLane, toLane,
            slide.Type
        );

        var cycleSlideInstance = cycleInterval switch
        {
            1 => Instantiate(cycleSlidePrefabs[0]),
            2 => Instantiate(cycleSlidePrefabs[1]),
            3 => Instantiate(cycleSlidePrefabs[2]),
            4 => Instantiate(cycleSlidePrefabs[3]),
            5 => Instantiate(cycleSlidePrefabs[4]),
            6 => Instantiate(cycleSlidePrefabs[5]),
            7 => Instantiate(cycleSlidePrefabs[6]),
            8 => Instantiate(cycleSlidePrefabs[7]),
            _ => Instantiate(cycleSlidePrefabs[1])
        };

        cycleSlideInstance.toLaneIndex = toLane - 1;
        cycleSlideInstance.fromLaneIndex = fromLane - 1;
        cycleSlideInstance.waitDuration = slide.WaitDuration;
        cycleSlideInstance.slideDuration = slide.SlideDuration;

        return cycleSlideInstance;
    }

    public static SlideBasedNote GeneratePqSlide(NoteDataObject.SlideDataObject slide)
    {
        var fromLane = slide.From;
        var toLane = slide.To[0];

        var pqInterval = SlideBasedNote.GetIntervalInBothWays(slide.From, slide.To[0]);

        var pqSlidePrefabs = NoteGenerator.Instance.slidePrefabs.pqSlidePrefabs;

        var interval = slide.Type == NoteDataObject.SlideDataObject.SlideType.P
            ? pqInterval.clockwiseInterval
            : pqInterval.counterClockwiseInterval;

        var pqSlideInstance = interval switch
        {
            0 => Instantiate(pqSlidePrefabs[0]),
            1 => Instantiate(pqSlidePrefabs[1]),
            2 => Instantiate(pqSlidePrefabs[2]),
            3 => Instantiate(pqSlidePrefabs[3]),
            4 => Instantiate(pqSlidePrefabs[4]),
            5 => Instantiate(pqSlidePrefabs[5]),
            6 => Instantiate(pqSlidePrefabs[6]),
            7 => Instantiate(pqSlidePrefabs[7]),
            _ => Instantiate(pqSlidePrefabs[0])
        };

        pqSlideInstance.toLaneIndex = toLane - 1;
        pqSlideInstance.fromLaneIndex = fromLane - 1;
        pqSlideInstance.waitDuration = slide.WaitDuration;
        pqSlideInstance.slideDuration = slide.SlideDuration;

        return pqSlideInstance;
    }

    public static SlideBasedNote GenerateLittleVSlide(NoteDataObject.SlideDataObject slide)
    {
        var fromLane = slide.From;
        var toLane = slide.To[0];

        var interval = SlideBasedNote.GetIntervalInBothWays(slide.From, slide.To[0]).clockwiseInterval;

        var vSlidePrefabs = NoteGenerator.Instance.slidePrefabs.vSlidePrefabs;

        var vSlideInstance = interval switch
        {
            0 => Instantiate(vSlidePrefabs[0]),
            1 => Instantiate(vSlidePrefabs[1]),
            2 => Instantiate(vSlidePrefabs[2]),
            3 => Instantiate(vSlidePrefabs[3]),
            4 => Instantiate(vSlidePrefabs[4]),
            5 => Instantiate(vSlidePrefabs[5]),
            6 => Instantiate(vSlidePrefabs[6]),
            7 => Instantiate(vSlidePrefabs[7]),
            _ => Instantiate(vSlidePrefabs[0])
        };

        vSlideInstance.toLaneIndex = toLane - 1;
        vSlideInstance.fromLaneIndex = fromLane - 1;
        vSlideInstance.waitDuration = slide.WaitDuration;
        vSlideInstance.slideDuration = slide.SlideDuration;

        return vSlideInstance;
    }

    public static SlideBasedNote GenerateLineSlide(NoteDataObject.SlideDataObject slide)
    {
        var fromLane = slide.From;
        var toLane = slide.To[0];

        var interval = SlideBasedNote.GetIntervalInBothWays(slide.From, slide.To[0]).clockwiseInterval;

        var lineSlidePrefabs = NoteGenerator.Instance.slidePrefabs.lineSlidePrefabs;

        var lineSlideInstance = interval switch
        {
            2 => Instantiate(lineSlidePrefabs[0]),
            3 => Instantiate(lineSlidePrefabs[1]),
            4 => Instantiate(lineSlidePrefabs[2]),
            5 => Instantiate(lineSlidePrefabs[3]),
            6 => Instantiate(lineSlidePrefabs[4]),
            _ => Instantiate(lineSlidePrefabs[0])
        };

        lineSlideInstance.toLaneIndex = toLane - 1;
        lineSlideInstance.fromLaneIndex = fromLane - 1;
        lineSlideInstance.waitDuration = slide.WaitDuration;
        lineSlideInstance.slideDuration = slide.SlideDuration;

        return lineSlideInstance;
    }

    public static SlideBasedNote GenerateBigVSlide(NoteDataObject.SlideDataObject slide)
    {
        var fromLane = slide.From;

        var interval = SlideBasedNote.GetShortestInterval(slide.From, slide.To[1]);

        var bigVSlidePrefabs = NoteGenerator.Instance.slidePrefabs.bigVSlidePrefabs;

        var bigVSlideInstance = interval switch
        {
            1 => Instantiate(bigVSlidePrefabs[0]),
            2 => Instantiate(bigVSlidePrefabs[1]),
            3 => Instantiate(bigVSlidePrefabs[2]),
            4 => Instantiate(bigVSlidePrefabs[3]),
            _ => Instantiate(bigVSlidePrefabs[0])
        };

        bigVSlideInstance.toLaneIndexes = slide.To.Select(x => x - 1).ToArray();
        bigVSlideInstance.fromLaneIndex = fromLane - 1;
        bigVSlideInstance.waitDuration = slide.WaitDuration;
        bigVSlideInstance.slideDuration = slide.SlideDuration;

        return bigVSlideInstance;
    }

    public static SlideBasedNote GenerateBigPqSlide(NoteDataObject.SlideDataObject slide)
    {
        var fromLane = slide.From;
        var toLane = slide.To[0];

        var bigPqInterval = SlideBasedNote.GetIntervalInBothWays(slide.From, slide.To[0]);

        var bigPqSlidePrefabs = NoteGenerator.Instance.slidePrefabs.bigPqSlidePrefabs;

        var interval = slide.Type == NoteDataObject.SlideDataObject.SlideType.BigP
            ? bigPqInterval.clockwiseInterval
            : bigPqInterval.counterClockwiseInterval;

        var bigPqSlideInstance = interval switch
        {
            0 => Instantiate(bigPqSlidePrefabs[0]),
            1 => Instantiate(bigPqSlidePrefabs[1]),
            2 => Instantiate(bigPqSlidePrefabs[2]),
            3 => Instantiate(bigPqSlidePrefabs[3]),
            4 => Instantiate(bigPqSlidePrefabs[4]),
            5 => Instantiate(bigPqSlidePrefabs[5]),
            6 => Instantiate(bigPqSlidePrefabs[6]),
            7 => Instantiate(bigPqSlidePrefabs[7]),
            _ => Instantiate(bigPqSlidePrefabs[0])
        };

        bigPqSlideInstance.toLaneIndex = toLane - 1;
        bigPqSlideInstance.fromLaneIndex = fromLane - 1;
        bigPqSlideInstance.waitDuration = slide.WaitDuration;
        bigPqSlideInstance.slideDuration = slide.SlideDuration;

        return bigPqSlideInstance;
    }

    public static SlideBasedNote GenerateZsSlide(NoteDataObject.SlideDataObject slide)
    {
        var fromLane = slide.From;
        var toLane = slide.To[0];

        var zsSlideInstance = Instantiate(NoteGenerator.Instance.slidePrefabs.zsSlidePrefab);

        zsSlideInstance.toLaneIndex = toLane - 1;
        zsSlideInstance.fromLaneIndex = fromLane - 1;
        zsSlideInstance.waitDuration = slide.WaitDuration;
        zsSlideInstance.slideDuration = slide.SlideDuration;

        return zsSlideInstance;
    }

    public static SlideBasedNote GenerateWifiSlide(NoteDataObject.SlideDataObject slide)
    {
        var fromLane = slide.From;
        var toLane = slide.To[0];

        var wifiSlideInstance = Instantiate(NoteGenerator.Instance.slidePrefabs.wifiSlidePrefab);

        wifiSlideInstance.toLaneIndex = toLane - 1;
        wifiSlideInstance.fromLaneIndex = fromLane - 1;
        wifiSlideInstance.waitDuration = slide.WaitDuration;
        wifiSlideInstance.slideDuration = slide.SlideDuration;

        return wifiSlideInstance;
    }
}