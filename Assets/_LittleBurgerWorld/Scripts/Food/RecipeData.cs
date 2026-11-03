using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using UnityEngine;

namespace LittleBurgerWorld.Food
{
    /// <summary>A named burger made from an ordered list of ingredients.</summary>
    [CreateAssetMenu(menuName = "Little Burger World/Recipe", fileName = "Recipe_New")]
    public sealed class RecipeData : ScriptableObject
    {
        public string id = "recipe";
        public string displayName = "Burger";
        public Sprite icon;
        [Tooltip("Bottom to top.")]
        public List<IngredientData> ingredients = new List<IngredientData>();
        [Tooltip("Optional prefab spawned instead of keeping the assembled stack.")]
        public GameObject finalFoodPrefab;
        public SfxId completionSound = SfxId.Jingle;
        [Tooltip("Name of the completion animation hook (\"hop\", \"spin\").")]
        public string completionAnimation = "spin";
    }
}
