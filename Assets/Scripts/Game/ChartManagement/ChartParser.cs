using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UI.Settings;
using UnityEngine;

namespace Game.ChartManagement
{
    public class ChartLoader : MonoBehaviour
    {
        private static readonly Regex NoteRegex = new("^.*?[,|`]");
        private static readonly Regex BpmRegex = new(@"^\(([^)]*?)\)");
        private static readonly Regex NoteValueRegex = new(@"^\{([^)]*?)\}");

        public static ChartLoader Instance;
        private double _bpm;

        private string _chartString;
        private double _firstNoteTime;
        private int _noteValue;
        private double _time;

        private void Awake()
        {
            Instance = this;
        }

        public NoteDataObject[] Parse(string chartString, float firstNoteTime)
        {
            _chartString = chartString.Trim().Replace("\n", "").Replace(" ", "");

            _firstNoteTime = firstNoteTime + SettingsPool.GetValue("audio_delay") / 1000f;

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
                var isNotSoleTimingMark = match.Groups[0].Value is not ("," or "`");

                if (isNotSoleTimingMark)
                    noteDataObject = new NoteDataObject(match.Groups[0].Value, (int)((_time + _firstNoteTime) * 1000),
                        _bpm, _time + _firstNoteTime);

                var timingMark = NoteRegex.Match(_chartString).Value.ToCharArray()[^1];

                _time += timingMark == ',' ? 4 * (60f / _bpm / _noteValue) : 0.001f;

                _chartString = NoteRegex.Replace(_chartString, "", 1).Trim();

                return isNotSoleTimingMark;
            }

