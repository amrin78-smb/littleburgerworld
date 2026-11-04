using System.Collections.Generic;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Save;

namespace LittleBurgerWorld.Rewards
{
    /// <summary>
    /// Lightweight, pressure-free rewards: stats, stickers and unlocks earned through positive play.
    /// Listens to <see cref="GameEvents"/>; never takes anything away.
    /// </summary>
    public sealed class RewardManager
    {
        private readonly GameConfig m_Config;
        private readonly SaveManager m_Save;

        public RewardManager(GameConfig config, SaveManager save)
        {
            m_Config = config;
            m_Save = save;
            GameEvents.Gameplay += OnGameplay;
            GameEvents.ActivityCompleted += OnActivityCompleted;
        }

        public void Dispose()
        {
            GameEvents.Gameplay -= OnGameplay;
            GameEvents.ActivityCompleted -= OnActivityCompleted;
        }

        private SaveData Data => m_Save.Data;

        public bool HasSticker(string id) => Data.stickers.Contains(id);

        // ------------------------------------------------------------------ surprise capsules
        public const int MaxPendingCapsules = 3;

        public int PendingCapsules => Data.pendingCapsules;
        public bool HasToy(string id) => Data.toys != null && Data.toys.Contains(id);
        public int ToyCount => Data.toys != null ? Data.toys.Count : 0;

        /// <summary>Drops a free capsule into the machine (capped so it never piles up).</summary>
        public void EarnCapsule()
        {
            if (Data.pendingCapsules >= MaxPendingCapsules) return;
            Data.pendingCapsules++;
            m_Save.MarkDirty();
            GameEvents.RaiseToyCapsuleEarned();
        }

        /// <summary>Opens a waiting capsule: a toy not found yet when possible. Returns its id (null if none waiting).</summary>
        public string OpenCapsule()
        {
            if (Data.pendingCapsules <= 0 || m_Config == null || m_Config.toys.Count == 0) return null;
            Data.pendingCapsules--;
            if (Data.toys == null) Data.toys = new List<string>();
            var fresh = new List<string>();
            foreach (var t in m_Config.toys) if (t != null && !Data.toys.Contains(t.name)) fresh.Add(t.name);
            string id;
            if (fresh.Count > 0) id = fresh[UnityEngine.Random.Range(0, fresh.Count)];
            else id = m_Config.toys[UnityEngine.Random.Range(0, m_Config.toys.Count)].name;
            if (!Data.toys.Contains(id)) Data.toys.Add(id);
            m_Save.MarkDirty();
            GameEvents.Raise(new GameplayEvent(GameplayEventType.ToyCollected, id));
            return id;
        }

        public bool IsIngredientUnlocked(Food.IngredientData ing)
        {
            if (ing == null) return false;
            return ing.unlockedByDefault || Data.unlockedIngredients.Contains(ing.id);
        }

        public bool IsCharacterUnlocked(Customers.CharacterData c)
        {
            if (c == null) return false;
            return c.unlockedByDefault || Data.unlockedCharacters.Contains(c.id);
        }

        public bool Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || m_Config == null) return false;
            if (m_Config.FindIngredient(id) != null)
            {
                if (Data.unlockedIngredients.Contains(id)) return false;
                Data.unlockedIngredients.Add(id);
            }
            else if (m_Config.FindCharacter(id) != null)
            {
                if (Data.unlockedCharacters.Contains(id)) return false;
                Data.unlockedCharacters.Add(id);
            }
            else return false;

