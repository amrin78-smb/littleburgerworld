using UnityEngine;

namespace LittleBurgerWorld.Rewards
{
    /// <summary>How a sticker is earned. All conditions reward positive actions only.</summary>
    public enum StickerCondition
    {
        FirstBurger,
        FirstCheeseburger,
        FirstFries,
        FirstMilkshake,
        FirstMeal,
        ServeCustomers,
        TallBurger,
        CompleteActivity,
        CompleteAllActivities,
        PopBalloons
    }

    [CreateAssetMenu(menuName = "Little Burger World/Sticker", fileName = "Sticker_New")]
    public sealed class StickerData : ScriptableObject
    {
        public string id = "sticker";
        public string displayName = "Sticker";
        public Sprite sprite;
        public StickerCondition condition;
        [Tooltip("Count for ServeCustomers / TallBurger / PopBalloons.")]
        public int threshold = 1;
        [Tooltip("Activity id for CompleteActivity.")]
        public string activityId;
    }
}
