using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LittleBurgerWorld.EditorTools
{
    /// <summary>
    /// All original 2D artwork, painted procedurally (no external or copyrighted assets).
    /// Icons are authored in a 256×256 space, y up.
    /// </summary>
    public static class IconLibrary
    {
        public const string IconFolder = EditorAssetUtil.Root + "/Art/UI/Icons";
        public const string TextureFolder = EditorAssetUtil.Root + "/Art/Textures";

        // Palette (brief colours + supporting tones)
        public static readonly Color Cheese = Hex("#F5A623");
        public static readonly Color CheeseLight = Hex("#FFD166");
        public static readonly Color Tomato = Hex("#E96A52");
        public static readonly Color TomatoRed = Hex("#E4503F");
        public static readonly Color Mint = Hex("#79BFA0");
        public static readonly Color Sky = Hex("#83B6E8");
        public static readonly Color Cream = Hex("#F5D9B5");
        public static readonly Color Brown = Hex("#A66A42");
        public static readonly Color Pink = Hex("#F2A8B7");
        public static readonly Color Bun = Hex("#EDAA4E");
        public static readonly Color BunDark = Hex("#D98A35");
        public static readonly Color Patty = Hex("#7E4A2C");
        public static readonly Color PattyRaw = Hex("#EE9A93");
        public static readonly Color Lettuce = Hex("#8CCB5E");
        public static readonly Color LettuceDark = Hex("#5FA84A");
        public static readonly Color White = Hex("#FFF9F1");
        public static readonly Color Dark = Hex("#4A3428");
        public static readonly Color Lavender = Hex("#B9A6E8");
        public static readonly Color Fries = Hex("#F7C948");
        public static readonly Color Chocolate = Hex("#8B5A3C");
        public static readonly Color Vanilla = Hex("#FFF0C9");
        public static readonly Color Strawberry = Hex("#F48FB1");

        private static Color Hex(string h) => EditorAssetUtil.Hex(h);

        private struct IconDef
        {
            public string Name;
            public int Size;
            public Action<Canvas2D> Draw;
            public Vector4 Border; // 9-slice border in pixels
            public Vector2 Pivot;
            public bool Native; // authored at its own pixel size instead of the 256 space
        }

        private static readonly List<IconDef> s_Defs = new List<IconDef>();

        private static void Def(string name, Action<Canvas2D> draw, int size = 256, Vector4 border = default, Vector2? pivot = null,
            bool native = false)
        {
            s_Defs.Add(new IconDef { Name = name, Size = size, Draw = draw, Border = border, Pivot = pivot ?? new Vector2(0.5f, 0.5f), Native = native });
        }

        /// <summary>Paints every icon to PNG and imports it as a sprite. Safe to re-run.</summary>
        public static void GenerateAll()
        {
            EditorAssetUtil.EnsureFolder(IconFolder);
            EditorAssetUtil.EnsureFolder(TextureFolder);
            s_Defs.Clear();
            RegisterFood();
            RegisterCharacters();
            RegisterUi();
            RegisterStickers();
            RegisterParticles();

            var paths = new List<(string path, IconDef def)>();
            foreach (IconDef d in s_Defs)
            {
                var c = new Canvas2D(d.Size, d.Size);
                if (!d.Native && d.Size != 256) c.Push(0, 0, d.Size / 256f);
                d.Draw(c);
                string path = $"{IconFolder}/{d.Name}.png";
                File.WriteAllBytes(path, c.ToPng());
                paths.Add((path, d));
            }

            WriteTexture("Floor_Checker", FloorChecker(), false, TextureWrapMode.Repeat);
            WriteTexture("Wall_Dots", WallDots(), false, TextureWrapMode.Repeat);
            WriteTexture("AppIcon", AppIcon(), false, TextureWrapMode.Clamp);

            AssetDatabase.Refresh();
            foreach (var (path, def) in paths) ConfigureSprite(path, def.Border, def.Pivot);
            ConfigureTexture($"{TextureFolder}/Floor_Checker.png", TextureWrapMode.Repeat, false);
            ConfigureTexture($"{TextureFolder}/Wall_Dots.png", TextureWrapMode.Repeat, false);
            ConfigureTexture($"{TextureFolder}/AppIcon.png", TextureWrapMode.Clamp, false);
        }

        public static Sprite Get(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/{name}.png");
        public static Texture2D GetTexture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{name}.png");

        private static void WriteTexture(string name, Canvas2D c, bool sprite, TextureWrapMode wrap)
        {
            File.WriteAllBytes($"{TextureFolder}/{name}.png", c.ToPng());
        }

        private static void ConfigureSprite(string path, Vector4 border, Vector2 pivot)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.spritePixelsPerUnit = 256;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteBorder = border;
            settings.spritePivot = pivot;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.SaveAndReimport();
        }

        private static void ConfigureTexture(string path, TextureWrapMode wrap, bool alpha)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            ti.textureType = TextureImporterType.Default;
            ti.wrapMode = wrap;
            ti.mipmapEnabled = true;
            ti.alphaIsTransparency = alpha;
            ti.SaveAndReimport();
        }

        // =====================================================================================
        // FOOD
        // =====================================================================================
        private static void RegisterFood()
        {
            Def("icon_bun_bottom", DrawBunBottom);
            Def("icon_patty_raw", c => DrawPatty(c, PattyRaw, false));
            Def("icon_patty", c => DrawPatty(c, Patty, true));
            Def("icon_cheese", DrawCheese);
            Def("icon_lettuce", DrawLettuce);
            Def("icon_tomato", DrawTomato);
            Def("icon_bun_top", DrawBunTop);
            Def("icon_pickle", DrawPickle);
            Def("icon_burger", c => DrawBurger(c, false));
            Def("icon_cheeseburger", c => DrawBurger(c, true));
            Def("icon_fries", DrawFriesBox);
            Def("icon_potato", DrawPotato);
            Def("icon_milkshake_strawberry", c => DrawShake(c, Strawberry));
            Def("icon_milkshake_chocolate", c => DrawShake(c, Chocolate));
            Def("icon_milkshake_vanilla", c => DrawShake(c, Vanilla));
            Def("icon_meal", DrawMeal);
        }

        public static void DrawBunBottom(Canvas2D c)
        {
            c.FillOutlined(Shape.RoundRect(128, 104, 104, 40, 34), Bun);
            c.Fill(Shape.RoundRect(128, 132, 96, 12, 10), CheeseLight);
            c.Fill(Shape.RoundRect(128, 86, 88, 8, 8), new Color(1, 1, 1, 0.25f));
        }

        public static void DrawPatty(Canvas2D c, Color col, bool grill)
        {
            c.FillOutlined(Shape.RoundRect(128, 128, 108, 36, 32), col);
            c.Fill(Shape.RoundRect(128, 142, 92, 10, 10), new Color(1, 1, 1, 0.18f));
            if (grill)
            {
                for (int i = 0; i < 4; i++) c.Fill(Shape.Capsule(70 + i * 38, 104, 92 + i * 38, 150, 5), new Color(0.3f, 0.17f, 0.1f, 0.85f));
            }
            else
            {
                var rnd = new System.Random(3);
                for (int i = 0; i < 9; i++) c.Fill(Shape.Circle(50 + rnd.Next(0, 156), 108 + rnd.Next(0, 40), 5), new Color(1f, 0.85f, 0.85f, 0.8f));
            }
        }

        public static void DrawCheese(Canvas2D c)
        {
            Shape slice = Shape.RoundRect(128, 132, 86, 86, 16, 8f);
            Shape holes = Shape.Union(Shape.Circle(96, 160, 15), Shape.Circle(160, 112, 18), Shape.Circle(150, 176, 10), Shape.Circle(92, 100, 9));
            c.FillOutlined(slice, Cheese);
            c.Fill(Shape.RoundRect(118, 150, 60, 40, 14, 8f), new Color(1, 1, 1, 0.15f));
            c.Fill(holes.Intersect(slice), new Color(0.85f, 0.55f, 0.1f));
            c.Fill(Shape.Capsule(84, 50, 84, 66, 11), Cheese);
        }

        public static void DrawLettuce(Canvas2D c)
        {
            var parts = new List<Shape> { Shape.Ellipse(128, 128, 96, 74) };
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f;
                parts.Add(Shape.Circle(128 + Mathf.Cos(a) * 92, 128 + Mathf.Sin(a) * 70, 22));
            }
            Shape leaf = Shape.Union(parts.ToArray());
            c.FillOutlined(leaf, Lettuce);
            c.Fill(Shape.Ellipse(128, 128, 70, 50), new Color(0.72f, 0.88f, 0.5f));
            c.Fill(Shape.Capsule(60, 128, 196, 128, 4), LettuceDark);
            for (int i = 0; i < 4; i++)
            {
                float x = 80 + i * 32;
                c.Fill(Shape.Capsule(x, 128, x + 20, 160, 3), LettuceDark);
                c.Fill(Shape.Capsule(x, 128, x + 20, 96, 3), LettuceDark);
            }
        }

        public static void DrawTomato(Canvas2D c)
        {
            c.FillOutlined(Shape.Circle(128, 128, 96), TomatoRed);
            c.Fill(Shape.Circle(128, 128, 76), Hex("#F27A62"));
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                float cx = 128 + Mathf.Cos(a) * 42, cy = 128 + Mathf.Sin(a) * 42;
                c.Fill(Shape.Ellipse(cx, cy, 26, 18, a * Mathf.Rad2Deg), Hex("#E4503F"));
                c.Fill(Shape.Ellipse(cx, cy, 6, 9, a * Mathf.Rad2Deg), Hex("#FFE7A0"));
            }
            c.Fill(Shape.Circle(128, 128, 12), Hex("#FFC2A8"));
        }

        public static void DrawBunTop(Canvas2D c)
        {
            Shape dome = Shape.Union(Shape.Ellipse(128, 120, 108, 90).Intersect(Shape.RoundRect(128, 170, 120, 70, 0)),
                Shape.RoundRect(128, 104, 108, 18, 16));
            c.FillOutlined(dome, Hex("#E89A3C"));
            c.Fill(Shape.Ellipse(96, 168, 40, 18, 20), new Color(1, 1, 1, 0.25f));
            DrawSeeds(c, 128, 150, 80, 40);
        }

        private static void DrawSeeds(Canvas2D c, float cx, float cy, float rx, float ry)
        {
            var rnd = new System.Random(7);
            for (int i = 0; i < 9; i++)
            {
                float x = cx + (float)(rnd.NextDouble() * 2 - 1) * rx;
                float y = cy + (float)(rnd.NextDouble() * 2 - 1) * ry;
                c.Fill(Shape.Ellipse(x, y, 8, 4.5f, rnd.Next(-40, 40)), White);
            }
        }

        public static void DrawPickle(Canvas2D c)
        {
            for (int i = 0; i < 3; i++)
            {
                float x = 80 + i * 48, y = 120 + (i % 2) * 24;
                c.FillOutlined(Shape.Circle(x, y, 44), Hex("#6FAF4A"));
                c.Fill(Shape.Circle(x, y, 33), Hex("#A7D474"));
                for (int k = 0; k < 6; k++)
                {
                    float a = k / 6f * Mathf.PI * 2f;
                    c.Fill(Shape.Circle(x + Mathf.Cos(a) * 16, y + Mathf.Sin(a) * 16, 4), Hex("#E8F2B8"));
                }
            }
        }

        public static void DrawBurger(Canvas2D c, bool cheese)
        {
            // bottom bun
            c.FillOutlined(Shape.RoundRect(128, 62, 100, 24, 20), Bun);
            // patty
            c.FillOutlined(Shape.RoundRect(128, 96, 108, 20, 18), Patty);
            if (cheese)
            {
                Shape sl = Shape.Union(Shape.RoundRect(128, 116, 104, 8, 4), Shape.Polygon(new Vector2(60, 116), new Vector2(92, 116), new Vector2(76, 88)),
                    Shape.Polygon(new Vector2(150, 116), new Vector2(186, 116), new Vector2(170, 92)));
                c.FillOutlined(sl, Cheese, 4f);
            }
            // lettuce
            var parts = new List<Shape> { Shape.RoundRect(128, 128, 100, 8, 6) };
            for (int i = 0; i < 9; i++) parts.Add(Shape.Circle(36 + i * 23, 124, 10));
            c.FillOutlined(Shape.Union(parts.ToArray()), Lettuce, 4f);
            // tomato
            c.FillOutlined(Shape.RoundRect(128, 140, 92, 7, 6), TomatoRed, 4f);
            // top bun
            Shape dome = Shape.Union(Shape.Ellipse(128, 150, 106, 70).Intersect(Shape.RoundRect(128, 200, 120, 50, 0)),
                Shape.RoundRect(128, 158, 106, 10, 9));
            c.FillOutlined(dome, Hex("#E89A3C"));
            c.Fill(Shape.Ellipse(96, 192, 34, 13, 18), new Color(1, 1, 1, 0.25f));
            DrawSeeds(c, 128, 184, 70, 20);
        }

        public static void DrawFriesBox(Canvas2D c)
        {
            var rnd = new System.Random(11);
            for (int i = 0; i < 9; i++)
            {
                float x = 74 + i * 14 + rnd.Next(-3, 4);
                float top = 196 + rnd.Next(-20, 26);
                float tilt = rnd.Next(-10, 10);
                c.FillOutlined(Shape.RoundRect(x, top - 40, 9, 48, 6, tilt), Fries, 4f);
            }
            Shape box = Shape.Polygon(new Vector2(58, 150), new Vector2(198, 150), new Vector2(178, 30), new Vector2(78, 30)).Round(6);
            c.FillOutlined(box, Tomato);
            c.Fill(Shape.RoundRect(128, 142, 70, 6, 5), new Color(1, 1, 1, 0.3f));
            c.FillOutlined(Shape.Star(128, 90, 30, 14, 5), CheeseLight, 4f);
        }

        public static void DrawPotato(Canvas2D c)
        {
            c.FillOutlined(Shape.Ellipse(128, 124, 92, 70, -12), Hex("#C9955A"));
            c.Fill(Shape.Ellipse(108, 148, 40, 18, -12), new Color(1, 1, 1, 0.2f));
            var rnd = new System.Random(5);
            for (int i = 0; i < 6; i++) c.Fill(Shape.Ellipse(70 + rnd.Next(0, 110), 90 + rnd.Next(0, 70), 6, 4), Hex("#9C6B3A"));
        }

        public static void DrawShake(Canvas2D c, Color flavour)
        {
            // straw
            c.FillOutlined(Shape.Capsule(150, 160, 178, 240, 8), Mint, 4f);
            // cream
            Shape cream = Shape.Union(Shape.Ellipse(128, 168, 66, 26), Shape.Ellipse(128, 192, 48, 22), Shape.Ellipse(128, 212, 28, 18));
            c.FillOutlined(cream, White);
            c.FillOutlined(Shape.Circle(132, 236, 13), TomatoRed, 4f);
            // cup
            Shape cup = Shape.Polygon(new Vector2(62, 162), new Vector2(194, 162), new Vector2(172, 22), new Vector2(84, 22)).Round(6);
            c.FillOutlined(cup, flavour);
            c.Fill(Shape.Polygon(new Vector2(70, 110), new Vector2(186, 110), new Vector2(181, 80), new Vector2(75, 80)), new Color(1, 1, 1, 0.55f));
            c.Fill(Shape.RoundRect(96, 130, 8, 22, 6), new Color(1, 1, 1, 0.35f));
        }

        public static void DrawMeal(Canvas2D c)
        {
            c.FillOutlined(Shape.RoundRect(128, 54, 118, 22, 16), Mint);
            c.Push(-10, 40, 0.55f); DrawBurger(c, true); c.Pop();
            c.Push(118, 40, 0.5f); DrawFriesBox(c); c.Pop();
            c.Push(150, 70, 0.45f); DrawShake(c, Strawberry); c.Pop();
        }

        // =====================================================================================
        // CHARACTERS
        // =====================================================================================
        private static void RegisterCharacters()
        {
            Def("portrait_benny", DrawBennyFace);
            Def("portrait_penny", DrawPennyFace);
        }

        public static void DrawBennyFace(Canvas2D c)
        {
            Color fur = Hex("#A0663F");
            Color furLight = Hex("#E7C39A");
            c.FillOutlined(Shape.Circle(62, 196, 34), fur);
            c.FillOutlined(Shape.Circle(194, 196, 34), fur);
            c.Fill(Shape.Circle(62, 196, 18), Pink);
            c.Fill(Shape.Circle(194, 196, 18), Pink);
            c.FillOutlined(Shape.Ellipse(128, 124, 102, 94), fur);
            c.Fill(Shape.Ellipse(128, 92, 52, 38), furLight);
            DrawEyes(c, 92, 140, 164, 140);
            c.Fill(Shape.Ellipse(128, 108, 18, 12), Dark);
            c.Fill(Shape.Ellipse(123, 112, 6, 3), new Color(1, 1, 1, 0.6f));
            c.Fill(Shape.Ring(128, 92, 20, 6).Intersect(Shape.RoundRect(128, 76, 30, 16, 0)), Dark);
            c.Fill(Shape.Ellipse(70, 106, 18, 11), new Color(0.95f, 0.55f, 0.6f, 0.6f));
            c.Fill(Shape.Ellipse(186, 106, 18, 11), new Color(0.95f, 0.55f, 0.6f, 0.6f));
        }

        public static void DrawPennyFace(Canvas2D c)
        {
            Color fur = Hex("#FFF4F2");
            c.FillOutlined(Shape.Capsule(92, 170, 74, 238, 22), fur);
            c.FillOutlined(Shape.Capsule(164, 170, 182, 238, 22), fur);
            c.Fill(Shape.Capsule(92, 176, 76, 230, 11), Pink);
            c.Fill(Shape.Capsule(164, 176, 180, 230, 11), Pink);
            c.FillOutlined(Shape.Ellipse(128, 116, 100, 90), fur);
            DrawEyes(c, 94, 132, 162, 132);
            c.Fill(Shape.Ellipse(128, 104, 12, 9), Hex("#F47C9A"));
            c.Fill(Shape.Ring(128, 88, 16, 5).Intersect(Shape.RoundRect(128, 74, 24, 14, 0)), Dark);
            c.Fill(Shape.Ellipse(74, 100, 18, 11), new Color(0.97f, 0.55f, 0.65f, 0.55f));
            c.Fill(Shape.Ellipse(182, 100, 18, 11), new Color(0.97f, 0.55f, 0.65f, 0.55f));
            c.FillOutlined(Shape.Union(Shape.Ellipse(178, 196, 20, 14, 30), Shape.Ellipse(206, 210, 20, 14, -30)), Lavender, 4f);
        }

        private static void DrawEyes(Canvas2D c, float lx, float ly, float rx, float ry)
        {
            foreach (var (x, y) in new[] { (lx, ly), (rx, ry) })
            {
                c.Fill(Shape.Ellipse(x, y, 17, 22), Dark);
                c.Fill(Shape.Circle(x - 5, y + 8, 6.5f), Color.white);
                c.Fill(Shape.Circle(x + 6, y - 7, 3f), new Color(1, 1, 1, 0.8f));
            }
        }

        // =====================================================================================
        // UI GLYPHS
        // =====================================================================================
        private static void RegisterUi()
        {
            Def("ui_circle", c => c.Fill(Shape.Circle(128, 128, 124), Color.white));
            Def("ui_ring", c => c.Fill(Shape.Ring(128, 128, 112, 22), Color.white));
            Def("ui_panel", c => c.Fill(Shape.RoundRect(64, 64, 62, 62, 40), Color.white), 128, new Vector4(48, 48, 48, 48), null, true);
            Def("ui_panel_outline", c =>
            {
                c.Fill(Shape.RoundRect(64, 64, 62, 62, 40), Canvas2D.Outline);
                c.Fill(Shape.RoundRect(64, 64, 56, 56, 34), Color.white);
            }, 128, new Vector4(48, 48, 48, 48), null, true);
            Def("ui_soft_shadow", c => c.Fill(Shape.RoundRect(64, 64, 52, 52, 36), new Color(0, 0, 0, 0.35f), 12f), 128, new Vector4(56, 56, 56, 56), null, true);
            Def("ui_bubble", c =>
            {
                c.Fill(Shape.RoundRect(64, 64, 62, 62, 44), Canvas2D.Outline);
                c.Fill(Shape.RoundRect(64, 64, 57, 57, 39), Color.white);
            }, 128, new Vector4(50, 50, 50, 50), null, true);
            Def("ui_thought_dots", c =>
            {
                c.FillOutlined(Shape.Circle(150, 200, 30), Color.white, 5f);
                c.FillOutlined(Shape.Circle(110, 130, 20), Color.white, 5f);
                c.FillOutlined(Shape.Circle(84, 76, 12), Color.white, 4f);
            });
            Def("icon_home", c =>
            {
                Shape roof = Shape.Polygon(new Vector2(30, 128), new Vector2(128, 218), new Vector2(226, 128)).Round(10);
                Shape body = Shape.RoundRect(128, 92, 70, 62, 16);
                c.FillOutlined(Shape.Union(roof, body), White);
                c.Fill(Shape.RoundRect(128, 70, 22, 40, 12), Tomato);
                c.Fill(Shape.Circle(128, 140, 14), Sky);
            });
            Def("icon_play", c =>
            {
                c.FillOutlined(Shape.Polygon(new Vector2(80, 40), new Vector2(80, 216), new Vector2(216, 128)).Round(14), White);
            });
            Def("icon_star", c =>
            {
                c.FillOutlined(Shape.Star(128, 124, 112, 52, 5).Round(8), CheeseLight);
                c.Fill(Shape.Ellipse(104, 156, 22, 12, 30), new Color(1, 1, 1, 0.55f));
            });
            Def("icon_star_empty", c =>
            {
                c.FillOutlined(Shape.Star(128, 124, 112, 52, 5).Round(8), new Color(0.92f, 0.86f, 0.78f));
            });
            Def("icon_heart", c =>
            {
                c.FillOutlined(Shape.Heart(128, 128, 80), Hex("#F47C9A"));
                c.Fill(Shape.Ellipse(92, 160, 20, 12, 30), new Color(1, 1, 1, 0.5f));
            });
            Def("icon_hand", DrawHand, 256, default, new Vector2(0.36f, 0.92f));
            Def("icon_check", c => c.FillOutlined(Shape.Union(Shape.Capsule(54, 132, 104, 82, 20), Shape.Capsule(104, 82, 200, 186, 20)), Mint));
            Def("icon_close", c =>
            {
                c.FillOutlined(Shape.Circle(128, 128, 110), Tomato);
                c.Fill(Shape.Union(Shape.Capsule(82, 82, 174, 174, 16), Shape.Capsule(82, 174, 174, 82, 16)), White);
            });
            Def("icon_gear", c =>
            {
                var teeth = new List<Shape> { Shape.Circle(128, 128, 78) };
                for (int i = 0; i < 8; i++) teeth.Add(Shape.RoundRect(128 + Mathf.Cos(i * Mathf.PI / 4) * 82, 128 + Mathf.Sin(i * Mathf.PI / 4) * 82, 22, 22, 8, i * 45));
                c.FillOutlined(Shape.Union(teeth.ToArray()).Subtract(Shape.Circle(128, 128, 30)), Hex("#C9D3DD"));
            });
            Def("icon_replay", c =>
            {
                Shape arc = Shape.Ring(128, 120, 74, 34).Subtract(Shape.Polygon(new Vector2(128, 120), new Vector2(240, 240), new Vector2(240, 120)));
                c.FillOutlined(arc, White);
                c.FillOutlined(Shape.Polygon(new Vector2(176, 160), new Vector2(236, 200), new Vector2(226, 128)).Round(6), White);
            });
            Def("icon_grid", c =>
            {
                for (int y = 0; y < 2; y++)
                for (int x = 0; x < 2; x++)
                    c.FillOutlined(Shape.RoundRect(84 + x * 88, 84 + y * 88, 34, 34, 12), x == y ? Cheese : Mint);
            });
            Def("icon_sparkle_clean", c =>
            {
                c.FillOutlined(Shape.Star(110, 120, 90, 24, 4, 45).Round(4), CheeseLight);
                c.FillOutlined(Shape.Star(196, 196, 40, 12, 4, 45).Round(3), White);
                c.FillOutlined(Shape.Star(190, 64, 30, 9, 4, 45).Round(3), Sky);
            });
            Def("icon_stickers", c =>
            {
                c.FillOutlined(Shape.RoundRect(128, 120, 92, 100, 18, -6), Lavender);
                c.Fill(Shape.RoundRect(128, 120, 76, 84, 12, -6), White);
                c.FillOutlined(Shape.Star(104, 150, 30, 13, 5), CheeseLight, 4f);
                c.FillOutlined(Shape.Heart(156, 92, 22), Hex("#F47C9A"), 4f);
                c.FillOutlined(Shape.Circle(106, 80, 18), Mint, 4f);
            });
            Def("icon_music", c =>
            {
                c.FillOutlined(Shape.Union(Shape.Ellipse(86, 70, 34, 26, -20), Shape.Ellipse(182, 92, 34, 26, -20),
                    Shape.RoundRect(114, 140, 10, 76, 6), Shape.RoundRect(210, 162, 10, 76, 6),
                    Shape.Polygon(new Vector2(104, 210), new Vector2(220, 236), new Vector2(220, 206), new Vector2(104, 180))), Lavender);
            });
            Def("icon_speaker", c =>
            {
                c.FillOutlined(Shape.Union(Shape.RoundRect(70, 128, 34, 40, 8), Shape.Polygon(new Vector2(80, 128), new Vector2(150, 206), new Vector2(150, 50))), Sky);
                c.Stroke(Shape.Circle(150, 128, 50).Intersect(Shape.RoundRect(210, 128, 60, 80, 0)), 12, Canvas2D.Outline);
                c.Stroke(Shape.Circle(150, 128, 84).Intersect(Shape.RoundRect(240, 128, 70, 110, 0)), 12, Canvas2D.Outline);
            });
            Def("icon_voice", c =>
            {
                c.FillOutlined(Shape.Union(Shape.RoundRect(128, 140, 100, 70, 40), Shape.Polygon(new Vector2(70, 90), new Vector2(110, 80), new Vector2(56, 30))), White);
                for (int i = 0; i < 3; i++) c.Fill(Shape.Circle(84 + i * 44, 140, 12), Mint);
            });
            Def("icon_lock", c =>
            {
                c.Stroke(Shape.RoundRect(128, 150, 50, 60, 48), 22, Canvas2D.Outline);
                c.FillOutlined(Shape.RoundRect(128, 96, 80, 62, 18), Cheese);
            });
            Def("icon_language", c =>
            {
                c.FillOutlined(Shape.Circle(128, 128, 100), Sky);
                c.Stroke(Shape.Ellipse(128, 128, 44, 98), 8, White);
                c.Fill(Shape.RoundRect(128, 128, 98, 4, 3), White);
                c.Fill(Shape.RoundRect(128, 170, 86, 4, 3), White);
                c.Fill(Shape.RoundRect(128, 86, 86, 4, 3), White);
            });
            Def("icon_shield", c =>
            {
                c.FillOutlined(Shape.Polygon(new Vector2(128, 236), new Vector2(214, 200), new Vector2(200, 90), new Vector2(128, 20), new Vector2(56, 90), new Vector2(42, 200)).Round(8), Mint);
                c.Fill(Shape.Union(Shape.Capsule(86, 132, 116, 102, 12), Shape.Capsule(116, 102, 172, 166, 12)), White);
            });
            Def("icon_ripple", c => c.Fill(Shape.Ring(128, 128, 104, 14), Color.white));
            Def("logo_burger", DrawBurgerMascot);
        }

        public static void DrawHand(Canvas2D c)
        {
            // Cartoon pointing hand; fingertip near (92, 236).
            Shape finger = Shape.Capsule(92, 226, 96, 140, 22);
            Shape palm = Shape.RoundRect(130, 98, 62, 58, 30, -8);
            Shape thumb = Shape.Capsule(74, 112, 46, 150, 18);
            Shape knuckles = Shape.Union(Shape.Circle(142, 148, 22), Shape.Circle(178, 136, 20), Shape.Circle(200, 112, 18));
            Shape cuff = Shape.RoundRect(150, 34, 52, 22, 12, -8);
            c.Fill(Shape.Union(finger, palm, thumb, knuckles).Offset(6, -6), new Color(0, 0, 0, 0.2f), 6f);
            c.FillOutlined(cuff, Sky);
            c.FillOutlined(Shape.Union(finger, palm, thumb, knuckles), White, 7f);
            c.Fill(Shape.Ellipse(94, 214, 9, 6), new Color(1f, 0.8f, 0.82f));
        }

        public static void DrawBurgerMascot(Canvas2D c)
        {
            DrawBurger(c, true);
            DrawEyes(c, 102, 192, 154, 192);
            c.Fill(Shape.Ring(128, 186, 16, 6).Intersect(Shape.RoundRect(128, 172, 24, 12, 0)), Dark);
            c.Fill(Shape.Ellipse(74, 178, 12, 7), new Color(0.95f, 0.5f, 0.55f, 0.6f));
            c.Fill(Shape.Ellipse(182, 178, 12, 7), new Color(0.95f, 0.5f, 0.55f, 0.6f));
        }

        // =====================================================================================
        // STICKERS
        // =====================================================================================
        private static void RegisterStickers()
        {
            Def("sticker_first_burger", c => Sticker(c, Cheese, x => DrawBurger(x, false)));
            Def("sticker_cheese_champ", c => Sticker(c, CheeseLight, x => DrawBurger(x, true)));
            Def("sticker_fries_fan", c => Sticker(c, Tomato, DrawFriesBox));
            Def("sticker_shake_maker", c => Sticker(c, Pink, x => DrawShake(x, Strawberry)));
            Def("sticker_meal_master", c => Sticker(c, Mint, DrawMeal));
            Def("sticker_bear_buddy", c => Sticker(c, Brown, DrawBennyFace));
            Def("sticker_tall_tower", c => Sticker(c, Sky, x =>
            {
                for (int i = 0; i < 4; i++) { x.Push(64, -40 + i * 52, 0.5f); DrawPatty(x, i % 2 == 0 ? Patty : Cheese, i % 2 == 0); x.Pop(); }
                x.Push(64, 160, 0.5f); DrawBunTop(x); x.Pop();
            }));
            Def("sticker_balloon_pop", c => Sticker(c, Lavender, x =>
            {
                x.FillOutlined(Shape.Ellipse(100, 150, 50, 62), Tomato);
                x.FillOutlined(Shape.Ellipse(164, 160, 46, 58), Sky);
                x.Stroke(Shape.Capsule(100, 88, 128, 30, 0.5f), 4, Canvas2D.Outline);
                x.Stroke(Shape.Capsule(164, 100, 128, 30, 0.5f), 4, Canvas2D.Outline);
            }));
            Def("sticker_super_chef", c => Sticker(c, Hex("#FFB86B"), x => x.FillOutlined(Shape.Star(128, 124, 104, 48, 5).Round(8), CheeseLight)));
        }

        private static void Sticker(Canvas2D c, Color ring, Action<Canvas2D> icon)
        {
            c.Fill(Shape.Circle(132, 122, 120), new Color(0, 0, 0, 0.18f), 4f);
            c.Fill(Shape.Circle(128, 128, 122), Canvas2D.Outline);
            c.Fill(Shape.Circle(128, 128, 116), White);
            c.Fill(Shape.Ring(128, 128, 104, 16), ring);
            c.Push(40, 40, 0.69f);
            icon(c);
            c.Pop();
        }

        // =====================================================================================
        // PARTICLES & TEXTURES
        // =====================================================================================
        private static void RegisterParticles()
        {
            Def("fx_sparkle", c =>
            {
                c.RadialGlow(128, 128, 120, new Color(1f, 1f, 0.9f, 0.6f), 2.5f);
                c.Fill(Shape.Star(128, 128, 120, 22, 4, 90).Round(2), Color.white);
            }, 128);
            Def("fx_soft", c => c.RadialGlow(128, 128, 126, Color.white, 1.6f), 64);
            Def("fx_confetti", c => c.Fill(Shape.RoundRect(128, 128, 110, 70, 26), Color.white), 64);
            Def("fx_heart", c => c.Fill(Shape.Heart(128, 124, 92), Color.white), 128);
        }

        private static Canvas2D FloorChecker()
        {
            var c = new Canvas2D(256, 256, Hex("#FFF3E2"));
            Color tile = Hex("#BFE3D0");
            c.Fill(Shape.RoundRect(64, 64, 60, 60, 14), tile);
            c.Fill(Shape.RoundRect(192, 192, 60, 60, 14), tile);
            c.Fill(Shape.RoundRect(192, 64, 58, 58, 14), Hex("#FFF8EE"));
            c.Fill(Shape.RoundRect(64, 192, 58, 58, 14), Hex("#FFF8EE"));
            return c;
        }

        private static Canvas2D WallDots()
        {
            var c = new Canvas2D(256, 256, Hex("#FFF1DE"));
            for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
            {
                float ox = (y % 2) * 32f;
                c.Fill(Shape.Circle(32 + x * 64 + ox, 32 + y * 64, 9), Hex("#FAD6DC"));
            }
            return c;
        }

        private static Canvas2D AppIcon()
        {
            var c = new Canvas2D(512, 512, Sky);
            c.Push(0, 0, 2f);
            c.Fill(Shape.Circle(128, 128, 112), Hex("#A9CDF0"));
            c.Push(24, 30, 0.82f);
            DrawBurgerMascot(c);
            c.Pop();
            c.Pop();
            return c;
        }
    }
}
