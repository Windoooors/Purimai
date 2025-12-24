using Game.Notes;

namespace UI.Result
{
    public class SpecifiedNoteScoreboard
    {
        public int TotalCount { set; get; }
        public int CurrentCount { private set; get; }
        public int CriticalPerfectCount { private set; get; }
        public int SemiCriticalPerfectCount { private set; get; }
        public int PerfectCount { private set; get; }
        public int GreatCount { private set; get; }
        public int SemiGreatCount { private set; get; }
        public int QuarterGreatCount { private set; get; }
        public int GoodCount { private set; get; }
        public int MissCount { private set; get; }

        public void Count(JudgeState judgeState)
        {
            switch (judgeState)
            {
                case JudgeState.CriticalPerfect:
                    CriticalPerfectCount++;
                    break;
                case JudgeState.SemiCriticalPerfect:
                    SemiCriticalPerfectCount++;
                    break;
                case JudgeState.Perfect:
                    PerfectCount++;
                    break;
                case JudgeState.Great:
                    GreatCount++;
                    break;
                case JudgeState.SemiGreat:
                    SemiGreatCount++;
                    break;
                case JudgeState.QuarterGreat:
                    QuarterGreatCount++;
                    break;
                case JudgeState.Good:
                    GoodCount++;
                    break;
                case JudgeState.Miss:
                    MissCount++;
                    break;
            }

            CurrentCount++;
        }

        public int GetPerfectCount()
        {
            return PerfectCount + SemiCriticalPerfectCount + CriticalPerfectCount;
        }

        public int GetGreatCount()
        {
            return GreatCount + SemiGreatCount + QuarterGreatCount;
        }
    }

    public static class Scoreboard
    {
        public static int Combo;

        private static int _highestComboAfterReset;

        public static SpecifiedNoteScoreboard SlideCount = new();
        public static SpecifiedNoteScoreboard HoldCount = new();
        public static SpecifiedNoteScoreboard TapCount = new();
        public static SpecifiedNoteScoreboard BreakCount = new();

        public static int HighestCombo => Combo > _highestComboAfterReset ? Combo : _highestComboAfterReset;

        public static LevelRankData GetLevelRankData()
        {
            var result = new LevelRankData();
            result.Combo = HighestCombo;
            result.FcState = GetFcState();
            result.TotalScore = GetTotalScore();
            result.LevelAchievements = new LevelAchievement()
            {
                DxBestAchievement = new LevelAchievement.ScorePair()
                {
                    DxAchievement = GetCurrentAchievement(ResultController.AchievementType.Dx),
                    FinaleAchievement = GetCurrentAchievement(ResultController.AchievementType.Finale),
                    Score = GetScore()
                },
                FinaleBestAchievement = new LevelAchievement.ScorePair()
                {
                    DxAchievement = GetCurrentAchievement(ResultController.AchievementType.Dx),
                    FinaleAchievement = GetCurrentAchievement(ResultController.AchievementType.Finale),
                    Score = GetScore()
                }
            };

            return result;
        }
        
        public static FcState GetFcState()
        {
            var totalCount = SlideCount.TotalCount + HoldCount.TotalCount + TapCount.TotalCount + BreakCount.TotalCount;
            var missCount = SlideCount.MissCount + HoldCount.MissCount + TapCount.MissCount + BreakCount.MissCount;
            var goodCount = SlideCount.GoodCount + HoldCount.GoodCount + TapCount.GoodCount + BreakCount.GoodCount;
            var greatCount = SlideCount.GreatCount + HoldCount.GreatCount + TapCount.GreatCount + BreakCount.GreatCount;

            if (totalCount == 0)
                return FcState.None;

            if (missCount + goodCount + greatCount == 0)
                return FcState.Ap;
            if (missCount + goodCount == 0)
                return FcState.FcGold;
            if (missCount == 0)
                return FcState.Fc;

            return FcState.None;
        }

        public static void Reset()
        {
            SlideCount = new SpecifiedNoteScoreboard();
            HoldCount = new SpecifiedNoteScoreboard();
            TapCount = new SpecifiedNoteScoreboard();
            BreakCount = new SpecifiedNoteScoreboard();
            Combo = 0;
            _highestComboAfterReset = 0;
        }

        public static int GetTotalScore()
        {
            var score = BreakCount.TotalCount * 2600 + TapCount.TotalCount * 500 + HoldCount.TotalCount * 1000 +
                        SlideCount.TotalCount * 1500;

            return score;
        }

