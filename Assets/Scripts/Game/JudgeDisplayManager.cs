using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class JudgeDisplayManager : MonoBehaviour
    {
        public static JudgeDisplayManager Instance;

        public List<Animator> judgeDisplayAnimators;
        public List<Animator> offsetDisplayAnimators;

        public void Awake()
        {
            Instance = this;
        }
    }
}