using System.Collections.Generic;
using LittleBurgerWorld.Core;
using UnityEditor;
using UnityEngine;
using A = LittleBurgerWorld.EditorTools.ArtLibrary;

namespace LittleBurgerWorld.EditorTools
{
    public static partial class PrefabBuilder
    {
        public const string FontPath = EditorAssetUtil.Root + "/Art/UI/Fonts/Fredoka-SemiBold.ttf";

        public static void BuildEnvironment()
        {
            Save(Table(), "Prop_Table");
            Save(Stool(A.SoftPink), "Prop_Stool");
            Save(Plant(), "Prop_Plant");
            Save(Lamp(), "Prop_Lamp");
            Save(Window(), "Prop_Window");
            Save(Bunting(9), "Prop_Bunting");
            Save(PictureFrame("icon_cheeseburger", A.CheeseYellow), "Prop_Picture_Burger");
            Save(PictureFrame("icon_milkshake_strawberry", A.Sky), "Prop_Picture_Shake");
            Save(PictureFrame("icon_fries", A.Mint), "Prop_Picture_Fries");
            Save(Sign(), "Prop_Sign");
            Save(Clock(), "Prop_Clock");
        }

        private static void Save(GameObject go, string name) => EditorAssetUtil.SavePrefab(go, $"{EnvFolder}/{name}.prefab");

        public static GameObject Table()
        {
            var root = new GameObject("Prop_Table");
            BlobShadow(root.transform, Vector3.zero, 2.0f, 2.0f);
            Part(root.transform, "Top", A.Disc(0.75f, 0.1f, 0.045f, 36), A.White, new Vector3(0f, 0.72f, 0f));
            Part(root.transform, "TopTrim", A.Mesh("TableTrim", () => MeshFactory.Torus(0.74f, 0.04f, 36, 8)), A.TomatoCoral, new Vector3(0f, 0.77f, 0f), castShadows: false);
            Part(root.transform, "Leg", A.Capsule(0.08f, 0.72f), A.Sky, new Vector3(0f, 0.36f, 0f));
            Part(root.transform, "Foot", A.Disc(0.35f, 0.06f, 0.025f, 28), A.Sky, Vector3.zero);
            AddBox(root, new Vector3(1.5f, 0.8f, 1.5f), new Vector3(0f, 0.4f, 0f));
            return root;
        }

        public static GameObject Stool(Material seat)
        {
            var root = new GameObject("Prop_Stool");
            BlobShadow(root.transform, Vector3.zero, 0.9f, 0.9f);
            Part(root.transform, "Seat", A.Disc(0.3f, 0.12f, 0.05f, 24), seat, new Vector3(0f, 0.48f, 0f));
            Part(root.transform, "Leg", A.Capsule(0.06f, 0.5f), A.Sky, new Vector3(0f, 0.24f, 0f));
            Part(root.transform, "Foot", A.Disc(0.22f, 0.04f, 0.02f, 20), A.Sky, Vector3.zero);
            return root;
        }

        public static GameObject Plant()
        {
            var root = new GameObject("Prop_Plant");
            Part(root.transform, "Pot", A.Mesh("PlantPot", () => MeshFactory.Lathe(new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(0.3f, 0f), new Vector2(0.4f, 0.55f), new Vector2(0.46f, 0.6f), new Vector2(0.44f, 0.66f), new Vector2(0f, 0.66f)
            }, 28, "PlantPot")), A.Terracotta, Vector3.zero);
            BlobShadow(root.transform, Vector3.zero, 1.3f, 1.3f);
            var rnd = new System.Random(8);
            Vector3[] blobs = { new Vector3(0f, 1.05f, 0f), new Vector3(-0.28f, 0.88f, 0.1f), new Vector3(0.27f, 0.9f, -0.05f), new Vector3(0.05f, 0.85f, 0.3f), new Vector3(0.05f, 1.35f, 0.05f) };
            for (int i = 0; i < blobs.Length; i++)
            {
                Part(root.transform, "Leaf" + i, A.SphereMesh, i % 2 == 0 ? A.Leaf : A.LeafDark, blobs[i], new Vector3(rnd.Next(0, 30), rnd.Next(0, 360), 0f),
                    Vector3.one * (0.5f + (float)rnd.NextDouble() * 0.15f));
            }
            AddBox(root, new Vector3(0.9f, 1.6f, 0.9f), new Vector3(0f, 0.8f, 0f));
            return root;
        }

