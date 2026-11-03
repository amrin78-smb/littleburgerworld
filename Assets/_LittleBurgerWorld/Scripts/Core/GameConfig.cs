using System.Collections.Generic;
using LittleBurgerWorld.Activities;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Customers;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.Rewards;
using UnityEngine;

namespace LittleBurgerWorld.Core
{
    /// <summary>
    /// Root configuration asset. Lives at Resources/LBW_GameConfig so the game can bootstrap from any scene.
    /// Every list here is data-driven: adding an asset to a list adds it to the game.
    /// </summary>
    [CreateAssetMenu(menuName = "Little Burger World/Game Config", fileName = "LBW_GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        public const string ResourcePath = "LBW_GameConfig";

        [Header("Content")]
        public List<IngredientData> ingredients = new List<IngredientData>();
        public List<RecipeData> recipes = new List<RecipeData>();
        public List<DrinkData> drinks = new List<DrinkData>();
        public List<CharacterData> characters = new List<CharacterData>();
        public List<ActivityData> activities = new List<ActivityData>();
        public List<StickerData> stickers = new List<StickerData>();

        [Header("Services")]
        public AudioLibrary audioLibrary;
        public LocalizationTable localization;

        [Header("Shared visuals")]
        public Sprite friesIcon;
        public Sprite trayIcon;
        public Sprite burgerIcon;
        public Sprite starIcon;
        public Sprite heartIcon;
        public Material sparkleMaterial;
        public Material softParticleMaterial;
        public Material confettiMaterial;
        public Material heartMaterial;
        public Material highlightRingMaterial;
        [Tooltip("Lit material tinted per balloon via _BaseColor.")]
        public Material balloonMaterial;

        [Header("Scenes")]
        public string bootScene = "Boot";
        public string menuScene = "MainMenu";
        public string restaurantScene = "Restaurant";

        [Header("Feel")]
        [Tooltip("Frame rate target on device. 30 is used automatically on low-end devices.")]
        public int targetFrameRate = 60;

        public IngredientData FindIngredient(string id) => ingredients.Find(i => i != null && i.id == id);
        public RecipeData FindRecipe(string id) => recipes.Find(r => r != null && r.id == id);
        public DrinkData FindDrink(string id) => drinks.Find(d => d != null && d.id == id);
        public CharacterData FindCharacter(string id) => characters.Find(c => c != null && c.id == id);
        public ActivityData FindActivity(string id) => activities.Find(a => a != null && a.id == id);
        public StickerData FindSticker(string id) => stickers.Find(s => s != null && s.id == id);

        /// <summary>Icon for a request/item id: ingredient, recipe, drink or one of the generic foods.</summary>
        public Sprite FindIcon(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            RecipeData r = FindRecipe(id);
            if (r != null) return r.icon;
            IngredientData i = FindIngredient(id);
            if (i != null) return i.icon;
            DrinkData d = FindDrink(id);
            if (d != null) return d.icon;
            switch (id)
            {
                case "fries": return friesIcon;
                case "tray":
                case "meal": return trayIcon;
                case "burger": return burgerIcon;
                case "milkshake": return drinks.Count > 0 && drinks[0] != null ? drinks[0].icon : null;
            }
            return null;
        }
    }
}
