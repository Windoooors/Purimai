using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ChartManagement
{
    public class ChartLoader : MonoBehaviour
    {
        private static readonly Regex NoteRegex = new("^.*?,");
        private static readonly Regex BpmRegex = new(@"^\(([^)]*?)\)");
        private static readonly Regex NoteValueRegex = new(@"^\{([^)]*?)\}");

        public static ChartLoader Instance;
        public double firstNoteTime;
        private double _bpm;

        private string _chartString;
        private int _noteValue;
        private double _time;

        private void Awake()
        {
            Instance = this;
        }

        public NoteDataObject[] Parse(string chartString)
        {
            _chartString = chartString.Trim().Replace("\n", "").Replace(" ", "");

            var noteList = new List<NoteDataObject>();

            while (true)
            {
                ParseBpm();
                ParseNoteValue();

                if (TryParseNote(out var note))
                    noteList.Add(note);

                if (_chartString is "E" or "")
                    break;
            }

            _chartString = string.Empty;
            _noteValue = 0;
            _bpm = 0;
            _time = 0;

            return noteList.ToArray();
        }

        private void ParseBpm()
        {
            var bpmParsed = double.TryParse(BpmRegex.Match(_chartString).Groups[1].Value, out var bpm);

            if (bpmParsed)
            {
                _bpm = bpm;
                _chartString = BpmRegex.Replace(_chartString, "", 1).Trim();
            }
        }

        private void ParseNoteValue()
        {
            var valueString = NoteValueRegex.Match(_chartString).Groups[1].Value;

            var noteValueParsed = int.TryParse(valueString, out var noteValue);

            if (noteValueParsed)
            {
                _noteValue = noteValue;
                _chartString = NoteValueRegex.Replace(_chartString, "", 1).Trim();
            }
        }

        private bool TryParseNote(out NoteDataObject noteDataObject)
        {
            var match = NoteRegex.Match(_chartString);

            var noteParsed = match.Success;

            noteDataObject = null;

            if (noteParsed)
            {
                var isNotSoleTimingMark = match.Groups[0].Value != ",";

                if (isNotSoleTimingMark)
                    noteDataObject = new NoteDataObject(match.Groups[0].Value, (int)((_time + firstNoteTime) * 1000),
                        _bpm);

                _time += 4 * (60f / _bpm / _noteValue);
                _chartString = NoteRegex.Replace(_chartString, "", 1).Trim();

                return isNotSoleTimingMark;
            }

            return false;
        }
    }

    public class NoteDataObject
    {
        private static readonly Regex HeadRegex = new("^([1-8])");

        private static readonly Regex HoldRegex =
            new(@"(h\[([0-9]*)\:([0-9]*)\])|(h\[(\d+\.\d+?|\d+)#([0-9]*)\:([0-9]*)\])|(h\[#(\d+\.\d+?|\d+)\])|(h)");

        // I'm sorry for this
        private static readonly Regex SlideRegex = new(
            @"([1-8])([1-8]|)((\[([0-9]*?):([0-9]*?)\])|(\[(\d+\.\d+?|\d+)#([0-9]*?):([0-9]*?)\])|(\[(\d+\.\d+?|\d+)#(\d+\.\d+?|\d+)\])|(\[(\d+\.\d+?|\d+)##(\d+\.\d+?|\d+)\])|(\[(\d+\.\d+?|\d+)##([0-9]*?):([0-9]*?)\])|(\[(\d+\.\d+?|\d+)##(\d+\.\d+?|\d+)#([0-9]*?):([0-9]*?)\]))");

        public readonly HoldDataObject[] HoldDataObjects;
        public readonly SlideDataObject[] SlideDataObjects;

        public readonly TapDataObject[] TapDataObjects;
        public readonly int Timing;

        public NoteDataObject(string noteString, int timing, double bpm)
        {
            Timing = timing;

            var briefEachTapRegex = new Regex("([0-8])([0-8]),");
            var briefEachTapMatch = briefEachTapRegex.Match(noteString);

            var noteStringSplitResult = noteString.Trim().Split("/");
            var separatedNoteStrings = noteStringSplitResult.Length > 1
                ? noteStringSplitResult
                : briefEachTapMatch.Success
                    ? new[] { briefEachTapMatch.Groups[1].Value, briefEachTapMatch.Groups[2].Value }
                    : new[] { noteString };

            separatedNoteStrings = separatedNoteStrings.Select(s => s.Trim(',')).ToArray();

            var taps = new List<TapDataObject>();
            var slides = new List<SlideDataObject>();
            var holds = new List<HoldDataObject>();

            foreach (var separatedNoteString in separatedNoteStrings)
            {
                var isBreak = separatedNoteString.Contains("b");
                var isSpinningStarHead = separatedNoteString.Contains("$$");
                var isNoSpinningStarHead = separatedNoteString.Contains("$") && !isSpinningStarHead;
                var isTapStyleStarHead = separatedNoteString.Contains("@");
                var isNoHeadSlide = separatedNoteString.Contains("?") || separatedNoteString.Contains("!");

                var separatedNoteStringWithNoHeadProperties =
                    separatedNoteString.Replace("$", "").Replace("b", "").Replace("?", "").Replace("!", "");

                var headMatch = HeadRegex.Match(separatedNoteStringWithNoHeadProperties);

                if (!headMatch.Success)
                    continue;

                if (!int.TryParse(headMatch.Groups[1].Value, out _))
                    Debug.Log(headMatch.Groups[1].Value);

                var lane = int.Parse(headMatch.Groups[1].Value);

                var holdOrSlideNoteString = HeadRegex.Replace(separatedNoteStringWithNoHeadProperties, "", 1).Trim();

                var holdMatch = HoldRegex.Match(holdOrSlideNoteString);
                if (holdMatch.Success)
                {
                    var holdDuration = 0;

                    if (holdMatch.Groups[5].Success)
                        holdDuration = (int)(4000 * (60f / double.Parse(holdMatch.Groups[5].Value) /
                                                     int.Parse(holdMatch.Groups[6].Value)) *
                                             int.Parse(holdMatch.Groups[7].Value));

                    if (holdMatch.Groups[9].Success)
                        holdDuration = (int)(double.Parse(holdMatch.Groups[9].Value) * 1000);

                    if (holdMatch.Groups[10].Success)
                        holdDuration = 0;

                    if (holdMatch.Groups[2].Success)
                        holdDuration = (int)(4000 * (60f / bpm / int.Parse(holdMatch.Groups[2].Value)) *
                                             int.Parse(holdMatch.Groups[3].Value));

                    holds.Add(new HoldDataObject
                    {
                        HoldDuration = holdDuration,
                        Lane = lane
                    });
                    continue; // Continue when separated note is a hold one.
                }

                // Process slides.

                var slideStringSplitResult = holdOrSlideNoteString.Split('*');
                var separatedSlideStrings = slideStringSplitResult.Length > 1
                    ? slideStringSplitResult
                    : new[] { holdOrSlideNoteString };

                foreach (var separatedSlideString in separatedSlideStrings)
                {
                    var slideMatch = SlideRegex.Match(separatedSlideString);

                    if (!slideMatch.Success)
                        continue;

                    var slideTypeString = SlideRegex.Replace(separatedSlideString, "", 1).Trim();

                    if (!SlideDataObject.SlideStringToSlideType.TryGetValue(slideTypeString, out var slideType))
                    {
                        Debug.Log("Slide type not supported");
                        continue;
                    }

                    var slideDuration = 0;
                    var waitDuration = 0;

                    if (slideMatch.Groups[8].Success)
                    {
                        slideDuration = (int)(4000 * (60f / double.Parse(slideMatch.Groups[8].Value) /
                                                      int.Parse(slideMatch.Groups[9].Value)) *
                                              int.Parse(slideMatch.Groups[10].Value));
                        waitDuration = (int)(1000 * (60f / double.Parse(slideMatch.Groups[8].Value)));
                    }

                    if (slideMatch.Groups[12].Success)
                    {
                        slideDuration = (int)(1000 * double.Parse(slideMatch.Groups[13].Value));
                        waitDuration = (int)(1000 * (60f / double.Parse(slideMatch.Groups[12].Value)));
                    }

                    if (slideMatch.Groups[15].Success)
                    {
                        slideDuration = (int)(1000 * double.Parse(slideMatch.Groups[16].Value));
                        waitDuration = (int)(1000 * double.Parse(slideMatch.Groups[15].Value));
                    }

                    if (slideMatch.Groups[18].Success)
                    {
                        slideDuration = (int)(4000 * (60f / bpm /
                                                      int.Parse(slideMatch.Groups[19].Value)) *
                                              int.Parse(slideMatch.Groups[20].Value));
                        waitDuration = (int)(1000 * double.Parse(slideMatch.Groups[18].Value));
                    }

                    if (slideMatch.Groups[22].Success)
                    {
                        slideDuration = (int)(4000 * (60f / double.Parse(slideMatch.Groups[23].Value) /
                                                      int.Parse(slideMatch.Groups[24].Value)) *
                                              int.Parse(slideMatch.Groups[25].Value));
                        waitDuration = (int)(1000 * double.Parse(slideMatch.Groups[22].Value));
                    }

                    if (slideMatch.Groups[5].Success)
                    {
                        slideDuration = (int)(4000 * (60f / bpm /
                                                      int.Parse(slideMatch.Groups[5].Value)) *
                                              int.Parse(slideMatch.Groups[6].Value));
                        waitDuration = (int)(1000 * (60f / bpm));
                    }

                    slides.Add(new SlideDataObject
                    {
                        From = lane,
                        To = slideMatch.Groups[2].Value == ""
                            ? new[] { int.Parse(slideMatch.Groups[1].Value) }
                            : new[] { int.Parse(slideMatch.Groups[1].Value), int.Parse(slideMatch.Groups[2].Value) },
                        SlideDuration = slideDuration,
                        WaitDuration = waitDuration,
                        Type = slideType
                    });
                }

                if (!isNoHeadSlide)
                    taps.Add(new TapDataObject
                    {
                        IsBreak = isBreak,
                        Lane = lane,
                        IsStarHead = !isTapStyleStarHead && (isSpinningStarHead || isNoSpinningStarHead ||
                                                             slides.Exists(x => x.From == lane)),
                        IsNoSpinningStarHead = !isTapStyleStarHead && isNoSpinningStarHead,
                        IsDoubleStarHead = !isTapStyleStarHead &&
                                           slides.Where(x => x.From == lane).Select(x => x).ToArray().Length > 1
                    });
            }

            TapDataObjects = taps.ToArray();
            SlideDataObjects = slides.ToArray();
            HoldDataObjects = holds.ToArray();
        }

        public class TapDataObjectBase
        {
            public int Lane;
        }

        public class TapDataObject : TapDataObjectBase
        {
            public bool IsBreak;
            public bool IsDoubleStarHead;
            public bool IsNoSpinningStarHead;
            public bool IsStarHead;
        }

        public class HoldDataObject : TapDataObjectBase
        {
            public int HoldDuration;
        }

        public class SlideDataObject
        {
            public enum SlideType
            {
                RotateRight,
                RotateLeft,
                RotateMinorArc,
                Line,
                LittleV,
                BigV,
                S,
                Z,
                P,
                Q,
                BigP,
                BigQ,
                Wifi
            }

            public static readonly Dictionary<string, SlideType> SlideStringToSlideType = new()
            {
                { "<", SlideType.RotateLeft },
                { ">", SlideType.RotateRight },
                { "^", SlideType.RotateMinorArc },
                { "-", SlideType.Line },
                { "v", SlideType.LittleV },
                { "s", SlideType.S },
                { "z", SlideType.Z },
                { "p", SlideType.P },
                { "q", SlideType.Q },
                { "pp", SlideType.BigP },
                { "qq", SlideType.BigQ },
                { "W", SlideType.Wifi },
                { "w", SlideType.Wifi },
                { "V", SlideType.BigV }
            };

            public int From;
            public int SlideDuration;
            public int[] To;

            public SlideType Type;
            public int WaitDuration;
        }
    }
}