using System;
using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using UnityEngine;

namespace LittleBurgerWorld.Activities
{
    /// <summary>Building blocks for guided activities.</summary>
    public enum ActivityStepType
    {
        /// <summary>Gently reset the kitchen work surfaces (start of an activity).</summary>
        PrepareKitchen,
        /// <summary>Bring a customer to the counter showing a picture request.</summary>
        CallCustomer,
        /// <summary>Pop an object into view with a bounce.</summary>
        ShowObject,
        /// <summary>Pulse-highlight an object for a moment.</summary>
        HighlightObject,
        /// <summary>Show the animated hand dragging from source to target once.</summary>
        DemonstrateDrag,
        /// <summary>Wait until the child picks up the given item.</summary>
        WaitForSelection,
        /// <summary>Wait until the given item is dropped on the target (board restricts to this item).</summary>
        WaitForDrop,
        /// <summary>Wait until the target is tapped.</summary>
        WaitForTap,
        /// <summary>Wait for any gameplay event (cooked, filled, served...).</summary>
        WaitForEvent,
        /// <summary>Trigger an animation hook on a target (customer cheer, object hop).</summary>
        PlayAnimation,
        PlayAudio,
        /// <summary>Pause for <c>duration</c> seconds.</summary>
        Wait,
        CompleteActivity
    }

    /// <summary>One step of a guided activity. Interpreted by <see cref="ActivityStepController"/>.</summary>
    [Serializable]
    public sealed class ActivityStep
    {
        public ActivityStepType type;

        [Tooltip("Scene key of the object acted on (see SceneKey). e.g. Board, Grill, Customer, Fryer.Potatoes")]
        public string targetKey;
        [Tooltip("Scene key the hint hand drags from (e.g. Tray.bun_bottom).")]
        public string sourceKey;
        [Tooltip("Item id filter (ingredient id, burger, fries, milkshake...). Empty = any.")]
        public string itemId;
        [Tooltip("Event for WaitForEvent steps.")]
        public GameplayEventType eventType;

        [Tooltip("Picture shown in the top prompt bubble while this step is active.")]
        public Sprite promptIcon;
        [Tooltip("Optional voice line key (AudioLibrary voice table).")]
        public string voiceKey;
        public SfxId sound = SfxId.None;
        [Tooltip("Animation hook name for PlayAnimation (cheer, hop, wave).")]
        public string animation;
        public float duration = 1f;

        [Header("Customer (CallCustomer)")]
        public string characterId;
        [Tooltip("Picture request item ids, e.g. cheeseburger, fries, milkshake.")]
        public List<string> requestItems = new List<string>();

        [Header("Hints")]
        [Tooltip("Seconds of inactivity before the hand hint appears.")]
        public float hintDelay = 2.5f;
        [Tooltip("After this many hint cycles the game gently does the action for the child (0 = never).")]
        public int autoAssistAfterHints = 4;

        public bool IsWaitStep =>
            type == ActivityStepType.WaitForSelection || type == ActivityStepType.WaitForDrop ||
            type == ActivityStepType.WaitForTap || type == ActivityStepType.WaitForEvent;
    }

    /// <summary>
    /// A guided activity described entirely by data. New activities need no code:
    /// create an asset (Assets ▸ Create ▸ Little Burger World ▸ Activity) and list it in the GameConfig.
    /// </summary>
    [CreateAssetMenu(menuName = "Little Burger World/Activity", fileName = "Activity_New")]
    public sealed class ActivityData : ScriptableObject
    {
        public string id = "activity";
        public string displayName = "Activity";
        public Sprite thumbnail;
        public Color cardColor = new Color(1f, 0.85f, 0.5f);
        public List<ActivityStep> steps = new List<ActivityStep>();
        public SfxId completionSound = SfxId.Celebration;
        [Tooltip("Animation played on completion (balloons, confetti).")]
        public string completionAnimation = "balloons";
        public int starReward = 1;
        [Tooltip("Ingredient/character ids unlocked when finished for the first time.")]
        public List<string> unlocks = new List<string>();
    }
}
