using System;
using UnityEngine;

namespace LittleBurgerWorld.Core
{
    /// <summary>Everything that happens in the kitchen that other systems may care about.</summary>
    public enum GameplayEventType
    {
        None = 0,
        ItemPicked,
        ItemDropped,
        IngredientStacked,
        BurgerCompleted,
        PattyOnGrill,
        PattyFlipped,
        PattyCooked,
        PotatoTapped,
        FriesReady,
        FriesBoxFilled,
        DrinkFlavourChosen,
        CupPlaced,
        DrinkFilled,
        CreamAdded,
        StrawAdded,
        MilkshakeCompleted,
        TrayItemAdded,
        MealReady,
        CustomerArrived,
        CustomerServed,
        CustomerFinishedEating,
        CustomerLeft,
        ObjectTapped,
        WorkSurfaceReset,
        BalloonPopped,
        ConePlaced,
        IceCreamScoop,
        SprinklesAdded,
        CherryAdded,
        IceCreamCompleted,
        DishWashed,
        AllDishesClean,
        ToyCollected,
        ThemeChanged
    }

    /// <summary>Payload for <see cref="GameEvents.Gameplay"/>.</summary>
    public struct GameplayEvent
    {
        public GameplayEventType Type;
        /// <summary>Id of the item involved (ingredient id, "burger", "fries", "milkshake", recipe id...).</summary>
        public string ItemId;
        /// <summary>Scene key of the object that received the action (see <see cref="SceneKey"/>).</summary>
        public string TargetKey;
        /// <summary>Optional extra id (recipe id, drink id, character id).</summary>
        public string ExtraId;
        public Component Source;
        public int Count;

        public GameplayEvent(GameplayEventType type, string itemId = null, string targetKey = null,
            Component source = null, string extraId = null, int count = 0)
        {
            Type = type;
            ItemId = itemId;
            TargetKey = targetKey;
            Source = source;
            ExtraId = extraId;
            Count = count;
        }

        public override string ToString() => $"{Type} item={ItemId} target={TargetKey} extra={ExtraId}";
    }

    /// <summary>
    /// Global, decoupled event channel. Systems raise events here instead of referencing each other,
    /// which keeps the guided-activity engine, rewards and audio independent of the kitchen code.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<GameplayEvent> Gameplay;
        public static event Action<GameMode> ModeChanged;
        public static event Action<string> ActivityStarted;
        public static event Action<string> ActivityCompleted;
        public static event Action<string> StickerEarned;
        public static event Action<string> ItemUnlocked;
        public static event Action LanguageChanged;
        public static event Action SettingsChanged;
        /// <summary>A surprise capsule dropped into the machine.</summary>
        public static event Action ToyCapsuleEarned;

        public static void Raise(GameplayEvent e)
        {
            Invoke(Gameplay, e);
        }

        public static void Raise(GameplayEventType type, string itemId = null, string targetKey = null,
            Component source = null, string extraId = null)
        {
            Raise(new GameplayEvent(type, itemId, targetKey, source, extraId));
        }

        public static void RaiseModeChanged(GameMode mode) => Invoke(ModeChanged, mode);
        public static void RaiseActivityStarted(string id) => Invoke(ActivityStarted, id);
        public static void RaiseActivityCompleted(string id) => Invoke(ActivityCompleted, id);
        public static void RaiseStickerEarned(string id) => Invoke(StickerEarned, id);
        public static void RaiseItemUnlocked(string id) => Invoke(ItemUnlocked, id);
        public static void RaiseLanguageChanged() => Invoke(LanguageChanged);
        public static void RaiseSettingsChanged() => Invoke(SettingsChanged);
        public static void RaiseToyCapsuleEarned() => Invoke(ToyCapsuleEarned);

        /// <summary>Removes every listener (used by tests and when returning to the main menu).</summary>
        public static void ClearAll()
        {
            Gameplay = null;
            ModeChanged = null;
            ActivityStarted = null;
            ActivityCompleted = null;
            StickerEarned = null;
            ItemUnlocked = null;
            LanguageChanged = null;
            SettingsChanged = null;
        }

        // Each listener is isolated: one faulty listener must never break the toddler's game.
        private static void Invoke<T>(Action<T> evt, T arg)
        {
            if (evt == null) return;
            foreach (Delegate d in evt.GetInvocationList())
            {
                try { ((Action<T>)d)(arg); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        private static void Invoke(Action evt)
        {
            if (evt == null) return;
            foreach (Delegate d in evt.GetInvocationList())
            {
                try { ((Action)d)(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }
    }
}
