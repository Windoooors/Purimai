using System.Collections;
using System.Collections.Generic;
using UI.Settings;
using UnityEngine;

namespace Game
{
    public class KeyFrameManager
    {
        private readonly Dictionary<string, List<AutoPlayKeyFrame>> _keys = new();

        public AutoPlayKeyFrame[] ToArray()
        {
            var result = new List<AutoPlayKeyFrame>();

            foreach (var key in _keys.Keys)
            {
                _keys.TryGetValue(key, out var list);

                list?.ForEach(x => x.SensorId = key);

                if (list != null)
                    result.AddRange(list);
            }

            result.Sort((x, y) => x.Time.CompareTo(y.Time));

            return result.ToArray();
        }

        public List<AutoPlayKeyFrame> GetKeyFrames(string sensorId)
        {
            if (!_keys.TryGetValue(sensorId, out var list))
            {
                list = new List<AutoPlayKeyFrame>();

                _keys.Add(sensorId, list);
            }

            return list;
        }

        public void Sort()
        {
            if (_keys == null) return;

            foreach (var group in _keys)
            {
                var frames = group.Value;
                if (frames == null || frames.Count < 2) continue;

                frames.Sort((a, b) => a.Time.CompareTo(b.Time));

                var holdIntervals = new List<(int start, int end)>();
                AutoPlayKeyFrame lastDown = null;

                foreach (var frame in frames)
                    if (frame.HoldNote)
                    {
                        if (frame.ManipulateType == AutoPlayKeyFrame.Type.PressDown)
                        {
                            lastDown = frame;
                        }
                        else if (frame.ManipulateType == AutoPlayKeyFrame.Type.PressUp && lastDown != null)
                        {
                            holdIntervals.Add((lastDown.Time, frame.Time));
                            lastDown = null;
                        }
                    }

                for (var i = frames.Count - 1; i >= 0; i--)
                {
                    var frame = frames[i];

                    if (frame.ManipulateType == AutoPlayKeyFrame.Type.PressUp && !frame.HoldNote)
                        foreach (var interval in holdIntervals)
                            if (frame.Time >= interval.start && frame.Time <= interval.end)
                            {
                                frames.RemoveAt(i);
                                break;
                            }
                }
            }
        }

        public void Clear()
        {
            _keys.Clear();
        }
    }

    public class AutoPlayer : MonoBehaviour
    {
        public static KeyFrameManager KeyFrameManager = new();
        private bool _autoEnabled;
        private bool _initialized;

        private AutoPlayKeyFrame[] _keyFrames;

        private int _keyIndex;

        private void Awake()
        {
            _autoEnabled = SettingsPool.GetValue("auto_play") == 1;

            if (!_autoEnabled)
                enabled = false;
        }

        private void Start()
        {
            StartCoroutine(AddKeyFrames());
        }

        public void Update()
        {
            if (!_initialized)
                return;

            var time = ChartPlayer.Instance.TimeInMilliseconds;

            if (_keyIndex >= _keyFrames.Length)
            {
                enabled = false;
                return;
            }

            while (_keyFrames[_keyIndex].Time + ChartPlayer.Instance.judgeDelay < time)
            {
                var key = _keyFrames[_keyIndex];

                switch (key.ManipulateType)
                {
                    case AutoPlayKeyFrame.Type.PressDown:
                        SimulatedSensor.OnTap?.Invoke(this, new TouchEventArgs(key.SensorId));
                        break;
                    case AutoPlayKeyFrame.Type.PressUp:
                        SimulatedSensor.OnLeave?.Invoke(this, new TouchEventArgs(key.SensorId));
                        break;
                    case AutoPlayKeyFrame.Type.Hold:
                        SimulatedSensor.OnHold?.Invoke(this, new TouchEventArgs(key.SensorId));
                        break;
                }

                _keyIndex++;

                if (_keyIndex >= _keyFrames.Length) break;
            }
        }

        private IEnumerator AddKeyFrames()
        {
            yield return new WaitForEndOfFrame();

            SimulatedSensor.Enabled = false;

            KeyFrameManager.Clear();

            NoteGenerator.Instance.notesList.ForEach(x => x.AddAutoPlayKeyFrame());

            KeyFrameManager.Sort();

            _keyFrames = KeyFrameManager.ToArray();

            _initialized = true;
        }
    }
}