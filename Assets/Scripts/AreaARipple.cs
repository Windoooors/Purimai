
    using System.Collections.Generic;
    using UnityEngine;

    public class AreaARipple : MonoBehaviour
    {
        public static readonly List<AreaARipple> AreaARipples = new();
        
        private Animator _animator;
        public string sensorId;

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
