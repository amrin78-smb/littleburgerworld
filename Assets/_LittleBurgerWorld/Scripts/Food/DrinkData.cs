using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using UnityEngine;

namespace LittleBurgerWorld.Food
{
    /// <summary>A milkshake flavour.</summary>
    [CreateAssetMenu(menuName = "Little Burger World/Drink", fileName = "Drink_New")]
    public sealed class DrinkData : ScriptableObject
    {
        public string id = "drink";
        public string displayName = "Milkshake";
        public Sprite icon;
        public Color liquidColor = Color.white;
        public Color creamColor = Color.white;
        public bool unlockedByDefault = true;
    }
}