        public static GameObject Lamp()
        {
            var root = new GameObject("Prop_Lamp");
            Part(root.transform, "Cord", A.Capsule(0.02f, 1.0f), A.Dark, new Vector3(0f, -0.5f, 0f), castShadows: false);
            Part(root.transform, "Shade", A.Mesh("LampShade", () => MeshFactory.Lathe(MeshFactory.DomeProfile(0.38f, 0.32f, 0.04f), 28, "LampShade")),
                A.CheeseYellow, new Vector3(0f, -1.3f, 0f), castShadows: false);
            Part(root.transform, "Bulb", A.SphereMesh, A.Lit("Bulb", Color.white, 0.2f, null, null, new Color(1f, 0.92f, 0.7f)), new Vector3(0f, -1.32f, 0f), null, Vector3.one * 0.2f, false);
            return root;
        }

        public static GameObject Window()
        {
            var root = new GameObject("Prop_Window");
            Part(root.transform, "Frame", A.RoundedBox(1.6f, 1.2f, 0.12f, 0.12f), A.White, Vector3.zero);
            Part(root.transform, "Glass", A.RoundedBox(1.38f, 0.98f, 0.06f, 0.08f), A.Glass, new Vector3(0f, 0f, -0.04f), castShadows: false);
            Part(root.transform, "BarV", A.RoundedBox(0.07f, 0.98f, 0.08f, 0.03f, 2), A.White, new Vector3(0f, 0f, -0.06f), castShadows: false);
            Part(root.transform, "BarH", A.RoundedBox(1.38f, 0.07f, 0.08f, 0.03f, 2), A.White, new Vector3(0f, 0f, -0.06f), castShadows: false);
            Part(root.transform, "Cloud1", A.SphereMesh, A.White, new Vector3(-0.35f, 0.22f, -0.03f), null, new Vector3(0.36f, 0.16f, 0.02f), false);
            Part(root.transform, "Cloud2", A.SphereMesh, A.White, new Vector3(0.3f, -0.18f, -0.03f), null, new Vector3(0.3f, 0.13f, 0.02f), false);
            Part(root.transform, "Sill", A.RoundedBox(1.8f, 0.1f, 0.3f, 0.04f, 2), A.SoftPink, new Vector3(0f, -0.65f, -0.1f));
            return root;
        }

        public static GameObject Bunting(int flags)
        {
            var root = new GameObject("Prop_Bunting");
            float span = 4.4f;
            Material[] mats = { A.CheeseYellow, A.TomatoCoral, A.Mint, A.Sky, A.SoftPink };
            Mesh flag = A.Mesh("Pennant", () => MeshFactory.Pennant(0.36f, 0.42f, 0.02f));
            for (int i = 0; i < flags; i++)
            {
                float t = i / (float)(flags - 1);
                float x = -span * 0.5f + t * span;
                float sag = -Mathf.Sin(t * Mathf.PI) * 0.3f;
                Part(root.transform, "Flag" + i, flag, mats[i % mats.Length], new Vector3(x, sag, 0f), new Vector3(0f, 0f, 0f), null, false);
            }
            for (int i = 0; i < flags - 1; i++)
            {
                float t0 = i / (float)(flags - 1), t1 = (i + 1) / (float)(flags - 1);
                var p0 = new Vector3(-span * 0.5f + t0 * span, -Mathf.Sin(t0 * Mathf.PI) * 0.3f, 0f);
                var p1 = new Vector3(-span * 0.5f + t1 * span, -Mathf.Sin(t1 * Mathf.PI) * 0.3f, 0f);
                Vector3 mid = (p0 + p1) * 0.5f;
                float len = Vector3.Distance(p0, p1);
                float ang = Mathf.Atan2(p1.y - p0.y, p1.x - p0.x) * Mathf.Rad2Deg;
                Part(root.transform, "String" + i, A.Capsule(0.012f, len + 0.02f), A.White, mid, new Vector3(0f, 0f, ang - 90f), null, false);
            }
            return root;
        }

