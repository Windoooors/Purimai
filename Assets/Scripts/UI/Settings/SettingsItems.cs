using System.Collections.Generic;

namespace UI.Settings
{
    public static class SettingsItems
    {
        public static SettingsCategory[] Settings =
        {
            new()
            {
                Identifier = "song_list",
                Items = new List<SettingsItem>
                {
                    new("song_list.group_rule", 0, new[] { "alphabet", "difficulty" })
                },
                VisibleFromSettingsPanel = false
            },
            new()
            {
                Identifier = "gameplay",
                Items = new List<SettingsItem>
                {
                    new("gameplay.delay", new SuccessiveIntegerValueSet
                    {
                        DefaultValue = 0,
                        ValueUpperLimit = 1000,
                        ValueLowerLimit = -1000
                    }),
                    new("gameplay.judge_delay", new SuccessiveIntegerValueSet
                    {
                        DefaultValue = 0,
                        ValueUpperLimit = 1000,
                        ValueLowerLimit = -1000
                    }),
                    new("gameplay.flow_speed_type", 0, new[] { "astro_dx", "maimai" }),
                    new("gameplay.flow_speed", 26, new[]
                    {
                        "1", "1.25", "1.5", "1.75",
                        "2", "2.25", "2.5", "2.75",
                        "3", "3.25", "3.5", "3.75",
                        "4", "4.25", "4.5", "4.75",
                        "5", "5.25", "5.5", "5.75",
                        "6", "6.25", "6.5", "6.75",
                        "7", "7.25", "7.5", "7.75",
                        "8", "8.25", "8.5", "8.75",
                        "9", "9.25", "9.5", "9.75",
                        "10", "10.25", "10.5", "10.75",
                        "11", "11.25", "11.5", "11.75",
                        "Sonic"
                    }, false),
                    new("gameplay.slide_delay", 10, new[]
                    {
                        "-1", "-0.9", "-0.8", "-0.7", "-0.6",
                        "-0.5", "-0.4", "-0.3", "-0.2", "-0.1", "0", "0.1", "0.2", "0.3", "0.4", "0.5",
                        "0.6", "0.7", "0.8", "0.9", "1"
                    }, false),
                    new("gameplay.sensor_radius", 3, new[]
                    {
                        "1", "1.1", "1.2", "1.3", "1.4",
                        "1.5", "1.6", "1.7", "1.8", "1.9", "2"
                    }, false),
                    new("gameplay.sensor_radius.area.a", 0, new[]
                    {
                        "1", "1.1", "1.2", "1.3", "1.4",
                        "1.5", "1.6", "1.7", "1.8", "1.9", "2"
                    }, false),
                    new("gameplay.sensor_radius.area.b", 6, new[]
                    {
                        "1", "1.1", "1.2", "1.3", "1.4",
                        "1.5", "1.6", "1.7", "1.8", "1.9", "2"
                    }, false),
                    new("gameplay.sensor_radius.area.c", 6, new[]
                    {
                        "1", "1.1", "1.2", "1.3", "1.4",
                        "1.5", "1.6", "1.7", "1.8", "1.9", "2"
                    }, false),

                    new("gameplay.offset_display_level", 0, new[]
                    {
                        "none", "non_perfect", "non_crit_perfect"
                    })
                }
            },
            new()
            {
                Identifier = "scoring_methods",
                Items = new List<SettingsItem>
                {
                    new("scoring_methods.score_indicator_content", 1,
                        new[] { "none", "combo", "achievement", "deducted_achievement", "score", "deducted_score" }),
                    new("scoring_methods.score_indicator_type", 1,
                        new[] { "score", "achievement" }),
                    new("scoring_methods.achievement_type", 1, new[] { "DX", "FiNALE" }, false)
                }
            },
            new()
            {
                Identifier = "graphics",
                Items = new List<SettingsItem>
                {
                    new("graphics.blurred_cover", 0, new[] { "none", "more_blurred", "most_blurred" }),
                    new("graphics.background_brightness", 2,
                        new[] { "darkest", "darker", "dark", "bright", "brighter", "brightest" }),
                    new("graphics.background_video_playback", new BoolValueSet
                    {
                        DefaultValue = true
                    }),
                    new("graphics.framerate_limiter", new BoolValueSet())
                }
            },
            new()
            {
                Identifier = "audio",
                Items = new List<SettingsItem>
                {
                    new("audio.volume.song", 10, new[]
                    {
                        "0", "0.1", "0.2", "0.3", "0.4",
                        "0.5", "0.6", "0.7", "0.8", "0.9", "1"
                    }, false),
                    new("audio.volume.break", 10, new[]
                    {
                        "0", "0.1", "0.2", "0.3", "0.4",
                        "0.5", "0.6", "0.7", "0.8", "0.9", "1"
                    }, false),
                    new("audio.volume.tap", 10, new[]
                    {
                        "0", "0.1", "0.2", "0.3", "0.4",
                        "0.5", "0.6", "0.7", "0.8", "0.9", "1"
                    }, false),
                    new("audio.volume.slide", 10, new[]
                    {
                        "0", "0.1", "0.2", "0.3", "0.4",
                        "0.5", "0.6", "0.7", "0.8", "0.9", "1"
                    }, false),
                    new("audio.volume.critical_sound", 10, new[]
                    {
                        "0", "0.1", "0.2", "0.3", "0.4",
                        "0.5", "0.6", "0.7", "0.8", "0.9", "1"
                    }, false)
                }
            }
        };
    }

    public class SettingsItem
    {
        public readonly string Identifier;

        public readonly bool ManagedValueLocalization;
        public readonly ValueSet ValueSet;

        public SettingsItem(string identifier, int defaultValue, string[] availableValues,
            bool managedValueLocalization = true)
        {
            Identifier = identifier;
            ValueSet = new SeparatedValueSet
            {
                DefaultValueIndex = defaultValue,
                AvailableValues = availableValues
            };
            ManagedValueLocalization = managedValueLocalization;
        }

        public SettingsItem(string identifier, ValueSet valueSet, bool managedValueLocalization = true)
        {
            Identifier = identifier;
            ValueSet = valueSet;
            ManagedValueLocalization = managedValueLocalization;
        }
    }

    public abstract class ValueSet
    {
    }

    public class SuccessiveIntegerValueSet : ValueSet
    {
        public int DefaultValue;
        public int ValueLowerLimit;
        public int ValueUpperLimit;
    }

    public class SeparatedValueSet : ValueSet
    {
        public string[] AvailableValues;
        public int DefaultValueIndex;
    }

    public class BoolValueSet : ValueSet
    {
        public bool DefaultValue;
        public bool Value;
    }

    public class SettingsCategory
    {
        public string Identifier;
        public List<SettingsItem> Items;
        public bool VisibleFromSettingsPanel = true;
    }
}