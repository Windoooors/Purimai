using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Notes
{
    public class JudgeManager : MonoBehaviour
    {
        private static JudgeManager _instance;
        private readonly List<JudgeAction> _holdJudgeActions = new();
        private readonly List<JudgeAction> _leaveJudgeActions = new();

        private readonly List<JudgeAction> _tapJudgeActions = new();
        public static JudgeManager Instance => _instance ??= FindAnyObjectByType<JudgeManager>();

        private void Awake()
        {
            _instance = this;

            SimulatedSensor.OnTap += OnTap;
            SimulatedSensor.OnLeave += OnLeave;
            SimulatedSensor.OnHold += OnHold;
        }

        private void OnDestroy()
        {
            SimulatedSensor.OnTap -= OnTap;
            SimulatedSensor.OnLeave -= OnLeave;
            SimulatedSensor.OnHold -= OnHold;
        }

        private void OnTap(object sender, TouchEventArgs e)
        {
            _tapJudgeActions.ForEach(x =>
            {
                if (x.Enabled && ChartPlayer.Instance.TimeInMilliseconds >= x.EnableTiming &&
                    ChartPlayer.Instance.TimeInMilliseconds <= x.DisableTiming)
                    x.Callback(sender, e);
            });
        }

        private void OnLeave(object sender, TouchEventArgs e)
        {
            _leaveJudgeActions.ForEach(x =>
            {
                if (x.Enabled && ChartPlayer.Instance.TimeInMilliseconds >= x.EnableTiming &&
                    ChartPlayer.Instance.TimeInMilliseconds <= x.DisableTiming)
                    x.Callback(sender, e);
            });
        }

        private void OnHold(object sender, TouchEventArgs e)
        {
            _holdJudgeActions.ForEach(x =>
            {
                if (x.Enabled && ChartPlayer.Instance.TimeInMilliseconds >= x.EnableTiming &&
                    ChartPlayer.Instance.TimeInMilliseconds <= x.DisableTiming)
                    x.Callback(sender, e);
            });
        }

        public void RegisterTap(int enableTiming, int disableTiming, Action<object, TouchEventArgs> callback,
            out JudgeAction action)
        {
            action = new JudgeAction
                { EnableTiming = enableTiming, DisableTiming = disableTiming, Callback = callback };
            _tapJudgeActions.Add(action);
        }

        public void RegisterHold(int enableTiming, int disableTiming, Action<object, TouchEventArgs> callback,
            out JudgeAction action)
        {
            action = new JudgeAction
                { EnableTiming = enableTiming, DisableTiming = disableTiming, Callback = callback };
            _holdJudgeActions.Add(action);
        }

        public void RegisterLeave(int enableTiming, int disableTiming, Action<object, TouchEventArgs> callback,
            out JudgeAction action)
        {
            action = new JudgeAction
                { EnableTiming = enableTiming, DisableTiming = disableTiming, Callback = callback };
            _leaveJudgeActions.Add(action);
        }

        public class JudgeAction
        {
            public Action<object, TouchEventArgs> Callback;
            public int DisableTiming;

            public bool Enabled = true;
            public int EnableTiming;
        }
    }
}