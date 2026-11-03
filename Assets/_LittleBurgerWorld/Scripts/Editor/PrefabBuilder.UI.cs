using LittleBurgerWorld.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBurgerWorld.EditorTools
{
    public static partial class PrefabBuilder
    {
        public static void BuildUiPrefabs()
        {
            BuildTrayButton();
            BuildActivityCard();
            BuildStickerSlot();
        }

        private static GameObject UiRoot(string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            ((RectTransform)go.transform).sizeDelta = size;
            return go;
        }

        private static void BuildTrayButton()
        {
            GameObject root = UiRoot("UI_TrayButton", new Vector2(176f, 176f));
            var le = root.AddComponent<LayoutElement>();
            le.preferredWidth = 176f;
            le.preferredHeight = 176f;
            UIFactory.Image("Shadow", root.transform, IconLibrary.Get("ui_circle"), new Color(0.3f, 0.18f, 0.1f, 0.22f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -8f), new Vector2(170f, 170f), false);
            Image bg = UIFactory.Image("Background", root.transform, IconLibrary.Get("ui_circle"), Color.white, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(170f, 170f));
            UIFactory.Image("Rim", bg.transform, IconLibrary.Get("ui_ring"), new Color(1f, 1f, 1f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(170f, 170f), false);
            Image icon = UIFactory.Image("Icon", bg.transform, null, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f),
                new Vector2(142f, 142f), false);
            var b = root.AddComponent<IngredientTrayButton>();
            EditorAssetUtil.Set(b, "m_Icon", icon);
            EditorAssetUtil.Set(b, "m_Background", bg);
            EditorAssetUtil.SavePrefab(root, $"{UiFolder}/UI_TrayButton.prefab");
        }

        private static void BuildActivityCard()
        {
            GameObject root = UiRoot("UI_ActivityCard", new Vector2(400f, 320f));
            Image shadow = UIFactory.Image("Shadow", root.transform, IconLibrary.Get("ui_panel"), new Color(0.3f, 0.18f, 0.1f, 0.25f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(400f, 320f), false, true);
            shadow.pixelsPerUnitMultiplier = 0.35f;
            Image bg = UIFactory.Panel("Background", root.transform, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 320f));
            UIFactory.Image("Window", bg.transform, IconLibrary.Get("ui_panel"), new Color(1f, 1f, 1f, 0.75f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 30f), new Vector2(330f, 210f), false, true).pixelsPerUnitMultiplier = 0.4f;
            Image thumb = UIFactory.Image("Thumbnail", bg.transform, null, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, 32f),
                new Vector2(200f, 200f), false);
            var stars = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                stars[i] = UIFactory.Image("Star" + i, bg.transform, IconLibrary.Get("icon_star_empty"), Color.white, new Vector2(0.5f, 0f),
                    new Vector2(-80f + i * 80f, 44f), new Vector2(76f, 76f), false);
            }
            var button = root.AddComponent<SquishyButton>();
            var card = root.AddComponent<ActivityCard>();
            EditorAssetUtil.Set(card, "m_Background", bg);
            EditorAssetUtil.Set(card, "m_Thumbnail", thumb);
            EditorAssetUtil.SetArray(card, "m_Stars", stars);
            EditorAssetUtil.Set(card, "m_StarOn", IconLibrary.Get("icon_star"));
            EditorAssetUtil.Set(card, "m_StarOff", IconLibrary.Get("icon_star_empty"));
            EditorAssetUtil.Set(card, "m_Button", button);
            EditorAssetUtil.SavePrefab(root, $"{UiFolder}/UI_ActivityCard.prefab");
        }

        private static void BuildStickerSlot()
        {
            GameObject root = UiRoot("UI_StickerSlot", new Vector2(190f, 190f));
            var img = root.AddComponent<Image>();
            img.sprite = IconLibrary.Get("ui_circle");
            img.color = new Color(1f, 0.97f, 0.92f, 1f);
            img.raycastTarget = false;
            UIFactory.Image("Sticker", root.transform, null, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(176f, 176f), false);
            EditorAssetUtil.SavePrefab(root, $"{UiFolder}/UI_StickerSlot.prefab");
        }
    }
}