            return false;
        }
    }

    public class NoteDataObject
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

        private static readonly Regex HeadRegex = new("^[A-Z]?([1-8])"); // Touch will fall back to tap

        public static Dictionary<SlideType, int> SlideTypeToSlideStringLength = new()
        {
            { SlideType.RotateLeft, 1 },
            { SlideType.RotateRight, 1 },
            { SlideType.RotateMinorArc, 1 },
            { SlideType.Line, 1 },
            { SlideType.LittleV, 1 },
            { SlideType.S, 1 },
            { SlideType.Z, 1 },
            { SlideType.P, 1 },
            { SlideType.Q, 1 },
            { SlideType.BigP, 2 },
            { SlideType.BigQ, 2 },
            { SlideType.Wifi, 1 },
            { SlideType.BigV, 1 }
        };

        public static readonly Dictionary<string, SlideType> SlideStringToSlideType = new()
        {
            { "pp", SlideType.BigP },
            { "qq", SlideType.BigQ },
            { "<", SlideType.RotateLeft },
            { ">", SlideType.RotateRight },
            { "^", SlideType.RotateMinorArc },
            { "-", SlideType.Line },
            { "v", SlideType.LittleV },
            { "s", SlideType.S },
            { "z", SlideType.Z },
            { "p", SlideType.P },
            { "q", SlideType.Q },
            { "W", SlideType.Wifi },
            { "w", SlideType.Wifi },
            { "V", SlideType.BigV }
        };

        public readonly HoldDataObject[] HoldDataObjects;
        public readonly SlideDataObject[] SlideDataObjects;

        public readonly TapDataObject[] TapDataObjects;
        public readonly int Timing;

        public readonly double TimingInSeconds;

        public NoteDataObject(string noteString, int timing, double bpm, double timingInSeconds)
        {
            Timing = timing;
            TimingInSeconds = timingInSeconds;

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
                var isSuddenAppearingSlide = separatedNoteString.Contains("!");

                var separatedNoteStringWithNoHeadProperties =
                    separatedNoteString.Replace("$", "").Replace("b", "").Replace("?", "").Replace("!", "");

                var headMatch = HeadRegex.Match(separatedNoteStringWithNoHeadProperties);

                if (!headMatch.Success)
                    continue;

                if (!int.TryParse(headMatch.Groups[1].Value, out _))
                    continue;

                var lane = int.Parse(headMatch.Groups[1].Value);

                var holdOrSlideNoteString = HeadRegex.Replace(separatedNoteStringWithNoHeadProperties, "", 1).Trim();

                var holdMatch = ParseHold(holdOrSlideNoteString, bpm);
                if (holdMatch.Success)
                {
                    holds.Add(new HoldDataObject
                    {
                        HoldDuration = (int)(holdMatch.HoldDuration * 1000),
                        HoldDurationInSeconds = holdMatch.HoldDuration,
                        Lane = lane
                    });

                    continue;
                }

                // Process slides.

                var slideStringSplitResult = holdOrSlideNoteString.Split('*');
                var separatedSlideStrings = slideStringSplitResult.Length > 1
                    ? slideStringSplitResult
                    : new[] { holdOrSlideNoteString };

                SlideDataObject slideAssociatedWithTap = null;

                foreach (var separatedSlideString in separatedSlideStrings)
                {
                    //var slideCount

                    var slideMatch = ParseSlide(separatedSlideString.Trim(), bpm, lane);

                    if (!slideMatch.Success)
                        continue;

                    var slideDataObject = new SlideDataObject
                    {
                        SuddenlyAppears = isSuddenAppearingSlide,
                        WaitDuration = (int)(slideMatch.TimingObject.WaitDuration * 1000),
                        SlideDuration = (int)(slideMatch.TimingObject.SlideDuration * 1000),
                        IndividualSlides = slideMatch.IndividualSlides.Select(x => new IndividualSlideDataObject
                        {
                            Type = x.SlideType,
                            From = x.From,
                            To = x.To.ToArray()
                        }).ToArray()
                    };

                    slides.Add(slideDataObject);

                    slideAssociatedWithTap = slideDataObject;
                }

                if (!isNoHeadSlide)
                    taps.Add(new TapDataObject
                    {
                        IsBreak = isBreak,
                        Lane = lane,
                        IsStarHead = !isTapStyleStarHead && (isSpinningStarHead || isNoSpinningStarHead ||
                                                             slides.Exists(x => x.IndividualSlides[0].From == lane)),
                        IsNoSpinningStarHead = !isTapStyleStarHead && isNoSpinningStarHead,
                        IsDoubleStarHead = !isTapStyleStarHead &&
                                           slides.Where(x => x.IndividualSlides[0].From == lane).Select(x => x)
                                               .ToArray().Length > 1,
                        RotateSpeed = 1000 / slideAssociatedWithTap?.SlideDuration ?? 0f
                    });
            }

            TapDataObjects = taps.ToArray();
            SlideDataObjects = slides.ToArray();
            HoldDataObjects = holds.ToArray();
        }

        private HoldResult ParseHold(string input, double globalBpm)
        {
            var result = new HoldResult();
            var quarter = 60.0 / globalBpm;

            var cases = new (string pattern, Action<Match> action)[]
            {
                (@"h\[([0-9]*)\:([0-9]*)\]", m =>
                {
                    var start = ParseNum(m.Groups[1].Value);
                    var end = ParseNum(m.Groups[2].Value);
                    var noteDuration = 4.0 / start * quarter;
                    result.HoldDuration = noteDuration * end;
                }),
                (@"h\[(\d+\.\d+?|\d+)#([0-9]*)\:([0-9]*)\]", m =>
                {
                    var bpm = ParseNum(m.Groups[1].Value);
                    var start = ParseNum(m.Groups[2].Value);
                    var end = ParseNum(m.Groups[3].Value);
                    var q = 60.0 / bpm;
                    var noteDuration = 4.0 / start * q;
                    result.HoldDuration = noteDuration * end;
                }),
                (@"h\[#(\d+\.\d+?|\d+)\]", m => { result.HoldDuration = ParseNum(m.Groups[1].Value); }),
                ("h", _ => { result.HoldDuration = 0; })
            };

            foreach (var (pattern, action) in cases)
            {
                var m = Regex.Match(input, pattern);
                if (!m.Success)
                    continue;

                action(m);
                result.Success = true;

                break;
            }

            return result;
        }

        private SlideResult ParseSlide(string input, double globalBpm, int fromLane)
        {
            var result = new SlideResult();
            var quarter = 60.0 / globalBpm;

            var timingCases = new (string pattern, Action<Match, SlideTimingObject, bool> action)[]
            {
                (@"\[([0-9]*?):([0-9]*?)\]", (m, timingObject, first) =>
                {
                    var start = ParseNum(m.Groups[1].Value);
                    var end = ParseNum(m.Groups[2].Value);
                    var noteDuration = 4.0 / start * quarter;
                    timingObject.SlideDuration += noteDuration * end;
                    if (first || timingObject.WaitDuration.CompareTo(-1) == 0) timingObject.WaitDuration = quarter;
                }),
                (@"\[(\d+\.\d+?|\d+)#([0-9]*?):([0-9]*?)\]", (m, timingObject, first) =>
                {
                    var bpm = ParseNum(m.Groups[1].Value);
                    var start = ParseNum(m.Groups[2].Value);
                    var end = ParseNum(m.Groups[3].Value);
                    var customizedQuarter = 60.0 / bpm;
                    var noteDuration = 4.0 / start * customizedQuarter;
                    timingObject.SlideDuration += noteDuration * end;
                    if (first || timingObject.WaitDuration.CompareTo(-1) == 0)
                        timingObject.WaitDuration = customizedQuarter;
                }),
                (@"\[(\d+\.\d+?|\d+)#(\d+\.\d+?|\d+)\]", (m, timingObject, first) =>
                {
                    var bpm = ParseNum(m.Groups[1].Value);
                    var slide = ParseNum(m.Groups[2].Value);
                    timingObject.SlideDuration += slide;
                    if (first || timingObject.WaitDuration.CompareTo(-1) == 0) timingObject.WaitDuration = 60.0 / bpm;
                }),
                (@"\[(\d+\.\d+?|\d+)##(\d+\.\d+?|\d+)\]", (m, timingObject, first) =>
                {
                    if (first || timingObject.WaitDuration.CompareTo(-1) == 0)
                        timingObject.WaitDuration = ParseNum(m.Groups[1].Value);
                    timingObject.SlideDuration += ParseNum(m.Groups[2].Value);
                }),
                (@"\[(\d+\.\d+?|\d+)##([0-9]*?):([0-9]*?)\]", (m, timingObject, first) =>
                {
                    if (first || timingObject.WaitDuration.CompareTo(-1) == 0)
                        timingObject.WaitDuration = ParseNum(m.Groups[1].Value);
                    var start = ParseNum(m.Groups[2].Value);
                    var end = ParseNum(m.Groups[3].Value);
                    var noteDuration = 4.0 / start * quarter;
                    timingObject.SlideDuration += noteDuration * end;
                }),
                (@"\[(\d+\.\d+?|\d+)##(\d+\.\d+?|\d+)#([0-9]*?):([0-9]*?)\]", (m, timingObject, first) =>
                {
                    if (first || timingObject.WaitDuration.CompareTo(-1) == 0)
                        timingObject.WaitDuration = ParseNum(m.Groups[1].Value);
                    var bpm = ParseNum(m.Groups[2].Value);
                    var start = ParseNum(m.Groups[3].Value);
                    var end = ParseNum(m.Groups[4].Value);
                    var customizedQuarter = 60.0 / bpm;
                    var noteDuration = 4.0 / start * customizedQuarter;
                    timingObject.SlideDuration += noteDuration * end;
                })
            };

            var slideTiming = new SlideTimingObject();
            var isFirst = true;

            var from = fromLane;

            var remainingInput = input;

            while (MatchCore(ref slideTiming,
                       isFirst, ref remainingInput))
                isFirst = false;

            var slideCharArray = remainingInput.ToCharArray();

            var individualSlideList = new List<IndividualSlideResult>();

            for (var i = 0; i < slideCharArray.Length; i++)
            {
                var isDigit = char.IsDigit(slideCharArray[i]);

                if (isDigit)
                {
                    individualSlideList[^1].To.Add(int.Parse(slideCharArray[i].ToString()));
                }
                else
                {
                    var type = SlideType.Line;

                    if (i < slideCharArray.Length - 1 && !char.IsDigit(slideCharArray[i + 1]))
                    {
                        var typeString = string.Concat(new[] { slideCharArray[i], slideCharArray[i + 1] });

                        if (SlideStringToSlideType.TryGetValue(typeString, out type))
                        {
                            i++;
                            individualSlideList.Add(new IndividualSlideResult
                            {
                                From = individualSlideList.Count == 0 ? from : individualSlideList[^1].To[^1],
                                SlideType = type
                            });
                            continue;
                        }
                    }

                    var singleLengthTypeString = string.Concat(new[] { slideCharArray[i] });

                    if (!SlideStringToSlideType.TryGetValue(singleLengthTypeString, out type)) continue;

                    individualSlideList.Add(new IndividualSlideResult
                    {
                        From = individualSlideList.Count == 0 ? from : individualSlideList[^1].To[^1],
                        SlideType = type
                    });
                }
            }

            result.IndividualSlides = individualSlideList.ToArray();
            result.TimingObject = slideTiming;

            return result;

            bool MatchCore(ref SlideTimingObject timingObject, bool first, ref string noteInput)
            {
                var count = 0;

                foreach (var (pattern, action) in timingCases)
                {
                    var matches = Regex.Matches(noteInput, pattern);

                    foreach (Match m in matches)
                    {
                        if (!m.Success)
                            continue;

                        action(m, timingObject, first);
                        result.Success = true;

                        noteInput = new Regex(pattern).Replace(noteInput, "");
                    }

                    count += matches.Count;
                }

                return count != 0;
            }
        }

        private double ParseNum(string s)
        {
            return double.Parse(s, CultureInfo.InvariantCulture);
        }

        private class SlideTimingObject
        {
            public double SlideDuration { get; set; }
            public double WaitDuration { get; set; } = -1;
        }

        private class SlideResult
        {
            public bool Success { get; set; }

            public IndividualSlideResult[] IndividualSlides { get; set; }

            public SlideTimingObject TimingObject { get; set; }

            //public double WaitDuration { get; set; } = -1;
            //public string RemainingInput { get; set; } = string.Empty;
        }

        private class IndividualSlideResult
        {
            public SlideType SlideType;
            public int From { get; set; }
            public List<int> To { get; } = new();
        }

        private class HoldResult
        {
            public bool Success { get; set; }
            public double HoldDuration { get; set; }
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

            public float RotateSpeed;
        }

        public class HoldDataObject : TapDataObjectBase
        {
            public int HoldDuration;
            public double HoldDurationInSeconds;
        }

        public class SlideDataObject
        {
            public IndividualSlideDataObject[] IndividualSlides;
            public int SlideDuration;
            public bool SuddenlyAppears;
            public int WaitDuration;
        }

        public class IndividualSlideDataObject
        {
            public int From;
            public int[] To;
            public SlideType Type;
        }
    }
}