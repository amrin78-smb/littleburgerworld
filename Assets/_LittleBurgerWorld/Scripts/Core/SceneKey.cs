using System.Collections.Generic;
using UnityEngine;

namespace LittleBurgerWorld.Core
{
    /// <summary>
    /// Gives a scene object (3D or UI) a stable string key so data-driven systems such as the
    /// guided activity engine can find it ("Board", "Grill", "Tray.cheese", "Customer"...).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneKey : MonoBehaviour
    {
        [SerializeField] private string m_Key;

        public string Key => m_Key;

        public void SetKey(string key)
        {
            if (isActiveAndEnabled) SceneObjectRegistry.Unregister(this);
            m_Key = key;
            if (isActiveAndEnabled) SceneObjectRegistry.Register(this);
        }

        private void OnEnable() => SceneObjectRegistry.Register(this);
        private void OnDisable() => SceneObjectRegistry.Unregister(this);
    }

    /// <summary>Lookup table for <see cref="SceneKey"/> objects.</summary>
    public static class SceneObjectRegistry
    {
        private static readonly Dictionary<string, List<SceneKey>> s_Objects = new Dictionary<string, List<SceneKey>>();

        internal static void Register(SceneKey k)
        {
            if (k == null || string.IsNullOrEmpty(k.Key)) return;
            if (!s_Objects.TryGetValue(k.Key, out var list))
            {
                list = new List<SceneKey>(1);
                s_Objects[k.Key] = list;
            }
            if (!list.Contains(k)) list.Add(k);
        }

        internal static void Unregister(SceneKey k)
        {
            if (k == null || string.IsNullOrEmpty(k.Key)) return;
            if (s_Objects.TryGetValue(k.Key, out var list)) list.Remove(k);
        }

        /// <summary>Returns the most recently registered live object with this key, or null.</summary>
        public static Transform Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!s_Objects.TryGetValue(key, out var list)) return null;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null) { list.RemoveAt(i); continue; }
                if (list[i].isActiveAndEnabled) return list[i].transform;
            }
            return null;
        }

        public static T Find<T>(string key) where T : class
        {
            Transform t = Find(key);
            return t == null ? null : t.GetComponentInParent<T>();
        }

        public static string KeyOf(Component c)
        {
            if (c == null) return null;
            var k = c.GetComponentInParent<SceneKey>();
            return k == null ? null : k.Key;
        }
    }
}
