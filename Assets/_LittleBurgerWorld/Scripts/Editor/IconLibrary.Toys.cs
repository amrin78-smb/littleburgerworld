using System.Collections.Generic;
using UnityEngine;

namespace LittleBurgerWorld.EditorTools
{
    /// <summary>Surprise-capsule toys (original, simple and friendly), plus washing-up and capsule UI art.</summary>
    public static partial class IconLibrary
    {
        /// <summary>Toy ids in shelf order. Each is drawn as "toy_&lt;id&gt;".</summary>
        public static readonly string[] ToyIds =
        {
            "duck", "ball", "car", "rocket", "robot", "teddy", "boat", "train", "kite", "drum",
            "blocks", "wand", "top", "dino", "fish", "plane", "crown", "rainbow", "snail", "butterfly"
        };

        private static void RegisterToysAndMinigames()
        {
            Def("toy_duck", DrawToyDuck);
            Def("toy_ball", c =>
            {
                c.FillOutlined(Shape.Circle(128, 124, 92), Hex("#FF6F91"));
                c.Fill(Shape.Circle(128, 124, 92).Intersect(Shape.RoundRect(128, 124, 120, 24, 0, 30)), White);
                c.Fill(Shape.Circle(128, 124, 92).Intersect(Shape.RoundRect(128, 124, 24, 120, 0, 30)), Hex("#FFD23F"));
                c.Fill(Shape.Ellipse(96, 160, 24, 14, 30), new Color(1, 1, 1, 0.5f));
            });
            Def("toy_car", c =>
            {
                c.FillOutlined(Shape.RoundRect(128, 132, 70, 34, 24).Union(Shape.RoundRect(118, 98, 104, 30, 18)), Hex("#E96A52"));
                c.Fill(Shape.RoundRect(104, 136, 22, 18, 8), Hex("#CFE8FA"));
                c.Fill(Shape.RoundRect(152, 136, 22, 18, 8), Hex("#CFE8FA"));
                foreach (float x in new[] { 70f, 170f }) { c.FillOutlined(Shape.Circle(x, 66, 26), Dark); c.Fill(Shape.Circle(x, 66, 11), Hex("#BFC8D6")); }
                c.Fill(Shape.Circle(214, 102, 9), Hex("#FFD23F"));
            });
            Def("toy_rocket", c =>
            {
                c.FillOutlined(Shape.Polygon(new Vector2(78, 60), new Vector2(104, 112), new Vector2(104, 60)).Round(4), Hex("#E96A52"));
                c.FillOutlined(Shape.Polygon(new Vector2(178, 60), new Vector2(152, 112), new Vector2(152, 60)).Round(4), Hex("#E96A52"));
                c.FillOutlined(Shape.Ellipse(128, 132, 40, 98), White);
                c.FillOutlined(Shape.Circle(128, 150, 20), Hex("#83B6E8"), 5f);
                c.Fill(Shape.Ellipse(128, 214, 26, 18).Intersect(Shape.Ellipse(128, 132, 40, 98)), Hex("#E96A52"));
                c.Fill(Shape.Ellipse(128, 30, 18, 26), Hex("#FFB627"));
                c.Fill(Shape.Ellipse(128, 34, 9, 14), Hex("#FFE7A0"));
            });
            Def("toy_robot", c =>
            {
                c.Stroke(Shape.Capsule(128, 200, 128, 228, 0.5f), 6f, Dark);
                c.FillOutlined(Shape.Circle(128, 232, 12), Hex("#E96A52"));
                c.FillOutlined(Shape.RoundRect(128, 160, 62, 44, 20), Hex("#9FB4D6"));
                c.Fill(Shape.Circle(104, 164, 14), White); c.Fill(Shape.Circle(152, 164, 14), White);
                c.Fill(Shape.Circle(104, 164, 7), Dark); c.Fill(Shape.Circle(152, 164, 7), Dark);
                c.Fill(Shape.RoundRect(128, 138, 22, 5, 3), Dark);
                c.FillOutlined(Shape.RoundRect(128, 76, 50, 40, 14), Hex("#83B6E8"));
                c.Fill(Shape.Heart(128, 76, 26), Hex("#FF6F91"));
            });
            Def("toy_teddy", c =>
            {
                c.FillOutlined(Shape.Ellipse(128, 76, 62, 58), Hex("#C98B5A"));
                c.Fill(Shape.Ellipse(128, 70, 34, 34), Hex("#EBC9A2"));
                c.FillOutlined(Shape.Circle(80, 196, 24), Hex("#C98B5A"));
                c.FillOutlined(Shape.Circle(176, 196, 24), Hex("#C98B5A"));
                c.FillOutlined(Shape.Circle(128, 160, 58), Hex("#C98B5A"));
                c.Fill(Shape.Ellipse(128, 142, 24, 17), Hex("#EBC9A2"));
                c.Fill(Shape.Circle(106, 172, 7), Dark); c.Fill(Shape.Circle(150, 172, 7), Dark);
                c.Fill(Shape.Ellipse(128, 148, 9, 6), Dark);
                c.FillOutlined(Shape.Union(Shape.Ellipse(112, 108, 16, 10, 20), Shape.Ellipse(144, 108, 16, 10, -20)), Hex("#83B6E8"), 4f);
            });
            Def("toy_boat", c =>
            {
                c.FillOutlined(Shape.Polygon(new Vector2(132, 210), new Vector2(132, 96), new Vector2(200, 104)).Round(4), White);
                c.FillOutlined(Shape.Polygon(new Vector2(124, 196), new Vector2(124, 104), new Vector2(72, 110)).Round(4), Hex("#FFD23F"));
                c.Stroke(Shape.Capsule(128, 90, 128, 214, 0.5f), 6f, Dark);
                c.FillOutlined(Shape.Polygon(new Vector2(40, 92), new Vector2(216, 92), new Vector2(186, 46), new Vector2(70, 46)).Round(8), Hex("#E96A52"));
                c.Fill(Shape.Capsule(30, 34, 226, 34, 6), Hex("#83B6E8"));
            });
            Def("toy_train", c =>
            {
                c.FillOutlined(Shape.RoundRect(90, 126, 56, 54, 12), Hex("#79BFA0"));
                c.FillOutlined(Shape.RoundRect(170, 108, 40, 36, 10), Hex("#E96A52"));
                c.FillOutlined(Shape.RoundRect(176, 162, 14, 22, 5), Dark);
                c.Fill(Shape.RoundRect(90, 140, 30, 20, 8), Hex("#CFE8FA"));
                foreach (float x in new[] { 60f, 116f, 176f }) { c.FillOutlined(Shape.Circle(x, 62, 20), Dark); c.Fill(Shape.Circle(x, 62, 8), Hex("#FFD23F")); }
                c.Fill(Shape.Circle(196, 196, 12), new Color(1, 1, 1, 0.8f)); c.Fill(Shape.Circle(214, 214, 16), new Color(1, 1, 1, 0.7f));
            });
            Def("toy_kite", c =>
            {
                Shape kite = Shape.Polygon(new Vector2(128, 236), new Vector2(196, 150), new Vector2(128, 70), new Vector2(60, 150)).Round(6);
                c.FillOutlined(kite, Hex("#FF6F91"));
                c.Fill(kite.Intersect(Shape.RoundRect(160, 190, 40, 50, 0)), Hex("#FFD23F"));
                c.Fill(kite.Intersect(Shape.RoundRect(96, 110, 40, 50, 0)), Hex("#83B6E8"));
                c.Stroke(Shape.Capsule(128, 70, 150, 20, 0.5f), 4f, Dark);
                foreach (var (x, y) in new[] { (136f, 50f), (146f, 30f) }) c.FillOutlined(Shape.Union(Shape.Ellipse(x - 9, y, 9, 6, 20), Shape.Ellipse(x + 9, y, 9, 6, -20)), Hex("#79BFA0"), 3f);
            });
            Def("toy_drum", c =>
            {
                c.FillOutlined(Shape.RoundRect(128, 108, 82, 58, 16), Hex("#E96A52"));
                for (int i = 0; i < 5; i++) c.Stroke(Shape.Capsule(60 + i * 34, 60, 77 + i * 34, 156, 0.5f), 5f, Hex("#FFD23F"));
                c.FillOutlined(Shape.Ellipse(128, 164, 84, 24), Hex("#FFF6E8"));
                c.Stroke(Shape.Capsule(150, 186, 210, 236, 0.5f), 8f, Hex("#C98B5A"));
                c.FillOutlined(Shape.Circle(212, 238, 12), Hex("#FFD23F"));
            });
            Def("toy_blocks", c =>
            {
                (float x, float y, string col, string letter)[] b = { (82, 72, "#83B6E8", "A"), (174, 72, "#79BFA0", "B"), (128, 166, "#FF6F91", "C") };
                foreach (var (x, y, col, _) in b)
                {
                    c.FillOutlined(Shape.RoundRect(x, y, 44, 44, 10), Hex(col));
                    c.Fill(Shape.RoundRect(x, y, 30, 30, 8), new Color(1, 1, 1, 0.35f));
                }
                c.Fill(Shape.Star(82, 72, 20, 9, 5), White); c.Fill(Shape.Heart(174, 72, 22), White); c.Fill(Shape.Circle(128, 166, 16), White);
            });
            Def("toy_wand", c =>
            {
                c.Stroke(Shape.Capsule(60, 40, 140, 140, 0.5f), 12f, Hex("#B9A6E8"));
                c.FillOutlined(Shape.Star(160, 166, 70, 30, 5).Round(6), Hex("#FFD23F"));
                c.Fill(Shape.Star(212, 220, 14, 6, 4), Hex("#FF6F91")); c.Fill(Shape.Star(92, 210, 10, 4, 4), Hex("#83B6E8"));
            });
            Def("toy_top", c =>
            {
                c.Stroke(Shape.Capsule(128, 190, 128, 230, 0.5f), 10f, Hex("#C98B5A"));
                c.FillOutlined(Shape.Polygon(new Vector2(40, 150), new Vector2(216, 150), new Vector2(128, 30)).Round(14).Union(Shape.Ellipse(128, 160, 88, 30)), Hex("#79BFA0"));
                c.Fill(Shape.Ellipse(128, 150, 88, 12), Hex("#FFD23F"));
                c.Fill(Shape.Ellipse(128, 120, 54, 9), Hex("#FF6F91"));
            });
            Def("toy_dino", c =>
            {
                Color g = Hex("#7BD389");
                c.FillOutlined(Shape.Union(Shape.Ellipse(118, 104, 74, 50), Shape.Capsule(170, 120, 196, 196, 26), Shape.Capsule(60, 100, 18, 150, 14)), g);
                foreach (var (x, y) in new[] { (90f, 150f), (118f, 158f), (146f, 152f) }) c.FillOutlined(Shape.Polygon(new Vector2(x - 14, y - 6), new Vector2(x + 14, y - 6), new Vector2(x, y + 18)).Round(3), Hex("#FFB627"), 4f);
                c.FillOutlined(Shape.RoundRect(92, 52, 16, 22, 8), g); c.FillOutlined(Shape.RoundRect(146, 52, 16, 22, 8), g);
                c.Fill(Shape.Circle(200, 196, 8), Dark);
                c.Fill(Shape.Ellipse(118, 96, 46, 26), Hex("#BFF0C4"));
            });
            Def("toy_fish", c =>
            {
                c.FillOutlined(Shape.Polygon(new Vector2(52, 128), new Vector2(14, 176), new Vector2(14, 80)).Round(8), Hex("#FFB627"));
                c.FillOutlined(Shape.Ellipse(132, 128, 88, 64), Hex("#FF8F5A"));
                c.Fill(Shape.Ellipse(118, 128, 14, 52), Hex("#FFD23F")); c.Fill(Shape.Ellipse(90, 128, 12, 44), Hex("#FFD23F"));
                c.Fill(Shape.Circle(180, 142, 13), White); c.Fill(Shape.Circle(183, 142, 7), Dark);
                c.Fill(Shape.Circle(226, 180, 10), new Color(0.75f, 0.9f, 1f, 0.9f)); c.Fill(Shape.Circle(236, 210, 7), new Color(0.75f, 0.9f, 1f, 0.9f));
            });
            Def("toy_plane", c =>
            {
                c.FillOutlined(Shape.RoundRect(128, 140, 98, 26, 24), Hex("#83B6E8"));
                c.FillOutlined(Shape.Polygon(new Vector2(110, 150), new Vector2(150, 150), new Vector2(120, 226), new Vector2(98, 226)).Round(8), Hex("#FFD23F"));
                c.FillOutlined(Shape.Polygon(new Vector2(110, 130), new Vector2(150, 130), new Vector2(120, 56), new Vector2(98, 56)).Round(8), Hex("#FFD23F"));
                c.FillOutlined(Shape.Polygon(new Vector2(36, 140), new Vector2(60, 140), new Vector2(42, 186)).Round(5), Hex("#E96A52"));
                for (int i = 0; i < 3; i++) c.Fill(Shape.Circle(150 + i * 24, 146, 7), White);
                c.Fill(Shape.Circle(226, 140, 9), Dark);
            });
            Def("toy_crown", c =>
            {
                c.FillOutlined(Shape.Polygon(new Vector2(40, 70), new Vector2(216, 70), new Vector2(226, 190), new Vector2(172, 136), new Vector2(128, 206), new Vector2(84, 136), new Vector2(30, 190)).Round(8),
                    Hex("#FFD23F"));
                c.Fill(Shape.RoundRect(128, 82, 86, 10, 5), Hex("#FFB627"));
                c.Fill(Shape.Circle(128, 120, 14), Hex("#FF6F91")); c.Fill(Shape.Circle(84, 112, 10), Hex("#83B6E8")); c.Fill(Shape.Circle(172, 112, 10), Hex("#79BFA0"));
                foreach (var (x, y) in new[] { (30f, 196f), (128f, 212f), (226f, 196f) }) c.FillOutlined(Shape.Circle(x, y, 11), Hex("#FFF6E8"), 4f);
            });
            Def("toy_rainbow", c =>
            {
                string[] cols = { "#FF6F91", "#FFB627", "#FFD23F", "#7BD389", "#83B6E8", "#B9A6E8" };
                Shape upper = Shape.RoundRect(128, 168, 130, 90, 0);
                for (int i = 0; i < cols.Length; i++) c.Fill(Shape.Ring(128, 78, 96 - i * 13, 13).Intersect(upper), Hex(cols[i]));
                c.Stroke(Shape.Ring(128, 78, 96, 84).Intersect(upper), 3f, Canvas2D.Outline);
                foreach (float x in new[] { 52f, 204f }) c.FillOutlined(Shape.Union(Shape.Circle(x - 20, 74, 22), Shape.Circle(x + 6, 84, 28), Shape.Circle(x + 30, 72, 20)), White);
            });
            Def("toy_snail", c =>
            {
                c.FillOutlined(Shape.Union(Shape.Capsule(40, 74, 200, 74, 22), Shape.Capsule(196, 74, 214, 150, 18)), Hex("#FFE0A3"));
                c.Stroke(Shape.Capsule(206, 150, 196, 196, 0.5f), 4f, Dark); c.Stroke(Shape.Capsule(218, 150, 230, 192, 0.5f), 4f, Dark);
                c.Fill(Shape.Circle(196, 198, 7), Dark); c.Fill(Shape.Circle(231, 194, 7), Dark);
                c.FillOutlined(Shape.Circle(116, 130, 62), Hex("#B9A6E8"));
                c.Stroke(Shape.Ring(118, 128, 38, 12), 3f, Canvas2D.Outline); c.Fill(Shape.Ring(118, 128, 38, 12), Hex("#D9CCF6"));
                c.Fill(Shape.Circle(118, 128, 12), Hex("#D9CCF6"));
            });
            Def("toy_butterfly", c =>
            {
                c.FillOutlined(Shape.Union(Shape.Ellipse(78, 168, 52, 44, 20), Shape.Ellipse(178, 168, 52, 44, -20)), Hex("#FF6F91"));
                c.FillOutlined(Shape.Union(Shape.Ellipse(88, 86, 40, 34, -20), Shape.Ellipse(168, 86, 40, 34, 20)), Hex("#FFD23F"));
                c.Fill(Shape.Circle(72, 172, 16), White); c.Fill(Shape.Circle(184, 172, 16), White);
                c.FillOutlined(Shape.Ellipse(128, 128, 14, 66), Hex("#8B5A3C"));
                c.Stroke(Shape.Capsule(122, 190, 104, 226, 0.5f), 4f, Dark); c.Stroke(Shape.Capsule(134, 190, 152, 226, 0.5f), 4f, Dark);
            });

            // Washing-up & capsule art
            Def("icon_sink", c =>
            {
                c.FillOutlined(Shape.RoundRect(128, 92, 100, 52, 30), Hex("#CFE8FA"));
                c.Fill(Shape.RoundRect(128, 104, 84, 28, 20), Hex("#9ED3F5"));
                c.Stroke(Shape.Capsule(170, 150, 170, 200, 0.5f), 12f, Hex("#BFC8D6"));
                c.Stroke(Shape.Capsule(170, 200, 130, 200, 0.5f), 12f, Hex("#BFC8D6"));
                foreach (var (x, y, r) in new[] { (86f, 150f, 22f), (118f, 168f, 16f), (70f, 186f, 13f) }) { c.FillOutlined(Shape.Circle(x, y, r), new Color(1, 1, 1, 0.95f), 4f); c.Fill(Shape.Circle(x - r * 0.35f, y + r * 0.35f, r * 0.25f), new Color(0.7f, 0.85f, 1f)); }
            });
            Def("icon_sponge", c =>
            {
                c.FillOutlined(Shape.RoundRect(128, 116, 86, 50, 22), Hex("#FFD23F"));
                c.Fill(Shape.RoundRect(128, 82, 86, 16, 12).Intersect(Shape.RoundRect(128, 116, 86, 50, 22)), Hex("#7BD389"));
                var rnd = new System.Random(3);
                for (int i = 0; i < 9; i++) c.Fill(Shape.Circle(64 + rnd.Next(0, 128), 108 + rnd.Next(0, 46), 4 + rnd.Next(0, 4)), Hex("#F2B71E"));
            });
            Def("ui_plate", c =>
            {
                c.Fill(Shape.Circle(132, 122, 120), new Color(0, 0, 0, 0.15f), 4f);
                c.FillOutlined(Shape.Circle(128, 128, 118), White);
                c.Fill(Shape.Ring(128, 128, 84, 3), Hex("#D8E4F0"));
                c.Fill(Shape.Ring(128, 128, 110, 6), Hex("#83B6E8"));
            });
            Def("icon_plate_clean", c =>
            {
                c.FillOutlined(Shape.Circle(128, 128, 104), White);
                c.Stroke(Shape.Ring(128, 128, 76, 2), 3f, Hex("#D8E4F0"));
                c.Fill(Shape.Ring(128, 128, 96, 6), Hex("#83B6E8"));
                c.Fill(Shape.Star(176, 176, 22, 8, 4), Hex("#FFD23F"));
            });
            Def("ui_splotch", c =>
            {
                Shape s = Shape.Union(Shape.Circle(128, 128, 70), Shape.Circle(176, 150, 40), Shape.Circle(86, 170, 34), Shape.Circle(96, 84, 38), Shape.Circle(170, 92, 30),
                    Shape.Circle(214, 112, 14), Shape.Circle(42, 120, 14));
                c.Fill(s, Color.white);
            });
            Def("ui_soap_bubble", c =>
            {
                c.Fill(Shape.Circle(128, 128, 120), new Color(0.85f, 0.95f, 1f, 0.35f));
                c.Fill(Shape.Ring(128, 128, 114, 8), new Color(1f, 1f, 1f, 0.9f));
                c.Fill(Shape.Ellipse(92, 166, 26, 16, 35), new Color(1f, 1f, 1f, 0.9f));
            });
            Def("ui_capsule_top", c =>
            {
                Shape dome = Shape.Circle(128, 60, 104).Intersect(Shape.RoundRect(128, 130, 120, 70, 0));
                c.FillOutlined(dome, Color.white);
                c.Fill(Shape.Ellipse(86, 120, 22, 34, -30).Intersect(dome), new Color(1, 1, 1, 0.55f));
            });
            Def("ui_capsule_bottom", c =>
            {
                Shape bowl = Shape.Circle(128, 196, 104).Intersect(Shape.RoundRect(128, 126, 120, 70, 0));
                c.FillOutlined(bowl, Hex("#FFF6E8"));
                c.Fill(Shape.RoundRect(128, 190, 104, 6, 3), Hex("#E8DCCB"));
            });
            Def("ui_burst", c =>
            {
                for (int i = 0; i < 12; i++)
                {
                    float a = i / 12f * Mathf.PI * 2f;
                    c.Fill(Shape.Polygon(new Vector2(128, 128), new Vector2(128 + Mathf.Cos(a - 0.12f) * 128, 128 + Mathf.Sin(a - 0.12f) * 128),
                        new Vector2(128 + Mathf.Cos(a + 0.12f) * 128, 128 + Mathf.Sin(a + 0.12f) * 128)), new Color(1f, 0.93f, 0.6f, i % 2 == 0 ? 0.55f : 0.3f));
                }
                c.RadialGlow(128, 128, 90, new Color(1f, 1f, 0.85f, 0.9f));
            });
            Def("icon_capsule_machine", c =>
            {
                c.FillOutlined(Shape.RoundRect(128, 62, 70, 46, 18), Hex("#E96A52"));
                c.Fill(Shape.RoundRect(128, 54, 26, 18, 8), Dark);
                c.FillOutlined(Shape.Circle(128, 166, 78), Hex("#CFE8FA"));
                (float x, float y, string col)[] caps = { (100, 140, "#FF6F91"), (148, 132, "#FFD23F"), (124, 182, "#83B6E8"), (160, 182, "#7BD389"), (92, 186, "#B9A6E8") };
                foreach (var (x, y, col) in caps) c.FillOutlined(Shape.Circle(x, y, 20), Hex(col), 4f);
                c.Fill(Shape.Ellipse(96, 214, 22, 12, 30), new Color(1, 1, 1, 0.7f));
                c.FillOutlined(Shape.Circle(128, 90, 12), Hex("#FFD23F"), 4f);
            });
            Def("icon_toy_shelf", c =>
            {
                c.FillOutlined(Shape.RoundRect(128, 120, 104, 96, 16), Hex("#C98B5A"));
                c.Fill(Shape.RoundRect(128, 120, 90, 82, 10), Hex("#FFE9CF"));
                c.Fill(Shape.RoundRect(128, 120, 90, 5, 2), Hex("#C98B5A"));
                c.Push(42, 126, 0.32f); DrawToyDuck(c); c.Pop();
                c.Fill(Shape.Circle(170, 160, 22), Hex("#FF6F91"));
                c.FillOutlined(Shape.RoundRect(96, 70, 22, 22, 6), Hex("#83B6E8"), 4f);
                c.Fill(Shape.Star(160, 72, 26, 11, 5), Hex("#FFD23F"));
            });
            Def("sticker_toy_collector", c => Sticker(c, Hex("#B9A6E8"), x => { x.Push(20, 20, 0.85f); DrawToyDuck(x); x.Pop(); }));
            Def("sticker_squeaky_clean", c => Sticker(c, Hex("#83B6E8"), x =>
            {
                x.FillOutlined(Shape.Circle(128, 120, 84), White);
                x.Fill(Shape.Ring(128, 120, 78, 6), Hex("#83B6E8"));
                x.Fill(Shape.Star(128, 120, 44, 18, 4), Hex("#FFD23F"));
                foreach (var (bx, by, r) in new[] { (196f, 196f, 22f), (60f, 196f, 16f), (204f, 60f, 14f) }) x.FillOutlined(Shape.Circle(bx, by, r), new Color(0.9f, 0.97f, 1f), 4f);
            }));
            Def("icon_party_hat", c =>
            {
                c.FillOutlined(Shape.Polygon(new Vector2(60, 40), new Vector2(196, 40), new Vector2(128, 210)).Round(8), Hex("#FF6F91"));
                c.Fill(Shape.Polygon(new Vector2(60, 40), new Vector2(196, 40), new Vector2(128, 210)).Round(8).Intersect(Shape.RoundRect(128, 100, 120, 14, 0, -20)), Hex("#FFD23F"));
                c.FillOutlined(Shape.Circle(128, 214, 18), Hex("#83B6E8"));
            });
        }

        private static void DrawToyDuck(Canvas2D c)
        {
            c.FillOutlined(Shape.Union(Shape.Ellipse(118, 92, 84, 52), Shape.Circle(162, 160, 50)), Hex("#FFD23F"));
            c.Fill(Shape.Ellipse(104, 102, 40, 22, 15), Hex("#FFE577"));
            c.FillOutlined(Shape.Ellipse(214, 152, 24, 12), Hex("#FF8F3A"), 4f);
            c.Fill(Shape.Circle(170, 174, 9), Dark);
            c.Fill(Shape.Circle(173, 177, 3), White);
            c.Fill(Shape.Ellipse(150, 146, 12, 8), new Color(1f, 0.6f, 0.6f, 0.6f));
        }
    }
}
