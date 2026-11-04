using System;
using System.Collections.Generic;

namespace LittleBurgerWorld.Save
{
    [Serializable]
    public sealed class SettingsData
    {
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.9f;
        public float voiceVolume = 1f;
        public string language = "en";
        public bool voicePrompts = true;
        /// <summary>Restaurant mood shown last visit (themes rotate each visit).</summary>
        public int lastTheme = -1;
    }

    [Serializable]
    public sealed class ActivityProgress
    {
        public string id;
        public int stars;
        public int timesCompleted;
    }

    [Serializable]
    public sealed class StatsData
    {
        public int burgersMade;
        public int cheeseburgersMade;
        public int friesMade;
        public int milkshakesMade;
        public int mealsMade;
        public int customersServed;
        public int tallestBurger;
        public int balloonsPopped;
        public int iceCreamsMade;
        public int dishesWashed;
        /// <summary>Ids of the different animals served (for the Animal Friends sticker).</summary>
        public List<string> animalsServed = new List<string>();
    }

    /// <summary>
    /// Everything saved locally. No personal information is ever stored.
    /// Bump <see cref="CurrentVersion"/> and extend <see cref="SaveMigration"/> when the format changes.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public List<ActivityProgress> activities = new List<ActivityProgress>();
        public List<string> unlockedIngredients = new List<string>();
        public List<string> unlockedCharacters = new List<string>();
        public List<string> stickers = new List<string>();
        public SettingsData settings = new SettingsData();
        public StatsData stats = new StatsData();
        /// <summary>Toys found in surprise capsules (Toy Shelf).</summary>
        public List<string> toys = new List<string>();
        /// <summary>Capsules waiting in the machine (capped, free, never bought).</summary>
        public int pendingCapsules;

        public ActivityProgress GetActivity(string id)
        {
            return activities.Find(a => a != null && a.id == id);
        }

        public bool IsActivityCompleted(string id)
        {
            ActivityProgress a = GetActivity(id);
            return a != null && a.timesCompleted > 0;
        }

        public int CompletedActivityCount
        {
            get
            {
                int n = 0;
                foreach (ActivityProgress a in activities)
                {
                    if (a != null && a.timesCompleted > 0) n++;
                }
                return n;
            }
        }

        /// <summary>Repairs nulls that can appear after hand-editing or partial JSON.</summary>
        public void Sanitize()
        {
            activities ??= new List<ActivityProgress>();
            unlockedIngredients ??= new List<string>();
            unlockedCharacters ??= new List<string>();
            stickers ??= new List<string>();
            settings ??= new SettingsData();
            stats ??= new StatsData();
            activities.RemoveAll(a => a == null || string.IsNullOrEmpty(a.id));
            unlockedIngredients.RemoveAll(string.IsNullOrEmpty);
            unlockedCharacters.RemoveAll(string.IsNullOrEmpty);
            stickers.RemoveAll(string.IsNullOrEmpty);
            settings.musicVolume = Clamp01(settings.musicVolume);
            settings.sfxVolume = Clamp01(settings.sfxVolume);
            settings.voiceVolume = Clamp01(settings.voiceVolume);
            if (string.IsNullOrEmpty(settings.language)) settings.language = "en";
        }

        private static float Clamp01(float v) => float.IsNaN(v) ? 1f : Math.Max(0f, Math.Min(1f, v));
    }

    /// <summary>Upgrades older save files step by step.</summary>
    public static class SaveMigration
    {
        public static SaveData Migrate(SaveData data)
        {
            if (data == null) return new SaveData();
            if (data.version < 2)
            {
                // v1 had no stats/settings.voicePrompts. Defaults are fine; just make sure the objects exist.
                data.stats ??= new StatsData();
                data.settings ??= new SettingsData();
                data.settings.voicePrompts = true;
                data.version = 2;
            }
            data.version = SaveData.CurrentVersion;
            return data;
        }
    }
}