            m_Save.MarkDirty();
            GameEvents.RaiseItemUnlocked(id);
            return true;
        }

        private void OnGameplay(GameplayEvent e)
        {
            StatsData s = Data.stats;
            switch (e.Type)
            {
                case GameplayEventType.BurgerCompleted:
                    s.burgersMade++;
                    if (e.ExtraId == "cheeseburger") s.cheeseburgersMade++;
                    if (e.Count > s.tallestBurger) s.tallestBurger = e.Count;
                    break;
                case GameplayEventType.FriesBoxFilled: s.friesMade++; break;
                case GameplayEventType.MilkshakeCompleted: s.milkshakesMade++; break;
                case GameplayEventType.MealReady: s.mealsMade++; break;
                case GameplayEventType.CustomerServed:
                    s.customersServed++;
                    EarnCapsule();
                    if (s.animalsServed == null) s.animalsServed = new System.Collections.Generic.List<string>();
                    if (!string.IsNullOrEmpty(e.ExtraId) && !s.animalsServed.Contains(e.ExtraId)) s.animalsServed.Add(e.ExtraId);
                    break;
                case GameplayEventType.BalloonPopped: s.balloonsPopped++; break;
                case GameplayEventType.IceCreamCompleted: s.iceCreamsMade++; break;
                case GameplayEventType.DishWashed: s.dishesWashed++; break;
                case GameplayEventType.ToyCollected: break;
                default: return;
            }
            m_Save.MarkDirty();
            EvaluateStickers();
        }

        private void OnActivityCompleted(string id)
        {
            EarnCapsule();
            EvaluateStickers();
            if (m_Config == null) return;
            Activities.ActivityData a = m_Config.FindActivity(id);
            if (a == null) return;
            foreach (string unlock in a.unlocks) Unlock(unlock);
        }

        /// <summary>Awards any sticker whose condition is met. Returns newly earned ids.</summary>
        public List<string> EvaluateStickers()
        {
            var earned = new List<string>();
            if (m_Config == null) return earned;
            foreach (StickerData st in m_Config.stickers)
            {
                if (st == null || HasSticker(st.id)) continue;
                if (!IsMet(st)) continue;
                Data.stickers.Add(st.id);
                earned.Add(st.id);
            }
            if (earned.Count > 0)
            {
                m_Save.MarkDirty();
                foreach (string id in earned) GameEvents.RaiseStickerEarned(id);
            }
            return earned;
        }

        private bool IsMet(StickerData st)
        {
            StatsData s = Data.stats;
            switch (st.condition)
            {
                case StickerCondition.FirstBurger: return s.burgersMade >= 1;
                case StickerCondition.FirstCheeseburger: return s.cheeseburgersMade >= 1;
                case StickerCondition.FirstFries: return s.friesMade >= 1;
                case StickerCondition.FirstMilkshake: return s.milkshakesMade >= 1;
                case StickerCondition.FirstMeal: return s.mealsMade >= 1;
                case StickerCondition.ServeCustomers: return s.customersServed >= st.threshold;
                case StickerCondition.TallBurger: return s.tallestBurger >= st.threshold;
                case StickerCondition.PopBalloons: return s.balloonsPopped >= st.threshold;
                case StickerCondition.FirstIceCream: return s.iceCreamsMade >= st.threshold;
                case StickerCondition.WashDishes: return s.dishesWashed >= st.threshold;
                case StickerCondition.CollectToys: return ToyCount >= st.threshold;
                case StickerCondition.ServeDifferentAnimals: return s.animalsServed != null && s.animalsServed.Count >= st.threshold;
                case StickerCondition.CompleteActivity: return Data.IsActivityCompleted(st.activityId);
                case StickerCondition.CompleteAllActivities:
                    foreach (Activities.ActivityData a in m_Config.activities)
                    {
                        if (a != null && !Data.IsActivityCompleted(a.id)) return false;
                    }
                    return m_Config.activities.Count > 0;
            }
            return false;
        }

        /// <summary>Records a finished activity and returns the stars now held for it.</summary>
        public int RecordActivityCompleted(Activities.ActivityData activity)
        {
            if (activity == null) return 0;
            ActivityProgress p = Data.GetActivity(activity.id);
            if (p == null)
            {
                p = new ActivityProgress { id = activity.id };
                Data.activities.Add(p);
            }
            p.timesCompleted++;
            p.stars = System.Math.Min(3, p.stars + System.Math.Max(1, activity.starReward));
            m_Save.MarkDirty();
            return p.stars;
        }
    }
}
