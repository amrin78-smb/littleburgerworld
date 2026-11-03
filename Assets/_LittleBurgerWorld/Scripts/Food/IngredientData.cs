using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using UnityEngine;

namespace LittleBurgerWorld.Food
{
    /// <summary>
    /// Configuration for one burger ingredient. Add a new ingredient by creating one of these
    /// (Assets ▸ Create ▸ Little Burger World ▸ Ingredient) and adding it to the GameConfig list.
    /// </summary>
    [CreateAssetMenu(menuName = "Little Burger World/Ingredient", fileName = "Ingredient_New")]
    public sealed class IngredientData : ScriptableObject
    {
        [Tooltip("Unique, stable id used by recipes, activities and save files.")]
        public string id = "ingredient";
        public string displayName = "Ingredient";
        [Tooltip("Prefab with a FoodItem component. Pivot must be at the bottom centre.")]
        public GameObject prefab;
        public Sprite icon;
        [Tooltip("Main colour, used for the tray button and particles.")]
        public Color color = Color.white;
        [Tooltip("Optional material override applied to the prefab's main renderer.")]
        public Material material;

        [Header("Stacking")]
        [Tooltip("Thickness when stacked on a burger (world units).")]
        public float stackHeight = 0.12f;
        [Tooltip("Can be placed on the burger board.")]
        public bool stackable = true;
        [Tooltip("Placing this on top of a stack completes the burger (top bun).")]
        public bool completesBurger;
        [Tooltip("Good first layer (bottom bun).")]
        public bool isBase;

        [Header("Cooking")]
        [Tooltip("If set, this ingredient is raw and turns into the cooked variant on the grill.")]
        public IngredientData cookedVariant;

        [Header("Interaction")]
        [Tooltip("Shown in the bottom ingredient tray.")]
        public bool showInTray = true;
        public bool unlockedByDefault = true;
        [Tooltip("Sorting order in the tray (low = left).")]
        public int trayOrder;
        public SfxId pickSound = SfxId.Pop;
        public SfxId placeSound = SfxId.Snap;

        public bool IsRaw => cookedVariant != null;
    }
}
