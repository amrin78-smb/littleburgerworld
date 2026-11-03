using System.Collections.Generic;
using LittleBurgerWorld.Core;
using UnityEngine;

namespace LittleBurgerWorld.Food
{
    /// <summary>
    /// Creates food objects from data (ingredients, burgers) and keeps track of what exists,
    /// so the kitchen can be tidied gently.
    /// </summary>
    public sealed class FoodManager : MonoBehaviour
    {
        public static FoodManager Instance { get; private set; }

        [SerializeField] private Transform m_FoodRoot;

        public Transform FoodRoot => m_FoodRoot != null ? m_FoodRoot : transform;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Spawns an ingredient prefab configured from its data.</summary>
        public static FoodItem SpawnIngredient(IngredientData data, Vector3 position, bool fromSupply = false)
        {
            if (data == null || data.prefab == null)
            {
                Debug.LogWarning("[FoodManager] Ingredient has no prefab: " + (data != null ? data.id : "null"));
                return null;
            }
            Transform parent = Instance != null ? Instance.FoodRoot : null;
            GameObject go = Instantiate(data.prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
            go.name = "Food_" + data.id;
            FoodItem item = go.GetComponent<FoodItem>();
            if (item == null) item = go.AddComponent<FoodItem>();
            item.Initialize(data);
            if (fromSupply) item.MarkFromSupply();
            item.HomePosition = position;
            return item;
        }

        /// <summary>Spawns an ingredient by id from the active GameConfig.</summary>
        public static FoodItem SpawnIngredient(string id, Vector3 position, bool fromSupply = false)
        {
            GameConfig cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;
            return SpawnIngredient(cfg != null ? cfg.FindIngredient(id) : null, position, fromSupply);
        }

        /// <summary>Wraps stacked ingredients into a single draggable burger.</summary>
        public static FoodItem CreateBurger(List<FoodItem> parts, RecipeData recipe, Vector3 basePosition, float height,
            Transform parent)
        {
            var go = new GameObject(recipe != null ? "Burger_" + recipe.id : "Burger_Custom");
            go.transform.SetParent(parent, false);
            go.transform.position = basePosition;
            go.layer = FoodItem.InteractableLayer;
            FoodItem burger = go.AddComponent<FoodItem>();
            burger.Configure(FoodType.Burger, Mathf.Max(0.3f, height));
            burger.SetRecipe(recipe);
            burger.SetContents(parts);
            float radius = 0.7f;
            burger.EnsurePickable(new Vector3(radius * 2f, Mathf.Max(0.4f, height) + 0.1f, radius * 2f),
                new Vector3(0f, Mathf.Max(0.4f, height) * 0.5f, 0f));
            return burger;
        }

        /// <summary>Gently removes every loose food item (staggered poofs). Customers' food is left alone.</summary>
        public static int VanishAll(System.Predicate<FoodItem> filter = null, float stagger = 0.04f)
        {
            var items = new List<FoodItem>(FoodItem.All);
            int n = 0;
            foreach (FoodItem f in items)
            {
                if (f == null || f.IsConsumed || f.Type == FoodType.Tray) continue;
                if (f.transform.parent != null && f.transform.parent.GetComponentInParent<FoodItem>() != null) continue; // part of something
                if (filter != null && !filter(f)) continue;
                f.Vanish(n * stagger);
                n++;
            }
            return n;
        }
    }
}
