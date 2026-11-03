using System.Collections.Generic;

namespace LittleBurgerWorld.Food
{
    /// <summary>
    /// Matches assembled ingredient stacks against <see cref="RecipeData"/> assets.
    /// Recipes are pure data; the assembly controller never hardcodes them.
    /// </summary>
    public sealed class RecipeManager
    {
        private readonly List<RecipeData> m_Recipes = new List<RecipeData>();

        public IReadOnlyList<RecipeData> Recipes => m_Recipes;

        public RecipeManager(IEnumerable<RecipeData> recipes)
        {
            if (recipes == null) return;
            foreach (RecipeData r in recipes)
            {
                if (r != null) m_Recipes.Add(r);
            }
        }

        /// <summary>Exact match (same ingredients, same order, bottom to top). Returns null for a custom burger.</summary>
        public RecipeData Match(IReadOnlyList<IngredientData> stack)
        {
            if (stack == null || stack.Count == 0) return null;
            foreach (RecipeData recipe in m_Recipes)
            {
                if (recipe.ingredients == null || recipe.ingredients.Count != stack.Count) continue;
                bool same = true;
                for (int i = 0; i < stack.Count; i++)
                {
                    if (!SameIngredient(recipe.ingredients[i], stack[i])) { same = false; break; }
                }
                if (same) return recipe;
            }
            return null;
        }

        /// <summary>
        /// Which ingredient comes next if the child follows <paramref name="recipe"/>; null when done.
        /// Used by guided play to highlight the next ingredient.
        /// </summary>
        public static IngredientData NextIngredient(RecipeData recipe, IReadOnlyList<IngredientData> stack)
        {
            if (recipe == null) return null;
            int n = stack?.Count ?? 0;
            for (int i = 0; i < n && i < recipe.ingredients.Count; i++)
            {
                if (!SameIngredient(recipe.ingredients[i], stack[i])) return null; // off-recipe: no suggestion
            }
            return n < recipe.ingredients.Count ? recipe.ingredients[n] : null;
        }

        public static bool SameIngredient(IngredientData a, IngredientData b)
        {
            if (a == null || b == null) return a == b;
            return a == b || a.id == b.id;
        }
    }
}
