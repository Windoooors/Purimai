using System.Collections.Generic;
using Game.NoteEffects;
using UnityEngine;

namespace Game
{
    public class JudgeDisplayManager : MonoBehaviour
    {
        public static JudgeDisplayManager Instance;

        public List<TapJudgeDisplayHandler> judgeDisplayAnimators;
        public List<TapJudgeDisplayHandler> offsetDisplayAnimators;

        public void Awake()
        {
            Instance = this;
        }
    }
}