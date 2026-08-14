using System.Collections.Generic;
using Game.NoteEffects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    public class AreaARipple : MonoBehaviour
    {
        public static readonly List<AreaARipple> AreaARipples = new();
        public string sensorId;
        
        private TapJudgeDisplayHandler _handler;

        private void Start()
        {
            AreaARipples.Add(this);

            SimulatedSensor.OnTap += Show;

            _handler = GetComponent<TapJudgeDisplayHandler>();

            SceneManager.sceneLoaded += ClearList;
            return;

            void ClearList(Scene scene, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= ClearList;
                AreaARipples.Clear();
            }
        }

        public void CancelAnimation()
        {
            _handler.Stop();
        }

        private void Show(object sender, TouchEventArgs e)
        {
            if (e.SensorId == sensorId)
                _handler.Show("ShowRipple");
        }
    }
}