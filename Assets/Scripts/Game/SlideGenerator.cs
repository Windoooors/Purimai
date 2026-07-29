using System;
using Game.ChartManagement;
using Game.Notes;
using Game.Notes.NormalIndividualSlides;
using UnityEngine;

namespace Game
{
    public class SlideGenerator : MonoBehaviour
    {
        public static IndividualSlideBase GenerateCycleSlide(NoteDataObject.IndividualSlideDataObject slide)
        {
            var cycleSlidePrefabs = NoteGenerator.Instance.individualSlidePrefabs.cycleSlidePrefabs;

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

            cycleSlideInstance.individualSlideDataObject = slide;

            return cycleSlideInstance;
        }

        public static IndividualSlideBase GeneratePqSlide(NoteDataObject.IndividualSlideDataObject slide)
        {
            var pqInterval
                = IndividualSlideBase.GetIntervalInBothWays(slide.From, slide.To[0]);

            var pqSlidePrefabs = NoteGenerator.Instance.individualSlidePrefabs.pqSlidePrefabs;

            var interval = slide.Type == NoteDataObject.SlideType.P
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

            pqSlideInstance.individualSlideDataObject = slide;
            return pqSlideInstance;
        }

        public static IndividualSlideBase GenerateLittleVSlide(NoteDataObject.IndividualSlideDataObject slide)
        {
            var fromLane = slide.From;
            var toLane = slide.To[0];

            var interval = IndividualSlideBase.GetIntervalInBothWays(slide.From, slide.To[0]).clockwiseInterval;

            var vSlidePrefabs = NoteGenerator.Instance.individualSlidePrefabs.vSlidePrefabs;

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

            vSlideInstance.individualSlideDataObject = slide;

            return vSlideInstance;
        }

        public static IndividualSlideBase GenerateLineSlide(NoteDataObject.IndividualSlideDataObject slide)
        {
            var fromLane = slide.From;
            var toLane = slide.To[0];

            var interval = IndividualSlideBase.GetIntervalInBothWays(slide.From, slide.To[0]).clockwiseInterval;

            var lineSlidePrefabs = NoteGenerator.Instance.individualSlidePrefabs.lineSlidePrefabs;

            var lineSlideInstance = interval switch
            {
                2 => Instantiate(lineSlidePrefabs[0]),
                3 => Instantiate(lineSlidePrefabs[1]),
                4 => Instantiate(lineSlidePrefabs[2]),
                5 => Instantiate(lineSlidePrefabs[3]),
                6 => Instantiate(lineSlidePrefabs[4]),
                _ => Instantiate(lineSlidePrefabs[0])
            };

            lineSlideInstance.individualSlideDataObject = slide;

            return lineSlideInstance;
        }

        public static IndividualSlideBase GenerateBigVSlide(NoteDataObject.IndividualSlideDataObject slide)
        {
            var fromLane = slide.From;

            var interval = IndividualSlideBase.GetShortestInterval(slide.From, slide.To[1]);

            var bigVSlidePrefabs = NoteGenerator.Instance.individualSlidePrefabs.bigVSlidePrefabs;

            var bigVSlideInstance = interval switch
            {
                1 => Instantiate(bigVSlidePrefabs[0]),
                2 => Instantiate(bigVSlidePrefabs[1]),
                3 => Instantiate(bigVSlidePrefabs[2]),
                4 => Instantiate(bigVSlidePrefabs[3]),
                _ => Instantiate(bigVSlidePrefabs[0])
            };

            bigVSlideInstance.individualSlideDataObject = slide;

            return bigVSlideInstance;
        }

        public static IndividualSlideBase GenerateBigPqSlide(NoteDataObject.IndividualSlideDataObject slide)
        {
            var fromLane = slide.From;
            var toLane = slide.To[0];

            var bigPqInterval = IndividualSlideBase.GetIntervalInBothWays(slide.From, slide.To[0]);

            var bigPqSlidePrefabs = NoteGenerator.Instance.individualSlidePrefabs.bigPqSlidePrefabs;

            var interval = slide.Type == NoteDataObject.SlideType.BigP
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

            bigPqSlideInstance.individualSlideDataObject = slide;

            return bigPqSlideInstance;
        }

        public static IndividualSlideBase GenerateZsSlide(NoteDataObject.IndividualSlideDataObject slide)
        {
            var fromLane = slide.From;
            var toLane = slide.To[0];

            var zsSlideInstance = Instantiate(NoteGenerator.Instance.individualSlidePrefabs.zsSlidePrefab);

            zsSlideInstance.individualSlideDataObject = slide;

            return zsSlideInstance;
        }

        public static WifiSlide GenerateWifiSlide(NoteDataObject.SlideDataObject slide)
        {
            if (slide.IndividualSlides.Length < 1 || slide.IndividualSlides[0].Type != NoteDataObject.SlideType.Wifi)
                throw new Exception("Invalid wifi slide data.");

            var individualSlideData = slide.IndividualSlides[0];

            var wifiSlideInstance = Instantiate(NoteGenerator.Instance.wifiSlidePrefab);

            wifiSlideInstance.slideData = individualSlideData;

            return wifiSlideInstance;
        }
    }
}