        public static GameObject PictureFrame(string icon, Material frame)
        {
            var root = new GameObject("Prop_Picture_" + icon);
            Part(root.transform, "Frame", A.RoundedBox(0.9f, 0.9f, 0.08f, 0.1f), frame, Vector3.zero);
            Part(root.transform, "Mat", A.RoundedBox(0.74f, 0.74f, 0.04f, 0.06f, 2), A.White, new Vector3(0f, 0f, -0.03f), castShadows: false);
            Sprite s = IconLibrary.Get(icon);
            Material pic = A.Sprite("Picture_" + icon, s != null ? s.texture : null);
            Part(root.transform, "Picture", A.Mesh("PictureQuad", () => MeshFactory.UprightQuad(0.62f, 0.62f)), pic, new Vector3(0f, 0f, -0.06f), castShadows: false);
            return root;
        }

        /// <summary>Original restaurant sign: rounded board, 3D text and a little burger emblem.</summary>
        public static GameObject Sign()
        {
            var root = new GameObject("Prop_Sign");
            Part(root.transform, "Board", A.RoundedBox(3.4f, 0.95f, 0.14f, 0.3f), A.CheeseYellow, Vector3.zero);
            Part(root.transform, "Inner", A.RoundedBox(3.2f, 0.78f, 0.06f, 0.24f), A.White, new Vector3(0f, 0f, -0.06f), castShadows: false);
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            GameObject textGo = Node(root.transform, "Text", new Vector3(0.3f, 0.0f, -0.1f));
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = "Little Burger World";
            tm.font = font;
            tm.fontSize = 96;
            tm.characterSize = 0.028f;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = EditorAssetUtil.Hex("#6E2E12");
            var mr = textGo.GetComponent<MeshRenderer>();
            if (font != null) mr.sharedMaterial = font.material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Emblem: tiny burger stack
            Transform emblem = Node(root.transform, "Emblem", new Vector3(-1.32f, -0.2f, -0.2f), new Vector3(-20f, 0f, 0f)).transform;
            Part(emblem, "Bun", A.Disc(0.64f, 0.26f, 0.11f), A.Bun, Vector3.zero, null, Vector3.one * 0.38f, false);
            Part(emblem, "Patty", A.Disc(0.66f, 0.2f, 0.085f), A.Lit("PattyCooked", EditorAssetUtil.Hex("#7E4A2C"), 0.25f), new Vector3(0f, 0.1f, 0f), null, Vector3.one * 0.38f, false);
            Part(emblem, "Cheese", A.RoundedBox(1.1f, 0.055f, 1.1f, 0.025f, 3), A.Cheese, new Vector3(0f, 0.18f, 0f), new Vector3(0f, 45f, 0f), Vector3.one * 0.38f, false);
            Part(emblem, "Top", A.GetMesh("BunTopDome"), A.BunTop, new Vector3(0f, 0.2f, 0f), null, Vector3.one * 0.38f, false);
            return root;
        }

        public static GameObject Clock()
        {
            var root = new GameObject("Prop_Clock");
            Part(root.transform, "Rim", A.Disc(0.4f, 0.1f, 0.04f, 32), A.TomatoCoral, Vector3.zero, new Vector3(-90f, 0f, 0f));
            Part(root.transform, "Face", A.Disc(0.33f, 0.02f, 0.01f, 32), A.White, new Vector3(0f, 0f, -0.1f), new Vector3(-90f, 0f, 0f), castShadows: false);
            Part(root.transform, "Hour", A.RoundedBox(0.04f, 0.18f, 0.02f, 0.01f, 2), A.Dark, new Vector3(0f, 0.07f, -0.12f), castShadows: false);
            Part(root.transform, "Minute", A.RoundedBox(0.03f, 0.25f, 0.02f, 0.01f, 2), A.Dark, new Vector3(0.09f, 0.0f, -0.12f), new Vector3(0f, 0f, -90f), castShadows: false);
            return root;
        }

        private static void AddBox(GameObject go, Vector3 size, Vector3 center)
        {
            var b = go.AddComponent<BoxCollider>();
            b.size = size;
            b.center = center;
        }
    }
}
