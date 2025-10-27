using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class TapJudgeDisplayManager : MonoBehaviour
    {
        public static TapJudgeDisplayManager Instance;

        public List<Animator> judgeDisplayAnimators;

        public void Awake()
        {
            Instance = this;
        }
    }
}