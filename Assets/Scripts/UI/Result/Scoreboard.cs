using Game.Notes;
using UnityEngine;

namespace UI.Result
{
    public class SpecifiedNoteScoreboard
    {
        public int TotalCount { set; get; }
        public int PerfectCount { private set; get; }
        public int GreatCount { private set; get; }
        public int GoodCount { private set; get; }
        public int MissCount { private set; get; }

        public void Count(JudgeState judgeState)
        {
            switch (judgeState)
            {
                case JudgeState.CriticalPerfect or JudgeState.SemiCriticalPerfect or JudgeState.Perfect:
                    PerfectCount++;
                    break;
                case JudgeState.Great or JudgeState.SemiGreat or JudgeState.QuarterGreat:
                    GreatCount++;
                    break;
                case JudgeState.Good:
                    GoodCount++;
                    break;
                case JudgeState.Miss:
                    MissCount++;
                    break;
            }
        }
    }

    public static class Scoreboard
    {
        public static int Score;
        public static int DeductedScore;
        public static int TotalScore;
        public static int TotalScoreWithExtraScore;

        public static int Combo;

        public static int HighestCombo;

        public static SpecifiedNoteScoreboard SlideCount = new SpecifiedNoteScoreboard();
        public static SpecifiedNoteScoreboard HoldCount = new SpecifiedNoteScoreboard();
        public static SpecifiedNoteScoreboard TapCount = new SpecifiedNoteScoreboard();
        public static SpecifiedNoteScoreboard BreakCount = new SpecifiedNoteScoreboard();

        public static void Reset()
        {
            SlideCount = new SpecifiedNoteScoreboard();
            HoldCount = new SpecifiedNoteScoreboard();
            TapCount = new SpecifiedNoteScoreboard();
            BreakCount = new SpecifiedNoteScoreboard();
            Score = 0;
            TotalScore = 0;
            Combo = 0;
            HighestCombo = 0;
        }

        public static float GetDeductedAchievement()
        {
            return DeductedScore / (float)TotalScore * 100;
        }

        public static float GetAchievement()
        {
            return Score / (float)TotalScore * 100;
        }

        public static void ResetCombo()
        {
            if (Combo > HighestCombo)
                Combo = HighestCombo;

            Combo = 0;
        }
    }
}