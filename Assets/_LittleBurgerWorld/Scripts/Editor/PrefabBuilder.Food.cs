using System.Collections.Generic;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.Kitchen;
using UnityEngine;
using UnityEngine.Rendering;
using A = LittleBurgerWorld.EditorTools.ArtLibrary;

namespace LittleBurgerWorld.EditorTools
{
    /// <summary>Builds every prefab from procedural meshes. Split across partial files by category.</summary>
    public static partial class PrefabBuilder
    {
        public const string FoodFolder = EditorAssetUtil.Root + "/Prefabs/Food";
        public const string KitchenFolder = EditorAssetUtil.Root + "/Prefabs/Kitchen";
        public const string EnvFolder = EditorAssetUtil.Root + "/Prefabs/Environment";
        public const string CharFolder = EditorAssetUtil.Root + "/Prefabs/Characters";
        public const string UiFolder = EditorAssetUtil.Root + "/Prefabs/UI";
        public const string FxFolder = EditorAssetUtil.Root + "/Prefabs/FX";

        public static GameObject Load(string path) => UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);

        // ------------------------------------------------------------------ helpers
        public static GameObject Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos,
            Vector3? euler = null, Vector3? scale = null, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler ?? Vector3.zero;
            go.transform.localScale = scale ?? Vector3.one;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }

        /// <summary>Soft contact shadow (cheap, GPU-independent alternative to realtime shadows).</summary>
        public static GameObject BlobShadow(Transform parent, Vector3 localPos, float sizeX, float sizeZ)
        {
            return Part(parent, "BlobShadow", A.BlobQuad, A.BlobShadow, localPos + new Vector3(0f, 0.012f, 0f), null,
                new Vector3(sizeX, 1f, sizeZ), false);
        }

        public static GameObject Node(Transform parent, string name, Vector3 pos, Vector3? euler = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler ?? Vector3.zero;
            return go;
        }

        public static void Key(GameObject go, string key)
        {
            var k = go.GetComponent<SceneKey>(); if (k == null) k = go.AddComponent<SceneKey>();
            EditorAssetUtil.Set(k, "m_Key", key);
        }

        private static void Pickable(GameObject go, Vector3 size, Vector3 center)
        {
            go.layer = FoodItem.InteractableLayer;
            var box = go.GetComponent<BoxCollider>(); if (box == null) box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.center = center;
        }

        private static FoodItem MakeFood(GameObject root, FoodType type, float height, float dragHeight = 2.1f)
        {
            var f = root.AddComponent<FoodItem>();
            EditorAssetUtil.Set(f, "m_Type", (int)type);
            EditorAssetUtil.Set(f, "m_Height", height);
            EditorAssetUtil.Set(f, "m_DragHeight", dragHeight);
            return f;
        }

        // ------------------------------------------------------------------ food meshes
        private static Mesh SeedsMesh(string name, float radius, float height, float baseBevel, int count, int seed)
        {
            return A.Mesh(name, () =>
            {
                var rnd = new System.Random(seed);
                Mesh s = MeshFactory.Sphere(0.5f, 8, 6);
                var parts = new List<(Mesh, Matrix4x4)>();
                for (int i = 0; i < count; i++)
                {
                    float a = (float)rnd.NextDouble() * Mathf.PI * 0.42f + 0.25f; // dome latitude
                    float th = (float)rnd.NextDouble() * Mathf.PI * 2f;
                    float x = Mathf.Cos(a) * radius;
                    float y = baseBevel + Mathf.Sin(a) * (height - baseBevel);
                    var pos = new Vector3(Mathf.Cos(th) * x, y, Mathf.Sin(th) * x);
                    Vector3 n = new Vector3(pos.x / (radius * radius), (y - baseBevel) / ((height - baseBevel) * (height - baseBevel)), pos.z / (radius * radius)).normalized;
                    Quaternion rot = Quaternion.FromToRotation(Vector3.up, n) * Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
                    parts.Add((s, Matrix4x4.TRS(pos, rot, new Vector3(0.09f, 0.035f, 0.05f))));
                }
                return MeshFactory.Combine(name, parts.ToArray());
            });
        }

        // ------------------------------------------------------------------ food prefabs
        public static void BuildFood()
        {
            BuildBunBottom();
            BuildPatty();
            BuildCheese();
            BuildLettuce();
            BuildTomato();
            BuildBunTop();
            BuildPickle();
            BuildEgg();
            BuildOnion();
            BuildSauce("Ketchup", A.Ketchup);
            BuildSauce("Mustard", A.Mustard);
            BuildPotato();
            BuildFriesBatch();
            BuildFriesBox();
            BuildMilkshakeCup();
            BuildIceCreamCone();
        }

        private static GameObject IngredientRoot(string name, float colliderHeight, float width = 1.35f)
        {
            var root = new GameObject(name);
            Pickable(root, new Vector3(width, Mathf.Max(0.25f, colliderHeight), width), new Vector3(0f, Mathf.Max(0.25f, colliderHeight) * 0.5f, 0f));
            MakeFood(root, FoodType.Ingredient, colliderHeight);
            return root;
        }

        private static void BuildBunBottom()
        {
            GameObject root = IngredientRoot("Ingredient_BunBottom", 0.26f);
            Part(root.transform, "Bun", A.Disc(0.64f, 0.26f, 0.11f), A.Bun, Vector3.zero);
            Part(root.transform, "Crumb", A.Disc(0.56f, 0.025f, 0.012f), A.BunInside, new Vector3(0f, 0.245f, 0f), castShadows: false);
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Ingredient_BunBottom.prefab");
        }

        private static void BuildPatty()
        {
            GameObject root = IngredientRoot("Ingredient_Patty", 0.2f);
            Mesh m = A.Mesh("Patty", () => MeshFactory.Lathe(MeshFactory.RoundedDiscProfile(0.66f, 0.2f, 0.085f), 40, "Patty",
                (u, v) => Mathf.Sin(u * Mathf.PI * 2f * 7f) * 0.025f + Mathf.Sin(u * Mathf.PI * 2f * 3f) * 0.015f));
            GameObject body = Part(root.transform, "Patty", m, A.Patty, Vector3.zero);
            GameObject marks = Node(root.transform, "GrillMarks", new Vector3(0f, 0.2f, 0f), new Vector3(0f, 35f, 0f));
            for (int i = -1; i <= 1; i++)
            {
                Part(marks.transform, "Mark" + (i + 1), A.RoundedBox(0.95f - Mathf.Abs(i) * 0.25f, 0.025f, 0.07f, 0.012f, 2), A.GrillMark,
                    new Vector3(0f, 0f, i * 0.26f), castShadows: false);
            }
            var cook = root.AddComponent<CookableVisual>();
            EditorAssetUtil.SetArray(cook, "m_Renderers", new Object[] { body.GetComponent<MeshRenderer>() });
            EditorAssetUtil.Set(cook, "m_GrillMarks", marks);
            EditorAssetUtil.Set(cook, "m_RawColor", EditorAssetUtil.Hex("#EE9A93"));
            EditorAssetUtil.Set(cook, "m_CookedColor", EditorAssetUtil.Hex("#7E4A2C"));
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Ingredient_Patty.prefab");
        }

        private static void BuildCheese()
        {
            GameObject root = IngredientRoot("Ingredient_Cheese", 0.06f);
            Part(root.transform, "Slice", A.RoundedBox(1.1f, 0.055f, 1.1f, 0.025f, 3), A.Cheese, new Vector3(0f, 0.028f, 0f));
            Part(root.transform, "DripA", A.Capsule(0.07f, 0.3f), A.Cheese, new Vector3(0.5f, -0.04f, 0.5f), new Vector3(0, 0, 0), castShadows: false);
            Part(root.transform, "DripB", A.Capsule(0.06f, 0.22f), A.Cheese, new Vector3(-0.52f, -0.01f, 0.48f), castShadows: false);
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Ingredient_Cheese.prefab");
        }

        private static void BuildLettuce()
        {
            GameObject root = IngredientRoot("Ingredient_Lettuce", 0.07f, 1.5f);
            Mesh m = A.Mesh("LettuceLeaf", () => MeshFactory.Lathe(MeshFactory.RoundedDiscProfile(0.76f, 0.07f, 0.03f), 72, "LettuceLeaf",
                (u, v) => Mathf.Sin(u * Mathf.PI * 2f * 11f) * 0.06f,
                (u, v) => Mathf.Sin(u * Mathf.PI * 2f * 11f) * 0.035f * (1f - Mathf.Abs(2f * v - 1f))));
            Part(root.transform, "Leaf", m, A.Lettuce, Vector3.zero);
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Ingredient_Lettuce.prefab");
        }

        private static void BuildTomato()
        {
            GameObject root = IngredientRoot("Ingredient_Tomato", 0.08f);
            Part(root.transform, "Slice", A.Disc(0.56f, 0.08f, 0.03f), A.Tomato, Vector3.zero);
            Part(root.transform, "Inner", A.Disc(0.46f, 0.012f, 0.005f), A.TomatoInner, new Vector3(0f, 0.078f, 0f), castShadows: false);
            Mesh seeds = A.Mesh("TomatoSeeds", () =>
            {
                Mesh s = MeshFactory.Sphere(0.5f, 8, 6);
                var parts = new List<(Mesh, Matrix4x4)>();
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                    for (int k = 0; k < 3; k++)
                    {
                        float r = 0.17f + k * 0.07f;
                        parts.Add((s, Matrix4x4.TRS(new Vector3(Mathf.Cos(a + k * 0.15f) * r, 0f, Mathf.Sin(a + k * 0.15f) * r),
                            Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f), new Vector3(0.07f, 0.02f, 0.045f))));
                    }
                }
                return MeshFactory.Combine("TomatoSeeds", parts.ToArray());
            });
            Part(root.transform, "Seeds", seeds, A.Seed, new Vector3(0f, 0.092f, 0f), castShadows: false);
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Ingredient_Tomato.prefab");
        }

        private static void BuildBunTop()
        {
            GameObject root = IngredientRoot("Ingredient_BunTop", 0.48f);
            Mesh dome = A.Mesh("BunTopDome", () => MeshFactory.Lathe(MeshFactory.DomeProfile(0.66f, 0.5f, 0.07f, 12), 40, "BunTopDome"));
            Part(root.transform, "Dome", dome, A.BunTop, Vector3.zero);
            Part(root.transform, "Seeds", SeedsMesh("SesameSeeds", 0.66f, 0.5f, 0.07f, 16, 42), A.Seed, Vector3.zero, castShadows: false);
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Ingredient_BunTop.prefab");
        }

        private static void BuildPickle()
        {
            GameObject root = IngredientRoot("Ingredient_Pickle", 0.05f, 1.2f);
            Vector3[] offsets = { new Vector3(-0.28f, 0f, 0.1f), new Vector3(0.26f, 0.005f, 0.18f), new Vector3(0.02f, 0.01f, -0.28f) };
            for (int i = 0; i < offsets.Length; i++)
            {
                Part(root.transform, "Slice" + i, A.Disc(0.24f, 0.05f, 0.02f, 20), A.Pickle, offsets[i]);
                Part(root.transform, "Inner" + i, A.Disc(0.18f, 0.01f, 0.004f, 20), A.PickleInner, offsets[i] + new Vector3(0f, 0.048f, 0f), castShadows: false);
            }
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Ingredient_Pickle.prefab");
        }

        private static void BuildEgg()
        {
            GameObject root = IngredientRoot("Ingredient_Egg", 0.07f, 1.4f);
            // Wavy fried-egg white with a glossy yolk dome slightly off centre.
            Mesh white = A.Mesh("EggWhite", () => MeshFactory.Lathe(MeshFactory.RoundedDiscProfile(0.62f, 0.06f, 0.03f), 48, "EggWhite",
                (u, v) => Mathf.Sin(u * Mathf.PI * 2f * 3f) * 0.07f + Mathf.Sin(u * Mathf.PI * 2f * 5f + 1f) * 0.035f));
            Part(root.transform, "White", white, A.EggWhite, Vector3.zero);
            Mesh yolk = A.Mesh("EggYolk", () => MeshFactory.Lathe(MeshFactory.DomeProfile(0.24f, 0.12f, 0.02f, 10), 28, "EggYolk"));
            Part(root.transform, "Yolk", yolk, A.Yolk, new Vector3(0.06f, 0.055f, -0.04f), castShadows: false);
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Ingredient_Egg.prefab");
        }

        private static void BuildOnion()
        {
            GameObject root = IngredientRoot("Ingredient_Onion", 0.05f, 1.3f);
            (Vector3 pos, float r)[] rings = { (new Vector3(-0.22f, 0.025f, 0.1f), 0.24f), (new Vector3(0.24f, 0.03f, 0.08f), 0.2f), (new Vector3(0f, 0.035f, -0.24f), 0.22f) };
            for (int i = 0; i < rings.Length; i++)
            {
                float r = rings[i].r;
                Mesh ring = A.Mesh($"OnionRing_{r:0.00}".Replace('.', 'p'), () => MeshFactory.Torus(r, 0.035f, 28, 8));
                Part(root.transform, "Ring" + i, ring, i == 1 ? A.OnionEdge : A.Onion, rings[i].pos);
                Mesh inner = A.Mesh($"OnionRingIn_{r:0.00}".Replace('.', 'p'), () => MeshFactory.Torus(r * 0.62f, 0.028f, 24, 8));
                Part(root.transform, "Inner" + i, inner, A.Onion, rings[i].pos + new Vector3(0f, 0.005f, 0f), castShadows: false);
            }
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Ingredient_Onion.prefab");
        }

        /// <summary>A playful zig-zag of sauce (ketchup / mustard) that lies on top of the stack.</summary>
        private static void BuildSauce(string name, Material mat)
        {
            GameObject root = IngredientRoot("Ingredient_" + name, 0.04f, 1.2f);
            Mesh zig = A.Mesh("SauceZigzag", () =>
            {
                var parts = new List<(Mesh, Matrix4x4)>();
                Mesh blob = MeshFactory.Sphere(0.5f, 10, 8);
                int n = 48;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (n - 1f);
                    float x = Mathf.Lerp(-0.42f, 0.42f, t);
                    float z = Mathf.Sin(t * Mathf.PI * 5f) * 0.3f * Mathf.Sqrt(1f - Mathf.Pow(2f * t - 1f, 2f) * 0.6f);
                    parts.Add((blob, Matrix4x4.TRS(new Vector3(x, 0.02f, z), Quaternion.identity, new Vector3(0.09f, 0.05f, 0.09f))));
                }
                return MeshFactory.Combine("SauceZigzag", parts.ToArray());
            });
            Part(root.transform, "Sauce", zig, mat, Vector3.zero, castShadows: false);
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Ingredient_{name}.prefab");
        }

        private static void BuildPotato()
        {
            var root = new GameObject("Food_Potato");
            Part(root.transform, "Potato", A.SphereMesh, A.Potato, new Vector3(0f, 0.2f, 0f), new Vector3(0f, 0f, 12f), new Vector3(0.62f, 0.46f, 0.5f));
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Food_Potato.prefab");
        }

        private static Mesh FryStick => A.RoundedBox(0.1f, 0.62f, 0.1f, 0.035f, 2);

        private static void BuildFriesBatch()
        {
            var root = new GameObject("Food_FriesBatch");
            Pickable(root, new Vector3(1.2f, 0.6f, 1f), new Vector3(0f, 0.3f, 0f));
            MakeFood(root, FoodType.FriesBatch, 0.35f);
            Mesh basket = A.Mesh("FryBasket", () => MeshFactory.Lathe(new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(0.42f, 0f), new Vector2(0.5f, 0.05f), new Vector2(0.55f, 0.2f),
                new Vector2(0.5f, 0.22f), new Vector2(0.44f, 0.08f), new Vector2(0f, 0.08f)
            }, 32, "FryBasket"));
            Part(root.transform, "Basket", basket, A.Sky, Vector3.zero);
            Mesh pile = A.Mesh("FriesPile", () =>
            {
                var rnd = new System.Random(9);
                var parts = new List<(Mesh, Matrix4x4)>();
                Mesh stick = MeshFactory.RoundedBox(new Vector3(0.1f, 0.62f, 0.1f), 0.035f, 2);
                for (int i = 0; i < 14; i++)
                {
                    var pos = new Vector3((float)rnd.NextDouble() * 0.6f - 0.3f, 0.14f + (i / 14f) * 0.12f, (float)rnd.NextDouble() * 0.5f - 0.25f);
                    var rot = Quaternion.Euler(80f + rnd.Next(-15, 15), rnd.Next(0, 360), rnd.Next(-20, 20));
                    parts.Add((stick, Matrix4x4.TRS(pos, rot, Vector3.one)));
                }
                return MeshFactory.Combine("FriesPile", parts.ToArray());
            });
            Part(root.transform, "Fries", pile, A.Fries, Vector3.zero);
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Food_FriesBatch.prefab");
        }

        private static void BuildFriesBox()
        {
            var root = new GameObject("Food_FriesBox");
            Pickable(root, new Vector3(0.9f, 1.1f, 0.75f), new Vector3(0f, 0.55f, 0f));
            MakeFood(root, FoodType.Fries, 0.65f);
            Mesh box = A.Mesh("FriesBoxBody", () =>
            {
                Mesh b = MeshFactory.RoundedBox(new Vector3(0.62f, 0.64f, 0.44f), 0.08f, 3);
                // Taper: widen the top for the classic paper-box silhouette.
                Vector3[] v = b.vertices;
                for (int i = 0; i < v.Length; i++)
                {
                    float k = Mathf.InverseLerp(-0.32f, 0.32f, v[i].y);
                    v[i].x *= Mathf.Lerp(0.85f, 1.12f, k);
                    v[i].z *= Mathf.Lerp(0.9f, 1.08f, k);
                    v[i].y += 0.32f;
                }
                b.vertices = v;
                b.RecalculateBounds();
                return b;
            });
            Part(root.transform, "Box", box, A.FriesBox, Vector3.zero);
            Part(root.transform, "Band", A.RoundedBox(0.72f, 0.07f, 0.5f, 0.03f, 2), A.CreamMat, new Vector3(0f, 0.6f, 0f), castShadows: false);
            Part(root.transform, "Badge", A.Disc(0.13f, 0.03f, 0.012f, 20), A.CheeseYellow, new Vector3(0f, 0.32f, -0.235f), new Vector3(-90f, 0f, 0f), castShadows: false);
            GameObject fill = Node(root.transform, "Fill", Vector3.zero);
            var rnd = new System.Random(21);
            for (int i = 0; i < 11; i++)
            {
                float x = -0.25f + (i % 6) * 0.1f + (float)rnd.NextDouble() * 0.03f;
                float z = i < 6 ? -0.06f : 0.08f;
                float y = 0.62f + (float)rnd.NextDouble() * 0.12f;
                Part(fill.transform, "Fry" + i, FryStick, A.Fries, new Vector3(x, y, z), new Vector3(rnd.Next(-8, 8), rnd.Next(0, 90), rnd.Next(-14, 14)), castShadows: false);
            }
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Food_FriesBox.prefab");
        }

        public static readonly (string id, string name, string color)[] IceCreamFlavours =
        {
            ("strawberry", "Strawberry", "#FF9EC1"), ("chocolate", "Chocolate", "#8B5A3C"), ("vanilla", "Vanilla", "#FFF1C9")
        };

        private static void BuildIceCreamCone()
        {
            var root = new GameObject("Food_IceCream");
            Pickable(root, new Vector3(0.62f, 1.5f, 0.62f), new Vector3(0f, 0.75f, 0f));
            MakeFood(root, FoodType.IceCream, 1.1f, 2.3f);
            Mesh cone = A.Mesh("WaffleCone", () => MeshFactory.Lathe(new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(0.04f, 0.02f), new Vector2(0.24f, 0.62f), new Vector2(0.27f, 0.66f), new Vector2(0.25f, 0.7f), new Vector2(0f, 0.7f)
            }, 28, "WaffleCone"));
            Part(root.transform, "Cone", cone, A.Waffle, Vector3.zero);
            for (int i = 0; i < 3; i++)
            {
                float y = 0.16f + i * 0.16f;
                float r = 0.04f + (y / 0.62f) * 0.2f + 0.008f;
                Mesh ring = A.Mesh($"WaffleLine{i}", () => MeshFactory.Torus(r, 0.012f, 24, 5));
                Part(root.transform, "Line" + i, ring, A.WaffleDark, new Vector3(0f, y, 0f), castShadows: false);
            }
            var scoops = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject s = Part(root.transform, "Scoop" + i, A.SphereMesh, A.Scoop, new Vector3(0f, 0.8f + i * 0.27f, 0f), null,
                    new Vector3(0.56f - i * 0.03f, 0.46f, 0.56f - i * 0.03f));
                scoops[i] = s.GetComponent<MeshRenderer>();
                s.SetActive(false);
            }
            GameObject sprinkles = Node(root.transform, "Sprinkles", new Vector3(0f, 0.8f, 0f));
            Material[] sm = { A.SprinklePink, A.SprinkleBlue, A.SprinkleYellow, A.SprinkleGreen };
            var rnd = new System.Random(5);
            for (int i = 0; i < 16; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float h = 0.35f + (float)rnd.NextDouble() * 0.6f; // 0..1 up the dome
                float rr = Mathf.Sqrt(1f - h * h) * 0.27f;
                Vector3 pos = new Vector3(Mathf.Cos(a) * rr, h * 0.23f, Mathf.Sin(a) * rr);
                Part(sprinkles.transform, "S" + i, A.Capsule(0.018f, 0.09f), sm[i % sm.Length], pos,
                    new Vector3(rnd.Next(0, 180), rnd.Next(0, 360), rnd.Next(0, 180)), Vector3.one, false);
            }
            sprinkles.SetActive(false);
            GameObject cherry = Node(root.transform, "Cherry", new Vector3(0f, 0.97f, 0f));
            Part(cherry.transform, "Berry", A.SphereMesh, A.CherryRed, new Vector3(0f, 0.08f, 0f), null, Vector3.one * 0.19f);
            Part(cherry.transform, "Stem", A.Capsule(0.012f, 0.16f), A.Lettuce, new Vector3(0.03f, 0.22f, 0f), new Vector3(0f, 0f, -20f), Vector3.one, false);
            cherry.SetActive(false);
            var vis = root.AddComponent<IceCreamVisual>();
            EditorAssetUtil.SetArray(vis, "m_Scoops", scoops);
            EditorAssetUtil.Set(vis, "m_Sprinkles", sprinkles.transform);
            EditorAssetUtil.Set(vis, "m_Cherry", cherry.transform);
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Food_IceCream.prefab");
        }

        private static void BuildMilkshakeCup()
        {
            var root = new GameObject("Food_MilkshakeCup");
            Pickable(root, new Vector3(0.95f, 1.5f, 0.95f), new Vector3(0f, 0.75f, 0f));
            MakeFood(root, FoodType.Milkshake, 1f, 2.3f);
            Mesh cup = A.Mesh("ShakeCup", () => MeshFactory.Lathe(new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(0.28f, 0f), new Vector2(0.31f, 0.03f), new Vector2(0.42f, 1.0f),
                new Vector2(0.44f, 1.03f), new Vector2(0.41f, 1.05f), new Vector2(0.38f, 1.0f), new Vector2(0.28f, 0.07f), new Vector2(0f, 0.07f)
            }, 36, "ShakeCup"));
            Part(root.transform, "Cup", cup, A.CupWhite, Vector3.zero);
            Mesh band = A.Mesh("ShakeCupBand", () => MeshFactory.Lathe(new List<Vector2>
            {
                new Vector2(0.345f, 0.4f), new Vector2(0.385f, 0.75f)
            }, 36, "ShakeCupBand", (u, v) => 0.025f));
            Part(root.transform, "Band", band, A.SoftPink, Vector3.zero, castShadows: false);
            GameObject liquid = Part(root.transform, "Liquid", A.Mesh("ShakeLiquid", () => MeshFactory.Lathe(new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(0.3f, 0f), new Vector2(0.385f, 0.92f), new Vector2(0f, 0.92f)
            }, 32, "ShakeLiquid")), A.Liquid, new Vector3(0f, 0.07f, 0f), castShadows: false);
            Mesh swirl = A.Mesh("CreamSwirl", () => MeshFactory.Combine("CreamSwirl",
                (MeshFactory.Torus(0.28f, 0.14f, 24, 10), Matrix4x4.TRS(new Vector3(0f, 0.08f, 0f), Quaternion.identity, Vector3.one)),
                (MeshFactory.Torus(0.18f, 0.12f, 20, 10), Matrix4x4.TRS(new Vector3(0f, 0.25f, 0f), Quaternion.identity, Vector3.one)),
                (MeshFactory.Sphere(0.14f, 14, 10), Matrix4x4.TRS(new Vector3(0f, 0.38f, 0f), Quaternion.identity, new Vector3(1f, 1.3f, 1f)))));
            GameObject cream = Part(root.transform, "Cream", swirl, A.Cream, new Vector3(0f, 0.98f, 0f));
            Part(cream.transform, "Cherry", A.SphereMesh, A.Tomato, new Vector3(0.02f, 0.55f, 0f), null, Vector3.one * 0.2f);
            GameObject straw = Part(root.transform, "Straw", A.Capsule(0.05f, 1.2f), A.Straw, new Vector3(0.14f, 1.35f, 0.05f), new Vector3(0f, 0f, -14f));
            var vis = root.AddComponent<MilkshakeVisual>();
            EditorAssetUtil.Set(vis, "m_Liquid", liquid.transform);
            EditorAssetUtil.Set(vis, "m_LiquidRenderer", liquid.GetComponent<MeshRenderer>());
            EditorAssetUtil.Set(vis, "m_Cream", cream);
            EditorAssetUtil.Set(vis, "m_CreamRenderer", cream.GetComponent<MeshRenderer>());
            EditorAssetUtil.Set(vis, "m_Straw", straw);
            EditorAssetUtil.SavePrefab(root, $"{FoodFolder}/Food_MilkshakeCup.prefab");
        }
    }
}
