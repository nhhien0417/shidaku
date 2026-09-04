using System.Collections.Generic;

namespace Analytics.Events
{
    public class LevelStartEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.LevelStart;

        public int Level { get; set; }
        public string PlayMode { get; set; }
        public string BaseMode { get; set; }
        public int Heart { get; set; }
        public string HardLabel { get; set; }
        public int RemainingCoins { get; set; }
        public int AttemptNum { get; set; }
        public string PuzzleId { get; set; }
        public string InternetConnection { get; set; }

        public bool DdaEnabled { get; set; }
        public string Difficulty { get; set; }
        public string BaseDifficulty { get; set; }
        public string UserDifficulty { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "level", Level },
                { "play_mode", PlayMode },
                { "base_mode", BaseMode },
                { "heart", Heart },
                { "hard_label", HardLabel },
                { "remaining_coins", RemainingCoins },
                { "attempt_num", AttemptNum },
                { "puzzle_id", PuzzleId },
                { "internet_connection", InternetConnection },
                { "dda_enabled", DdaEnabled },
                { "difficulty", Difficulty },
                { "base_difficulty", BaseDifficulty },
                { "user_difficulty", UserDifficulty }
            };
        }
    }

    public class LevelEndEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.LevelEnd;
        public int Level { get; set; }
        public string PlayMode { get; set; }
        public string BaseMode { get; set; }
        public bool Success { get; set; }
        public string FailReason { get; set; }
        public float PercentCompleted { get; set; }
        public int AttemptNum { get; set; }
        public string PuzzleId { get; set; }
        public int HeartRemain { get; set; }
        public string HardLabel { get; set; }
        public float Duration { get; set; }
        public int BoosterQueenUsed { get; set; }
        public int BoosterHintUsed { get; set; }
        public int BoosterRandomMarkUsed { get; set; }
        public int CoinSpend { get; set; }
        public int InterAdWatched { get; set; }
        public int RvAdWatched { get; set; }
        public float AdDuration { get; set; }
        public double AdRevenue { get; set; }

        public bool DdaEnabled { get; set; }
        public string Difficulty { get; set; }
        public string BaseDifficulty { get; set; }
        public string UserDifficulty { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            var parameters = new Dictionary<string, object>
            {
                { "level", Level },
                { "play_mode", PlayMode },
                { "base_mode", BaseMode },
                { "success", Success },
                { "percent_completed", PercentCompleted },
                { "attempt_num", AttemptNum },
                { "puzzle_id", PuzzleId },
                { "heart_remain", HeartRemain },
                { "hard_label", HardLabel },
                { "playing_time", Duration },
                { "booster_queen_used", BoosterQueenUsed },
                { "booster_hint_used", BoosterHintUsed },
                { "booster_random_mark_used", BoosterRandomMarkUsed },
                { "coin_spend", CoinSpend },
                { "inter_ad_watched", InterAdWatched },
                { "rv_ad_watched", RvAdWatched },
                { "ad_duration", AdDuration },
                { "ad_revenue", AdRevenue },
                { "dda_enabled", DdaEnabled },
                { "difficulty", Difficulty },
                { "base_difficulty", BaseDifficulty },
                { "user_difficulty", UserDifficulty }
            };

            if (!Success && !string.IsNullOrEmpty(FailReason))
            {
                parameters["fail_reason"] = FailReason;
            }

            return parameters;
        }
    }

    public class LevelFailEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.LevelFail;

        public int Level { get; set; }
        public string PlayMode { get; set; }
        public string BaseMode { get; set; }
        public int Move { get; set; }
        public string HardLabel { get; set; }
        public float Duration { get; set; }
        public string FailReason { get; set; }
        public string InternetConnection { get; set; }

        public bool DdaEnabled { get; set; }
        public string Difficulty { get; set; }
        public string BaseDifficulty { get; set; }
        public string UserDifficulty { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "level", Level },
                { "play_mode", PlayMode },
                { "base_mode", BaseMode },
                { "move", Move },
                { "hard_label", HardLabel },
                { "playing_time", Duration },
                { "fail_reason", FailReason },
                { "internet_connection", InternetConnection },
                { "dda_enabled", DdaEnabled },
                { "difficulty", Difficulty },
                { "base_difficulty", BaseDifficulty },
                { "user_difficulty", UserDifficulty }
            };
        }
    }

    public class LevelContinueEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.LevelContinue;

        public int Level { get; set; }
        public string PlayMode { get; set; }
        public string BaseMode { get; set; }
        public float Duration { get; set; }
        public string HardLabel { get; set; }
        public string FailReason { get; set; }

        public bool DdaEnabled { get; set; }
        public string Difficulty { get; set; }
        public string BaseDifficulty { get; set; }
        public string UserDifficulty { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "level", Level },
                { "play_mode", PlayMode },
                { "base_mode", BaseMode },
                { "hard_label", HardLabel },
                { "playing_time", Duration },
                { "fail_reason", FailReason },
                { "dda_enabled", DdaEnabled },
                { "difficulty", Difficulty },
                { "base_difficulty", BaseDifficulty },
                { "user_difficulty", UserDifficulty }
            };
        }
    }

    public class CheckpointLevelEvent : AnalyticsEvent
    {
        public override string EventName => EventNames.CheckpointLevel;

        public int Level { get; set; }
        public string PlayMode { get; set; }
        public string BaseMode { get; set; }
        public int AttemptNum { get; set; }
        public string PuzzleId { get; set; }
        public int HeartRemain { get; set; }
        public string HardLabel { get; set; }
        public float Duration { get; set; }
        public int BoosterQueenUsed { get; set; }
        public int BoosterHintUsed { get; set; }
        public int BoosterRandomMarkUsed { get; set; }
        public int CoinSpend { get; set; }
        public int InterAdWatched { get; set; }
        public int RvAdWatched { get; set; }

        public bool DdaEnabled { get; set; }
        public string Difficulty { get; set; }
        public string BaseDifficulty { get; set; }
        public string UserDifficulty { get; set; }

        public override Dictionary<string, object> ToParameters()
        {
            return new Dictionary<string, object>
            {
                { "level", Level },
                { "play_mode", PlayMode },
                { "base_mode", BaseMode },
                { "attempt_num", AttemptNum },
                { "puzzle_id", PuzzleId },
                { "heart_remain", HeartRemain },
                { "hard_label", HardLabel },
                { "playing_time", Duration },
                { "booster_queen_used", BoosterQueenUsed },
                { "booster_hint_used", BoosterHintUsed },
                { "booster_random_mark_used", BoosterRandomMarkUsed },
                { "coin_spend", CoinSpend },
                { "inter_ad_watched", InterAdWatched },
                { "rv_ad_watched", RvAdWatched },
                { "dda_enabled", DdaEnabled },
                { "difficulty", Difficulty },
                { "base_difficulty", BaseDifficulty },
                { "user_difficulty", UserDifficulty }
            };
        }
    }
}
