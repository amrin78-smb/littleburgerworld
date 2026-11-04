using LittleBurgerWorld.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LittleBurgerWorld.EditorTools
{
    /// <summary>Helpers to build uGUI hierarchies from code (toddler-sized, rounded, colourful).</summary>
    public static class UIFactory
    {
        private static Font s_Font;

        public static Font Font => s_Font != null ? s_Font : (s_Font = AssetDatabase.LoadAssetAtPath<Font>(PrefabBuilder.FontPath));

        public static Canvas Canvas(string name, int order, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920f, 1080f);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = 1f;
            return c;
        }

        public static GameObject EventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            return go;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size,
            Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent, float inset = 0f)
        {
            RectTransform rt = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size,
            bool raycast = true, bool sliced = false)
        {
            RectTransform rt = Rect(name, parent, anchor, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sliced)
            {
                img.type = UnityEngine.UI.Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 0.45f;
            }
            else img.preserveAspect = true;
            return img;
        }

        public static Image FillImage(Image img)
        {
            RectTransform rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            img.preserveAspect = false;
            return img;
        }

        public static Text Text(string name, Transform parent, string text, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 box,
            TextAnchor align = TextAnchor.MiddleCenter, bool outline = true, string locKey = null)
        {
            RectTransform rt = Rect(name, parent, anchor, anchor, pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.35f, 0.22f, 0.15f, 1f);
                o.effectDistance = new Vector2(3f, -3f);
                var sh = rt.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0.35f, 0.22f, 0.15f, 0.6f);
                sh.effectDistance = new Vector2(0f, -6f);
            }
            if (!string.IsNullOrEmpty(locKey))
            {
                var lt = rt.gameObject.AddComponent<LocalizedText>();
                EditorAssetUtil.Set(lt, "m_Key", locKey);
            }
            return t;
        }

        /// <summary>Round picture button with a soft drop shadow.</summary>
        public static SquishyButton RoundButton(string name, Transform parent, Sprite icon, Color bg, Vector2 anchor, Vector2 pos, float size,
            float iconScale = 0.62f)
        {
            RectTransform rt = Rect(name, parent, anchor, anchor, pos, new Vector2(size, size));
            Image(name + "_Shadow", rt, IconLibrary.Get("ui_circle"), new Color(0.3f, 0.18f, 0.1f, 0.25f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -8f), new Vector2(size, size), false);
            Image bgImg = Image("Background", rt, IconLibrary.Get("ui_circle"), bg, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            Image("Icon", bgImg.transform, icon, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * iconScale, size * iconScale), false);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0);
            hit.sprite = IconLibrary.Get("ui_circle");
            return rt.gameObject.AddComponent<SquishyButton>();
        }

        /// <summary>Big pill button with icon + short label (main menu).</summary>
        public static SquishyButton BigButton(string name, Transform parent, Sprite icon, string label, string locKey, Color bg,
            Vector2 anchor, Vector2 pos, Vector2 size)
        {
            RectTransform rt = Rect(name, parent, anchor, anchor, pos, size);
            Image sh = Image("Shadow", rt, IconLibrary.Get("ui_panel"), new Color(0.3f, 0.18f, 0.1f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), size, false, true);
            Image body = Image("Body", rt, IconLibrary.Get("ui_panel_outline"), bg, new Vector2(0.5f, 0.5f), Vector2.zero, size, true, true);
            body.pixelsPerUnitMultiplier = 0.35f;
            sh.pixelsPerUnitMultiplier = 0.35f;
            Image("Shine", body.transform, IconLibrary.Get("ui_panel"), new Color(1, 1, 1, 0.25f), new Vector2(0.5f, 1f),
                new Vector2(0f, -size.y * 0.2f), new Vector2(size.x - 60f, size.y * 0.22f), false, true);
            float iconSize = size.y * 0.78f;
            Image("Icon", body.transform, icon, Color.white, new Vector2(0f, 0.5f), new Vector2(iconSize * 0.5f + 26f, 4f), new Vector2(iconSize, iconSize), false);
            Text("Label", body.transform, label, (int)(size.y * 0.27f), Color.white, new Vector2(0.5f, 0.5f), new Vector2(iconSize * 0.45f, 4f),
                new Vector2(size.x - iconSize - 40f, size.y), TextAnchor.MiddleCenter, true, locKey);
            return rt.gameObject.AddComponent<SquishyButton>();
        }

        public static HoldButton HoldButton(string name, Transform parent, Sprite icon, Color bg, Vector2 anchor, Vector2 pos, float size,
            string tipText = null, string tipLocKey = null, float tipPivotX = 0.5f)
        {
            RectTransform rt = Rect(name, parent, anchor, anchor, pos, new Vector2(size, size));
            Image(name + "_Shadow", rt, IconLibrary.Get("ui_circle"), new Color(0.3f, 0.18f, 0.1f, 0.25f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -8f), new Vector2(size, size), false);
            Image bgImg = Image("Background", rt, IconLibrary.Get("ui_circle"), bg, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            Image("Icon", bgImg.transform, icon, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.6f, size * 0.6f), false);
            Image fill = Image("Fill", rt, IconLibrary.Get("ui_ring"), new Color(1f, 0.85f, 0.3f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(size * 1.18f, size * 1.18f), false);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            fill.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0);
            hit.sprite = IconLibrary.Get("ui_circle");
            var hb = rt.gameObject.AddComponent<HoldButton>();
            EditorAssetUtil.Set(hb, "m_Fill", fill);
            if (!string.IsNullOrEmpty(tipText))
            {
                // Small speech tip under the button, shown after a too-quick tap.
                const float w = 400f, h = 96f;
                float x = (tipPivotX - 0.5f) * -size * 0.6f;
                Image tip = Panel("Tip", rt, new Color(1f, 0.98f, 0.94f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x, -size * 0.62f - 14f), new Vector2(w, h));
                tip.rectTransform.pivot = new Vector2(tipPivotX, 1f);
                tip.raycastTarget = false;
                Text t = Text("Label", tip.transform, tipText, 40, EditorAssetUtil.Hex("#5A3A26"), new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(w - 36f, h - 16f), TextAnchor.MiddleCenter, false, tipLocKey);
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 24;
                t.resizeTextMaxSize = 40;
                tip.gameObject.SetActive(false);
                EditorAssetUtil.Set(hb, "m_Tip", tip.rectTransform);
            }
            return hb;
        }

        public static Image Panel(string name, Transform parent, Color color, Vector2 anchor, Vector2 pos, Vector2 size, bool outline = true)
        {
            Image p = Image(name, parent, IconLibrary.Get(outline ? "ui_panel_outline" : "ui_panel"), color, anchor, pos, size, true, true);
            p.pixelsPerUnitMultiplier = 0.35f;
            return p;
        }

        public static Image Dim(Transform parent, float alpha = 0.45f)
        {
            Image d = Image("Dim", parent, null, new Color(0.25f, 0.15f, 0.1f, alpha), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            FillImage(d);
            return d;
        }
    }
}
