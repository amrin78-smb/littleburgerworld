using UnityEngine;

namespace LittleBurgerWorld.Customers
{
    /// <summary>
    /// Configuration for an animal customer. Add a new animal by creating a prefab with an
    /// <see cref="AnimalCustomerController"/> + <see cref="ProceduralCharacterRig"/> and one of these assets.
    /// </summary>
    [CreateAssetMenu(menuName = "Little Burger World/Character", fileName = "Character_New")]
    public sealed class CharacterData : ScriptableObject
    {
        public string id = "character";
        public string displayName = "Friend";
        public GameObject prefab;
        public Sprite portrait;
        public bool unlockedByDefault = true;
        [Tooltip("Pitch multiplier applied to this character's happy sounds.")]
        public float voicePitch = 1f;
        public float walkSpeed = 1.6f;
    }
}
