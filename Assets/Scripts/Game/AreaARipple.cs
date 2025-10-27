using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class AreaARipple : MonoBehaviour
    {
        public static readonly List<AreaARipple> AreaARipples = new();
        public string sensorId;

        private Animator _animator;

        private void Start()
        {
            AreaARipples.Add(this);

            SimulatedSensor.OnTap += Show;

            _animator = GetComponent<Animator>();
        }

        public void CancelAnimation()
        {
            _animator.SetTrigger("Reset");
        }

        private void Show(object sender, TouchEventArgs e)
        {
            if (e.SensorId == sensorId)
                _animator.SetTrigger("ShowRipple");
        }
    }
}