        public static int GetScore()
        {
            var score = BreakCount.CriticalPerfectCount * 2600 +
                        BreakCount.SemiCriticalPerfectCount * 2550 + BreakCount.PerfectCount * 2500 +
                        BreakCount.GreatCount * 2000 + BreakCount.SemiGreatCount * 1500 +
                        BreakCount.QuarterGreatCount * 1250 + BreakCount.GoodCount * 1000;

            score +=
                (TapCount.SemiCriticalPerfectCount + TapCount.CriticalPerfectCount + TapCount.PerfectCount) * 500 +
                (TapCount.GreatCount + TapCount.SemiGreatCount + TapCount.QuarterGreatCount) * 400 +
                TapCount.GoodCount * 250;

            score +=
                (HoldCount.SemiCriticalPerfectCount + HoldCount.CriticalPerfectCount + HoldCount.PerfectCount) * 1000 +
                (HoldCount.GreatCount + HoldCount.SemiGreatCount + HoldCount.QuarterGreatCount) * 800
                + HoldCount.GoodCount * 500;

            score += (SlideCount.SemiCriticalPerfectCount + SlideCount.CriticalPerfectCount + SlideCount.PerfectCount) *
                     1500 +
                     (SlideCount.GreatCount + SlideCount.SemiGreatCount + SlideCount.QuarterGreatCount) * 1200
                     + SlideCount.GoodCount * 750;

            return score;
        }

        public static (int deltaBasicScore, int extraScore) GetDeltaScore(ResultController.AchievementType type)
        {
            switch (type)
            {
                case ResultController.AchievementType.Finale:
                    var deductedScore = BreakCount.CriticalPerfectCount * 100 +
                                        BreakCount.SemiCriticalPerfectCount * 50 +
                                        BreakCount.GreatCount * -500 + BreakCount.SemiGreatCount * -1000 +
                                        BreakCount.QuarterGreatCount * -1250 + BreakCount.GoodCount * -1500
                                        + BreakCount.MissCount * -2500;

                    deductedScore +=
                        (TapCount.GreatCount + TapCount.SemiGreatCount + TapCount.QuarterGreatCount) * -100 +
                        TapCount.GoodCount * -250 + TapCount.MissCount * -500;

                    deductedScore += (HoldCount.GreatCount + HoldCount.SemiGreatCount + HoldCount.QuarterGreatCount) *
                                     -200
                                     + HoldCount.GoodCount * -500 + HoldCount.MissCount * -1000;

                    deductedScore +=
                        (SlideCount.GreatCount + SlideCount.SemiGreatCount + SlideCount.QuarterGreatCount) * -300
                        + SlideCount.GoodCount * -750 + SlideCount.MissCount * -1500;

                    return (deductedScore, 0);
                case ResultController.AchievementType.Dx:
                    var deductedBasicScore =
                        BreakCount.GreatCount * -500 + BreakCount.SemiGreatCount * -1000 +
                        BreakCount.QuarterGreatCount * -1250 + BreakCount.GoodCount * -1500
                        + BreakCount.MissCount * -2500;

                    deductedBasicScore +=
                        (TapCount.GreatCount + TapCount.SemiGreatCount + TapCount.QuarterGreatCount) * -100 +
                        TapCount.GoodCount * -250 + TapCount.MissCount * -500;

                    deductedBasicScore +=
                        (HoldCount.GreatCount + HoldCount.SemiGreatCount + HoldCount.QuarterGreatCount) *
                        -200
                        + HoldCount.GoodCount * -500 + HoldCount.MissCount * -1000;

                    deductedBasicScore +=
                        (SlideCount.GreatCount + SlideCount.SemiGreatCount + SlideCount.QuarterGreatCount) * -300
                        + SlideCount.GoodCount * -750 + SlideCount.MissCount * -1500;

                    var extraScore = BreakCount.CriticalPerfectCount * 100 +
                                     BreakCount.SemiCriticalPerfectCount * 75 + BreakCount.PerfectCount * 50
                                     + (BreakCount.GreatCount + BreakCount.SemiGreatCount +
                                        BreakCount.QuarterGreatCount) * 40
                                     + BreakCount.GoodCount * 30;

                    return (deductedBasicScore, extraScore);
            }

            return (0, 0);
        }

        public static int GetHighestExtraScore()
        {
            var score = BreakCount.TotalCount * 100;
            return score;
        }

        public static float GetDeltaAchievement(ResultController.AchievementType type)
        {
            switch (type)
            {
                case ResultController.AchievementType.Finale:
                    return GetDeltaScore(ResultController.AchievementType.Finale).deltaBasicScore /
                        (float)(GetTotalScore() - GetHighestExtraScore()) * 100;
                case ResultController.AchievementType.Dx:
                    var deltaScore = GetDeltaScore(ResultController.AchievementType.Dx);

                    return deltaScore.deltaBasicScore / (float)(GetTotalScore() - GetHighestExtraScore()) * 100 +
                           deltaScore.extraScore / ((float)GetHighestExtraScore() == 0
                               ? 1
                               : (float)GetHighestExtraScore());
            }

            return 0;
        }

        public static float GetCurrentAchievement(ResultController.AchievementType type)
        {
            var currentBasicAchievement = (BreakCount.CurrentCount * 2500 + TapCount.CurrentCount * 500 +
                                           HoldCount.CurrentCount * 1000 + SlideCount.CurrentCount * 1500) /
                (float)(GetTotalScore() - GetHighestExtraScore()) * 100;

            return currentBasicAchievement + GetDeltaAchievement(type);
        }

        public static void ResetCombo()
        {
            if (Combo > _highestComboAfterReset)
                _highestComboAfterReset = Combo;

            Combo = 0;
        }
    }
}