using System;
using System.Collections.Generic;
using System.Globalization;
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

                var holdMatch = ParseHold(holdOrSlideNoteString, bpm);
                if (holdMatch.Success)
                {
                    holds.Add(new HoldDataObject
                    {
                        HoldDuration = (int)(holdMatch.HoldDuration * 1000),
                        Lane = lane
                    });

                    continue;
                }

                // Process slides.

                var slideStringSplitResult = holdOrSlideNoteString.Split('*');
                var separatedSlideStrings = slideStringSplitResult.Length > 1
                    ? slideStringSplitResult
                    : new[] { holdOrSlideNoteString };

                foreach (var separatedSlideString in separatedSlideStrings)
                {
                    var slideMatch = ParseSlide(separatedSlideString.Trim(), bpm);

                    if (!slideMatch.Success)
                        continue;

                    var slideTypeString = slideMatch.RemainingInput.Trim();

                    if (!SlideDataObject.SlideStringToSlideType.TryGetValue(slideTypeString, out var slideType))
                    {
                        Debug.Log("Slide type not supported");
                        continue;
                    }

                    var slideDuration = (int)(slideMatch.SlideDuration * 1000);
                    var waitDuration = (int)(slideMatch.WaitDuration * 1000);

                    slides.Add(new SlideDataObject
                    {
                        From = lane,
                        To = slideMatch.To,
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

        private SlideResult ParseSlide(string input, double globalBpm)
        {
            var result = new SlideResult { RemainingInput = input };
            var quarter = 60.0 / globalBpm;

            var cases = new (string pattern, Action<Match> action)[]
            {
                (@"([1-8]{1,2})\[([0-9]*?):([0-9]*?)\]", m =>
                {
                    result.To = m.Groups[1].Value.Select(c => int.Parse(c.ToString())).ToArray();
                    var start = ParseNum(m.Groups[2].Value);
                    var end = ParseNum(m.Groups[3].Value);
                    var noteDuration = 4.0 / start * quarter;
                    result.SlideDuration = noteDuration * end;
                    result.WaitDuration = quarter;
                }),
                (@"([1-8]{1,2})\[(\d+\.\d+?|\d+)#([0-9]*?):([0-9]*?)\]", m =>
                {
                    result.To = m.Groups[1].Value.Select(c => int.Parse(c.ToString())).ToArray();
                    var bpm = ParseNum(m.Groups[2].Value);
                    var start = ParseNum(m.Groups[3].Value);
                    var end = ParseNum(m.Groups[4].Value);
                    var q = 60.0 / bpm;
                    var noteDuration = 4.0 / start * q;
                    result.SlideDuration = noteDuration * end;
                    result.WaitDuration = q;
                }),
                (@"([1-8]{1,2})\[(\d+\.\d+?|\d+)#(\d+\.\d+?|\d+)\]", m =>
                {
                    result.To = m.Groups[1].Value.Select(c => int.Parse(c.ToString())).ToArray();
                    var bpm = ParseNum(m.Groups[2].Value);
                    var slide = ParseNum(m.Groups[3].Value);
                    result.SlideDuration = slide;
                    result.WaitDuration = 60.0 / bpm;
                }),
                (@"([1-8]{1,2})\[(\d+\.\d+?|\d+)##(\d+\.\d+?|\d+)\]", m =>
                {
                    result.To = m.Groups[1].Value.Select(c => int.Parse(c.ToString())).ToArray();
                    result.WaitDuration = ParseNum(m.Groups[2].Value);
                    result.SlideDuration = ParseNum(m.Groups[3].Value);
                }),
                (@"([1-8]{1,2})\[(\d+\.\d+?|\d+)##([0-9]*?):([0-9]*?)\]", m =>
                {
                    result.To = m.Groups[1].Value.Select(c => int.Parse(c.ToString())).ToArray();
                    result.WaitDuration = ParseNum(m.Groups[2].Value);
                    var start = ParseNum(m.Groups[3].Value);
                    var end = ParseNum(m.Groups[4].Value);
                    var noteDuration = 4.0 / start * quarter;
                    result.SlideDuration = noteDuration * end;
                }),
                (@"([1-8]{1,2})\[(\d+\.\d+?|\d+)##(\d+\.\d+?|\d+)#([0-9]*?):([0-9]*?)\]", m =>
                {
                    result.To = m.Groups[1].Value.Select(c => int.Parse(c.ToString())).ToArray();
                    result.WaitDuration = ParseNum(m.Groups[2].Value);
                    var bpm = ParseNum(m.Groups[3].Value);
                    var start = ParseNum(m.Groups[4].Value);
                    var end = ParseNum(m.Groups[5].Value);
                    var q = 60.0 / bpm;
                    var noteDuration = 4.0 / start * q;
                    result.SlideDuration = noteDuration * end;
                })
            };

            foreach (var (pattern, action) in cases)
            {
                var m = Regex.Match(input, pattern);
                if (!m.Success)
                    continue;

                action(m);
                result.Success = true;

                result.RemainingInput = new Regex(pattern).Replace(input, "", 1);
                break;
            }

            return result;
        }

        private double ParseNum(string s)
        {
            return double.Parse(s, CultureInfo.InvariantCulture);
        }

        private class SlideResult
        {
            public bool Success { get; set; }
            public int[] To { get; set; } = Array.Empty<int>();
            public double SlideDuration { get; set; }
            public double WaitDuration { get; set; }
            public string RemainingInput { get; set; } = string.Empty;
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