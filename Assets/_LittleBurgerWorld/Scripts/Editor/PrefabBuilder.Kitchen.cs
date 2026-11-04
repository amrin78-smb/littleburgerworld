using System.Collections.Generic;
using LittleBurgerWorld.Audio;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.Kitchen;
using UnityEngine;
using A = LittleBurgerWorld.EditorTools.ArtLibrary;

namespace LittleBurgerWorld.EditorTools
{
    public static partial class PrefabBuilder
    {
        public const float CounterHeight = 0.9f;

        public static void BuildKitchen(IList<DrinkData> drinks)
        {
            BuildCounter("Counter_2m", 2f, A.Mint);
            BuildBoard();
            BuildGrill();
            BuildFryer();
            BuildFriesHolder();
            BuildDrinks(drinks);
            BuildTray();
            BuildIceCreamCart();
        }

        /// <summary>A little wheeled ice cream cart with a striped umbrella (stands on the floor).</summary>
        /// <summary>
        /// Above the cone's food priority (13): the cone stands in front of the tubs, so a tap that touches
        /// both must scoop rather than grab or tap the cone.
        /// </summary>
        public const int CartButtonPriority = 15;
        // Tubs sit at the back, so the cone stack can overlap them on screen too; scooping wins
        // (tapping a tub without a cone places one by itself, so the stack is never needed).

        private static void BuildIceCreamCart()
        {
            var root = new GameObject("Station_IceCream");
            BlobShadow(root.transform, Vector3.zero, 2.6f, 1.6f);
            const float top = 1.15f;
            Part(root.transform, "Body", A.RoundedBox(2.0f, 0.85f, 1.05f, 0.16f), A.SoftPink, new Vector3(0f, 0.3f + 0.425f, 0f));
            Part(root.transform, "Stripe", A.RoundedBox(2.04f, 0.14f, 1.09f, 0.06f), A.White, new Vector3(0f, 0.86f, 0f), castShadows: false);
            Part(root.transform, "TopSlab", A.RoundedBox(2.1f, 0.08f, 1.15f, 0.04f), A.White, new Vector3(0f, top - 0.04f, 0f));
            Face(root.transform, new Vector3(0f, 0.6f, -0.53f), 0.2f, 1.0f, Vector3.zero);
            foreach (float x in new[] { -0.75f, 0.75f })
                foreach (float z in new[] { -0.4f, 0.4f })
                    Part(root.transform, "Wheel", A.Disc(0.17f, 0.1f, 0.04f, 20), A.Sky, new Vector3(x, 0.17f, z), new Vector3(0f, 0f, 90f));
            // Little cone topper on the back edge instead of an umbrella (keeps the serving counter visible).
            GameObject sign = Node(root.transform, "Topper", new Vector3(0.95f, top, 0.42f));
            Part(sign.transform, "Post", A.Capsule(0.03f, 0.5f), A.White, new Vector3(0f, 0.25f, 0f), castShadows: false);
            Part(sign.transform, "Cone", A.GetMesh("WaffleCone"), A.Waffle, new Vector3(0f, 0.5f, 0f), null, Vector3.one * 0.55f, false);
            Part(sign.transform, "Scoop", A.SphereMesh, A.Lit("IceCream_strawberry", EditorAssetUtil.Hex("#FF9EC1"), 0.35f),
                new Vector3(0f, 0.92f, 0f), null, Vector3.one * 0.3f, false);
            // Flavour tubs along the back
            var tubs = new List<Object>();
            var ids = new List<string>();
            var colors = new List<Color>();
            Mesh tubMesh = A.Mesh("IceCreamTub", () => MeshFactory.Lathe(new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(0.2f, 0f), new Vector2(0.24f, 0.24f), new Vector2(0.26f, 0.27f), new Vector2(0.22f, 0.27f), new Vector2(0.21f, 0.2f), new Vector2(0f, 0.2f)
            }, 28, "IceCreamTub"));
            for (int i = 0; i < IceCreamFlavours.Length; i++)
            {
                var f = IceCreamFlavours[i];
                GameObject tub = Node(root.transform, "Tub_" + f.id, new Vector3(-0.62f + i * 0.5f, top, 0.24f));
                GameObject tv = Node(tub.transform, "Visual", Vector3.zero);
                Part(tv.transform, "Tub", tubMesh, A.White, Vector3.zero);
                Material m = A.Lit("IceCream_" + f.id, EditorAssetUtil.Hex(f.color), 0.35f);
                Part(tv.transform, "Cream", A.SphereMesh, m, new Vector3(0f, 0.22f, 0f), null, new Vector3(0.44f, 0.26f, 0.44f));
                Part(tv.transform, "Scoop", A.SphereMesh, m, new Vector3(0.05f, 0.33f, -0.02f), null, new Vector3(0.22f, 0.18f, 0.22f), false);
                var c = tub.AddComponent<SphereCollider>();
                c.radius = 0.4f;
                c.center = new Vector3(0f, 0.25f, 0f);
                tubs.Add(Button3D(tub, CartButtonPriority + 1, SfxId.Pop, tv.transform));
                Key(tub, "IceCream.Tub." + f.id);
                ids.Add(f.id);
                colors.Add(EditorAssetUtil.Hex(f.color));
            }
            // Cone stack (front left)
            GameObject cones = Node(root.transform, "ConeStack", new Vector3(-0.72f, top, -0.27f));
            GameObject cv = Node(cones.transform, "Visual", Vector3.zero);
            Part(cv.transform, "Holder", A.Disc(0.2f, 0.06f, 0.02f, 20), A.Sky, Vector3.zero);
            for (int i = 0; i < 3; i++)
                Part(cv.transform, "Cone" + i, A.GetMesh("WaffleCone"), A.Waffle, new Vector3(0f, 0.75f - i * 0.12f, 0f), new Vector3(180f, 0f, 0f), Vector3.one * 0.85f);
            var cc = cones.AddComponent<BoxCollider>();
            cc.size = new Vector3(0.6f, 0.9f, 0.6f);
            cc.center = new Vector3(0f, 0.4f, 0f);
            TapButton3D coneButton = Button3D(cones, CartButtonPriority, SfxId.Pop, cv.transform);
            Key(cones, "IceCream.Cones");
            GameObject spawn = Node(root.transform, "ConeSpawn", new Vector3(-0.72f, top + 0.9f, -0.27f));
            // Cone holder (front middle)
            GameObject holder = Node(root.transform, "Holder", new Vector3(-0.1f, top, -0.25f));
            Part(holder.transform, "Ring", A.Mesh("ConeHolderRing", () => MeshFactory.Torus(0.19f, 0.04f, 24, 8)), A.Sky, new Vector3(0f, 0.36f, 0f));
            Part(holder.transform, "Post", A.Capsule(0.04f, 0.34f), A.Sky, new Vector3(0.2f, 0.17f, 0f), castShadows: false);
            GameObject spot = Node(holder.transform, "Spot", new Vector3(0f, 0.02f, 0f));
            Key(holder, "IceCream.Holder");
            // Sprinkle shaker
            GameObject shaker = Node(root.transform, "Sprinkles", new Vector3(0.42f, top, -0.3f));
            GameObject sv = Node(shaker.transform, "Visual", Vector3.zero);
            Part(sv.transform, "Jar", A.Capsule(0.15f, 0.5f), A.Glass, new Vector3(0f, 0.25f, 0f));
            Part(sv.transform, "Lid", A.Disc(0.16f, 0.08f, 0.03f, 20), A.SprinklePink, new Vector3(0f, 0.48f, 0f));
            Material[] sm = { A.SprinklePink, A.SprinkleBlue, A.SprinkleYellow, A.SprinkleGreen };
            for (int i = 0; i < 8; i++)
                Part(sv.transform, "Bit" + i, A.Capsule(0.02f, 0.08f), sm[i % 4], new Vector3(((i * 37) % 9 - 4) * 0.02f, 0.08f + (i % 4) * 0.07f, -0.07f),
                    new Vector3(0f, 0f, i * 40f), Vector3.one, false);
            var shc = shaker.AddComponent<BoxCollider>();
            shc.size = new Vector3(0.55f, 0.75f, 0.55f);
            shc.center = new Vector3(0f, 0.3f, 0f);
            TapButton3D shakerButton = Button3D(shaker, CartButtonPriority, SfxId.Sparkle, sv.transform);
            Key(shaker, "IceCream.Sprinkles");
            // Cherry bowl
            GameObject bowl = Node(root.transform, "Cherries", new Vector3(0.82f, top, -0.12f));
            GameObject bv = Node(bowl.transform, "Visual", Vector3.zero);
            Part(bv.transform, "Bowl", A.Mesh("CherryBowl", () => MeshFactory.Lathe(new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(0.12f, 0f), new Vector2(0.22f, 0.12f), new Vector2(0.24f, 0.16f), new Vector2(0.2f, 0.15f), new Vector2(0f, 0.06f)
            }, 24, "CherryBowl")), A.White, Vector3.zero);
            for (int i = 0; i < 4; i++)
                Part(bv.transform, "Cherry" + i, A.SphereMesh, A.CherryRed, new Vector3(Mathf.Cos(i * 1.6f) * 0.08f, 0.16f + (i == 3 ? 0.07f : 0f), Mathf.Sin(i * 1.6f) * 0.08f),
                    null, Vector3.one * 0.13f, false);
            var bc = bowl.AddComponent<SphereCollider>();
            bc.radius = 0.3f;
            bc.center = new Vector3(0f, 0.15f, 0f);
            TapButton3D cherryButton = Button3D(bowl, CartButtonPriority, SfxId.Pop, bv.transform);
            Key(bowl, "IceCream.Cherry");

            var station = root.AddComponent<IceCreamStation>();
            var so = new UnityEditor.SerializedObject(station);
            SerializedStrings(so, "m_FlavourIds", ids);
            SerializedColors(so, "m_FlavourColors", colors);
            SetList(so, "m_Tubs", tubs);
            so.FindProperty("m_Cones").objectReferenceValue = coneButton;
            so.FindProperty("m_SprinkleShaker").objectReferenceValue = shakerButton;
            so.FindProperty("m_CherryJar").objectReferenceValue = cherryButton;
            so.FindProperty("m_Spot").objectReferenceValue = spot.transform;
            so.FindProperty("m_Spawn").objectReferenceValue = spawn.transform;
            so.FindProperty("m_ConePrefab").objectReferenceValue = Load($"{FoodFolder}/Food_IceCream.prefab");
            so.ApplyModifiedPropertiesWithoutUndo();
            Key(root, "IceCream");
            EditorAssetUtil.SavePrefab(root, $"{KitchenFolder}/Station_IceCream.prefab");
        }

        private static void SerializedStrings(UnityEditor.SerializedObject so, string field, List<string> values)
        {
            UnityEditor.SerializedProperty p = so.FindProperty(field);
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        private static void SerializedColors(UnityEditor.SerializedObject so, string field, List<Color> values)
        {
            UnityEditor.SerializedProperty p = so.FindProperty(field);
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).colorValue = values[i];
        }

        private static TapButton3D Button3D(GameObject go, int priority, SfxId sound, Transform visual = null)
        {
            go.layer = FoodItem.InteractableLayer;
            var b = go.AddComponent<TapButton3D>();
            EditorAssetUtil.Set(b, "m_Priority", priority);
            EditorAssetUtil.Set(b, "m_Sound", (int)sound);
            if (visual != null) EditorAssetUtil.Set(b, "m_Visual", visual);
            return b;
        }

        /// <summary>Counter with a cabinet base, rounded top and little handles. Pivot at floor, front faces -Z.</summary>
        public static GameObject CounterObject(string name, float length, Material top, float depth = 1.2f)
        {
            var root = new GameObject(name);
            Part(root.transform, "Cabinet", A.RoundedBox(length - 0.06f, CounterHeight - 0.08f, depth - 0.1f, 0.08f), A.CreamMat,
                new Vector3(0f, (CounterHeight - 0.08f) * 0.5f, 0f));
            Part(root.transform, "Top", A.RoundedBox(length, 0.12f, depth, 0.05f), top, new Vector3(0f, CounterHeight - 0.06f, 0f));
            int doors = Mathf.Max(1, Mathf.RoundToInt(length / 1f));
            for (int i = 0; i < doors; i++)
            {
                float x = -length * 0.5f + (i + 0.5f) * (length / doors);
                Part(root.transform, "Door" + i, A.RoundedBox(length / doors - 0.16f, 0.56f, 0.04f, 0.03f, 2), A.White,
                    new Vector3(x, 0.42f, -depth * 0.5f + 0.03f), castShadows: false);
                Part(root.transform, "Knob" + i, A.SphereMesh, A.TomatoCoral, new Vector3(x, 0.55f, -depth * 0.5f - 0.01f), null, Vector3.one * 0.09f, false);
            }
            BlobShadow(root.transform, Vector3.zero, length + 0.7f, depth + 0.7f);
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(length, CounterHeight, depth);
            col.center = new Vector3(0f, CounterHeight * 0.5f, 0f);
            return root;
        }

        private static void BuildCounter(string name, float length, Material top)
        {
            EditorAssetUtil.SavePrefab(CounterObject(name, length, top), $"{KitchenFolder}/{name}.prefab");
        }

        private static void BuildBoard()
        {
            var root = new GameObject("BurgerBoard");
            BlobShadow(root.transform, Vector3.zero, 2.3f, 2.3f);
            Part(root.transform, "Board", A.Disc(0.95f, 0.08f, 0.035f, 40), A.Wood, Vector3.zero);
            Part(root.transform, "Rim", A.Mesh("BoardRim", () => MeshFactory.Torus(0.93f, 0.035f, 40, 8)), A.WoodDark, new Vector3(0f, 0.07f, 0f), castShadows: false);
            Part(root.transform, "Handle", A.RoundedBox(0.4f, 0.07f, 0.22f, 0.03f), A.Wood, new Vector3(-1.05f, 0.035f, 0f));
            GameObject stackBase = Node(root.transform, "StackBase", new Vector3(0f, 0.08f, 0f));
            var board = root.AddComponent<BurgerAssemblyController>();
            EditorAssetUtil.Set(board, "m_StackBase", stackBase.transform);
            EditorAssetUtil.Set(board, "m_SnapPoint", stackBase.transform);
            EditorAssetUtil.Set(board, "m_AcceptRadius", 1.4f);
            EditorAssetUtil.Set(board, "m_RingRadius", 1.05f);
            EditorAssetUtil.Set(board, "m_DropPriority", 12);
            Key(root, "Board");
            EditorAssetUtil.SavePrefab(root, $"{KitchenFolder}/BurgerBoard.prefab");
        }

        private static void BuildGrill()
        {
            var root = new GameObject("Station_Grill");
            BlobShadow(root.transform, Vector3.zero, 2.5f, 1.6f);
            Part(root.transform, "Body", A.RoundedBox(2.1f, 0.34f, 1.25f, 0.12f), A.Sky, new Vector3(0f, 0.17f, 0f));
            Part(root.transform, "Plate", A.RoundedBox(1.9f, 0.08f, 1.05f, 0.04f), A.GrillTop, new Vector3(0f, 0.36f, 0f));
            Part(root.transform, "Glow", A.RoundedBox(1.7f, 0.03f, 0.85f, 0.015f, 2), A.Glow, new Vector3(0f, 0.405f, 0f), castShadows: false);
            for (int i = 0; i < 7; i++)
            {
                Part(root.transform, "Bar" + i, A.RoundedBox(0.07f, 0.06f, 1.0f, 0.03f, 2), A.GrillBar,
                    new Vector3(-0.78f + i * 0.26f, 0.44f, 0f), castShadows: false);
            }
            for (int i = 0; i < 3; i++)
            {
                Part(root.transform, "Knob" + i, A.Disc(0.1f, 0.08f, 0.03f, 16), i == 1 ? A.CheeseYellow : A.TomatoCoral,
                    new Vector3(-0.5f + i * 0.5f, 0.17f, -0.65f), new Vector3(-90f, 0f, 0f), castShadows: false);
            }
            // friendly "toy" handles on the sides
            Part(root.transform, "HandleL", A.Mesh("GrillHandle", () => MeshFactory.Torus(0.14f, 0.04f, 16, 8, 0.5f)), A.White,
                new Vector3(-1.05f, 0.22f, 0f), new Vector3(0f, 180f, 0f), castShadows: false);
            Part(root.transform, "HandleR", A.Mesh("GrillHandle", () => MeshFactory.Torus(0.14f, 0.04f, 16, 8, 0.5f)), A.White,
                new Vector3(1.05f, 0.22f, 0f), new Vector3(0f, 0f, 0f), castShadows: false);
            GameObject a = Node(root.transform, "SpotA", new Vector3(-0.47f, 0.48f, 0f));
            GameObject b = Node(root.transform, "SpotB", new Vector3(0.47f, 0.48f, 0f));
            var grill = root.AddComponent<GrillStation>();
            var so = new UnityEditor.SerializedObject(grill);
            var spots = so.FindProperty("m_Spots");
            spots.arraySize = 2;
            spots.GetArrayElementAtIndex(0).FindPropertyRelative("anchor").objectReferenceValue = a.transform;
            spots.GetArrayElementAtIndex(1).FindPropertyRelative("anchor").objectReferenceValue = b.transform;
            so.FindProperty("m_SnapPoint").objectReferenceValue = a.transform;
            so.FindProperty("m_AcceptRadius").floatValue = 1.35f;
            so.FindProperty("m_RingRadius").floatValue = 1.1f;
            so.FindProperty("m_DropPriority").intValue = 12;
            so.ApplyModifiedPropertiesWithoutUndo();
            Key(root, "Grill");
            EditorAssetUtil.SavePrefab(root, $"{KitchenFolder}/Station_Grill.prefab");
        }

        private static void Face(Transform parent, Vector3 center, float spacing, float scale, Vector3 euler)
        {
            GameObject face = Node(parent, "Face", center, euler);
            Part(face.transform, "EyeL", A.SphereMesh, A.Dark, new Vector3(-spacing, 0f, 0f), null, new Vector3(0.12f, 0.16f, 0.06f) * scale, false);
            Part(face.transform, "EyeR", A.SphereMesh, A.Dark, new Vector3(spacing, 0f, 0f), null, new Vector3(0.12f, 0.16f, 0.06f) * scale, false);
            Part(face.transform, "ShineL", A.SphereMesh, A.EyeShine, new Vector3(-spacing - 0.02f * scale, 0.04f * scale, -0.03f * scale), null, Vector3.one * 0.04f * scale, false);
            Part(face.transform, "ShineR", A.SphereMesh, A.EyeShine, new Vector3(spacing - 0.02f * scale, 0.04f * scale, -0.03f * scale), null, Vector3.one * 0.04f * scale, false);
            Part(face.transform, "Smile", A.SmileArc, A.Dark, new Vector3(0f, -0.06f * scale, -0.02f), null, Vector3.one * scale, false);
            Part(face.transform, "CheekL", A.SphereMesh, A.Blush, new Vector3(-spacing * 1.7f, -0.08f * scale, 0.01f), null, new Vector3(0.12f, 0.07f, 0.03f) * scale, false);
            Part(face.transform, "CheekR", A.SphereMesh, A.Blush, new Vector3(spacing * 1.7f, -0.08f * scale, 0.01f), null, new Vector3(0.12f, 0.07f, 0.03f) * scale, false);
        }

        private static void BuildFryer()
        {
            var root = new GameObject("Station_Fryer");
            // Happy fryer vat
            BlobShadow(root.transform, Vector3.zero, 1.6f, 1.4f);
            Part(root.transform, "Vat", A.RoundedBox(1.15f, 0.7f, 1.0f, 0.16f), A.CheeseYellow, new Vector3(0f, 0.35f, 0f));
            Part(root.transform, "Oil", A.RoundedBox(0.9f, 0.04f, 0.76f, 0.02f, 2), A.Oil, new Vector3(0f, 0.68f, 0f), castShadows: false);
            Face(root.transform, new Vector3(0f, 0.38f, -0.505f), 0.17f, 1f, Vector3.zero);
            GameObject basket = Node(root.transform, "FryBasket", new Vector3(0f, 0.72f, 0f));
            Part(basket.transform, "Rim", A.Mesh("FryerRim", () => MeshFactory.Torus(0.33f, 0.035f, 24, 8)), A.Sky, Vector3.zero, castShadows: false);
            Part(basket.transform, "Handle", A.Capsule(0.04f, 0.6f), A.Sky, new Vector3(0f, 0.12f, -0.5f), new Vector3(70f, 0f, 0f));
            Part(basket.transform, "HandleGrip", A.Capsule(0.07f, 0.24f), A.TomatoCoral, new Vector3(0f, 0.22f, -0.76f), new Vector3(70f, 0f, 0f));
            GameObject fryerPoint = Node(root.transform, "FryerPoint", new Vector3(0f, 0.75f, 0f));

            // Potato basket (tap me!)
            GameObject potatoes = Node(root.transform, "PotatoBasket", new Vector3(-1.15f, 0f, 0.05f));
            GameObject pv = Node(potatoes.transform, "Visual", Vector3.zero);
            Mesh bowl = A.Mesh("WovenBasket", () => MeshFactory.Lathe(new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(0.36f, 0f), new Vector2(0.48f, 0.3f), new Vector2(0.52f, 0.36f),
                new Vector2(0.46f, 0.38f), new Vector2(0.4f, 0.1f), new Vector2(0f, 0.1f)
            }, 28, "WovenBasket", (u, v) => Mathf.Sin(u * Mathf.PI * 2f * 14f) * 0.015f));
            Part(pv.transform, "Basket", bowl, A.WoodDark, Vector3.zero);
            var rnd = new System.Random(4);
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                Part(pv.transform, "Potato" + i, A.SphereMesh, A.Potato, new Vector3(Mathf.Cos(a) * 0.22f, 0.3f + (i % 2) * 0.05f, Mathf.Sin(a) * 0.2f),
                    new Vector3(rnd.Next(0, 40), rnd.Next(0, 360), 0f), new Vector3(0.34f, 0.26f, 0.28f));
            }
            Part(pv.transform, "PotatoTop", A.SphereMesh, A.Potato, new Vector3(0f, 0.42f, 0f), new Vector3(10f, 30f, 0f), new Vector3(0.36f, 0.27f, 0.3f));
            var col = potatoes.AddComponent<BoxCollider>();
            col.size = new Vector3(1.3f, 0.9f, 1.3f);
            col.center = new Vector3(0f, 0.35f, 0f);
            TapButton3D potatoButton = Button3D(potatoes, 7, SfxId.Pop, pv.transform);
            Key(potatoes, "Fryer.Potatoes");
            GameObject potatoSpawn = Node(potatoes.transform, "Spawn", new Vector3(0f, 0.5f, 0f));

            // Batch landing plate
            GameObject plate = Node(root.transform, "BatchPlate", new Vector3(1.1f, 0f, -0.05f));
            Part(plate.transform, "Plate", A.Disc(0.5f, 0.05f, 0.02f, 28), A.White, Vector3.zero);
            GameObject batchSpot = Node(plate.transform, "BatchSpot", new Vector3(0f, 0.05f, 0f));
            Key(plate, "Fryer.Batch");

            var fry = root.AddComponent<FryStation>();
            EditorAssetUtil.Set(fry, "m_PotatoBasket", potatoButton);
            EditorAssetUtil.Set(fry, "m_PotatoSpawn", potatoSpawn.transform);
            EditorAssetUtil.Set(fry, "m_FryerPoint", fryerPoint.transform);
            EditorAssetUtil.Set(fry, "m_BatchSpot", batchSpot.transform);
            EditorAssetUtil.Set(fry, "m_FryerBasketVisual", basket.transform);
            EditorAssetUtil.Set(fry, "m_PotatoPrefab", Load($"{FoodFolder}/Food_Potato.prefab"));
            EditorAssetUtil.Set(fry, "m_FriesBatchPrefab", Load($"{FoodFolder}/Food_FriesBatch.prefab"));
            Key(root, "Fryer");
            EditorAssetUtil.SavePrefab(root, $"{KitchenFolder}/Station_Fryer.prefab");
        }

        private static void BuildFriesHolder()
        {
            var root = new GameObject("Station_FriesBox");
            Part(root.transform, "Stand", A.RoundedBox(0.95f, 0.08f, 0.8f, 0.04f), A.SoftPink, new Vector3(0f, 0.04f, 0f));
            GameObject snap = Node(root.transform, "BoxSpot", new Vector3(0f, 0.08f, 0f));
            var holder = root.AddComponent<FriesBoxHolder>();
            EditorAssetUtil.Set(holder, "m_SnapPoint", snap.transform);
            EditorAssetUtil.Set(holder, "m_AcceptRadius", 1.25f);
            EditorAssetUtil.Set(holder, "m_RingRadius", 0.8f);
            EditorAssetUtil.Set(holder, "m_DropPriority", 12);
            EditorAssetUtil.Set(holder, "m_BoxPrefab", Load($"{FoodFolder}/Food_FriesBox.prefab"));
            Key(root, "FriesBox");
            EditorAssetUtil.SavePrefab(root, $"{KitchenFolder}/Station_FriesBox.prefab");
        }

        private static void BuildDrinks(IList<DrinkData> drinks)
        {
            var root = new GameObject("Station_Drinks");
            // Machine body
            BlobShadow(root.transform, new Vector3(0f, 0f, 0.1f), 2.2f, 1.3f);
            Part(root.transform, "Body", A.RoundedBox(1.7f, 1.55f, 0.85f, 0.2f), A.SoftPink, new Vector3(0f, 0.775f, 0.15f));
            Part(root.transform, "Top", A.Mesh("ShakeMachineTop", () => MeshFactory.Lathe(MeshFactory.DomeProfile(0.62f, 0.45f, 0.05f), 32, "ShakeMachineTop")),
                A.White, new Vector3(0f, 1.55f, 0.15f), null, new Vector3(1.25f, 1f, 0.62f));
            Part(root.transform, "TopBall", A.SphereMesh, A.TomatoCoral, new Vector3(0f, 2.05f, 0.15f), null, Vector3.one * 0.28f);
            Part(root.transform, "Recess", A.RoundedBox(0.9f, 0.75f, 0.3f, 0.12f), A.White, new Vector3(0f, 0.5f, -0.18f), castShadows: false);
            GameObject nozzle = Part(root.transform, "Nozzle", A.Disc(0.12f, 0.22f, 0.06f, 16), A.Liquid, new Vector3(0f, 0.86f, -0.3f), new Vector3(180f, 0f, 0f), castShadows: false);
            Part(root.transform, "Drip", A.Disc(0.4f, 0.04f, 0.02f, 24), A.MintLight, new Vector3(0f, 0.12f, -0.3f), castShadows: false);
            GameObject cupSpot = Node(root.transform, "CupSpot", new Vector3(0f, 0.14f, -0.3f));
            Key(cupSpot, "Drinks.Cup");
            Face(root.transform, new Vector3(0f, 1.22f, -0.27f), 0.2f, 1.1f, Vector3.zero);

            // Flavour buttons across the front-top
            var flavourButtons = new List<Object>();
            var flavourData = new List<Object>();
            for (int i = 0; i < drinks.Count; i++)
            {
                DrinkData d = drinks[i];
                float x = -0.55f + i * 0.55f;
                GameObject btn = Node(root.transform, "Flavour_" + d.id, new Vector3(x, 1.62f, -0.36f));
                GameObject vis = Node(btn.transform, "Visual", Vector3.zero, new Vector3(-90f, 0f, 0f));
                Part(vis.transform, "Base", A.Disc(0.22f, 0.07f, 0.03f, 24), A.White, Vector3.zero, castShadows: false);
                Material m = A.Lit("Flavour_" + d.id, d.liquidColor, 0.5f);
                Part(vis.transform, "Cap", A.Mesh("FlavourCap", () => MeshFactory.Lathe(MeshFactory.DomeProfile(0.17f, 0.12f, 0.03f), 24, "FlavourCap")), m, new Vector3(0f, 0.06f, 0f));
                var c = btn.AddComponent<SphereCollider>();
                c.radius = 0.32f;
                flavourButtons.Add(Button3D(btn, 8, SfxId.ButtonTap, vis.transform));
                flavourData.Add(d);
                Key(btn, "Drinks.Flavour." + d.id);
            }

            // Big dispense lever (round button on the side)
            GameObject lever = Node(root.transform, "Lever", new Vector3(0.98f, 0.95f, -0.05f));
            GameObject lv = Node(lever.transform, "Visual", Vector3.zero, new Vector3(0f, 0f, -90f));
            Part(lv.transform, "Base", A.Disc(0.26f, 0.08f, 0.03f, 24), A.White, Vector3.zero, castShadows: false);
            Part(lv.transform, "Cap", A.Mesh("LeverCap", () => MeshFactory.Lathe(MeshFactory.DomeProfile(0.21f, 0.16f, 0.03f), 24, "LeverCap")), A.TomatoCoral, new Vector3(0f, 0.07f, 0f));
            var lc = lever.AddComponent<SphereCollider>();
            lc.radius = 0.38f;
            TapButton3D leverButton = Button3D(lever, 8, SfxId.ButtonTap, lv.transform);
            Key(lever, "Drinks.Lever");

            // Cup stack
            GameObject cups = Node(root.transform, "CupStack", new Vector3(-1.35f, 0f, -0.1f));
            GameObject cv = Node(cups.transform, "Visual", Vector3.zero);
            Mesh cupMesh = A.GetMesh("ShakeCup");
            for (int i = 0; i < 3; i++) Part(cv.transform, "Cup" + i, cupMesh, A.CupWhite, new Vector3(0f, i * 0.14f, 0f), null, Vector3.one * 0.72f);
            var cc = cups.AddComponent<BoxCollider>();
            cc.size = new Vector3(0.8f, 1.2f, 0.8f);
            cc.center = new Vector3(0f, 0.55f, 0f);
            TapButton3D cupButton = Button3D(cups, 8, SfxId.Pop, cv.transform);
            Key(cups, "Drinks.Cups");
            GameObject cupSpawn = Node(root.transform, "CupSpawn", new Vector3(-1.35f, 0.9f, -0.1f));

            // Whipped cream button
            GameObject cream = Node(root.transform, "CreamButton", new Vector3(1.4f, 0f, -0.25f));
            GameObject crv = Node(cream.transform, "Visual", Vector3.zero);
            Part(crv.transform, "Can", A.Capsule(0.17f, 0.7f), A.Sky, new Vector3(0f, 0.35f, 0f));
            Part(crv.transform, "Swirl", A.GetMesh("CreamSwirl"), A.Cream, new Vector3(0f, 0.68f, 0f), null, Vector3.one * 0.6f);
            var crc = cream.AddComponent<BoxCollider>();
            crc.size = new Vector3(0.7f, 1.2f, 0.7f);
            crc.center = new Vector3(0f, 0.55f, 0f);
            TapButton3D creamButton = Button3D(cream, 8, SfxId.Cream, crv.transform);
            Key(cream, "Drinks.Cream");

            // Straw jar
            GameObject jar = Node(root.transform, "StrawJar", new Vector3(1.4f, 0f, 0.45f));
            GameObject jv = Node(jar.transform, "Visual", Vector3.zero);
            Part(jv.transform, "Jar", A.Mesh("StrawJar", () => MeshFactory.Lathe(new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(0.2f, 0f), new Vector2(0.24f, 0.05f), new Vector2(0.24f, 0.45f), new Vector2(0.21f, 0.47f), new Vector2(0f, 0.47f)
            }, 24, "StrawJar")), A.Glass, Vector3.zero);
            Material[] strawMats = { A.Straw, A.TomatoCoral, A.CheeseYellow, A.Lavender };
            for (int i = 0; i < 4; i++)
            {
                Part(jv.transform, "Straw" + i, A.Capsule(0.035f, 0.9f), strawMats[i], new Vector3(-0.08f + i * 0.055f, 0.6f, (i % 2) * 0.05f - 0.02f),
                    new Vector3((i % 2) * 8f - 4f, 0f, -12f + i * 8f), null, false);
            }
            var jc = jar.AddComponent<BoxCollider>();
            jc.size = new Vector3(0.7f, 1.2f, 0.7f);
            jc.center = new Vector3(0f, 0.55f, 0f);
            TapButton3D strawButton = Button3D(jar, 8, SfxId.Straw, jv.transform);
            Key(jar, "Drinks.Straws");

            var station = root.AddComponent<DrinkStation>();
            var so = new UnityEditor.SerializedObject(station);
            SetList(so, "m_Flavours", flavourData);
            SetList(so, "m_FlavourButtons", flavourButtons);
            so.FindProperty("m_CupStack").objectReferenceValue = cupButton;
            so.FindProperty("m_Lever").objectReferenceValue = leverButton;
            so.FindProperty("m_CreamButton").objectReferenceValue = creamButton;
            so.FindProperty("m_StrawJar").objectReferenceValue = strawButton;
            so.FindProperty("m_CupSpot").objectReferenceValue = cupSpot.transform;
            so.FindProperty("m_CupSpawn").objectReferenceValue = cupSpawn.transform;
            so.FindProperty("m_NozzleRenderer").objectReferenceValue = nozzle.GetComponent<MeshRenderer>();
            so.FindProperty("m_CupPrefab").objectReferenceValue = Load($"{FoodFolder}/Food_MilkshakeCup.prefab");
            so.ApplyModifiedPropertiesWithoutUndo();
            Key(root, "Drinks");
            EditorAssetUtil.SavePrefab(root, $"{KitchenFolder}/Station_Drinks.prefab");
        }

        private static void SetList(UnityEditor.SerializedObject so, string field, List<Object> values)
        {
            UnityEditor.SerializedProperty p = so.FindProperty(field);
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void BuildTray()
        {
            var root = new GameObject("ServingTray");
            root.layer = FoodItem.InteractableLayer;
            Part(root.transform, "Tray", A.RoundedBox(2.1f, 0.1f, 1.45f, 0.05f), A.Mint, new Vector3(0f, 0.05f, 0f));
            Part(root.transform, "Mat", A.RoundedBox(1.9f, 0.02f, 1.25f, 0.01f, 2), A.CreamMat, new Vector3(0f, 0.105f, 0f), castShadows: false);
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(2.2f, 0.3f, 1.55f);
            col.center = new Vector3(0f, 0.1f, 0f);
            Transform slots = Node(root.transform, "Slots", new Vector3(0f, 0.115f, 0f)).transform;
            Transform s0 = Node(slots, "Burger", new Vector3(-0.45f, 0f, -0.15f)).transform;
            Transform s1 = Node(slots, "Fries", new Vector3(0.5f, 0f, -0.3f)).transform;
            Transform s2 = Node(slots, "Drink", new Vector3(0.55f, 0f, 0.35f)).transform;
            Transform s3 = Node(slots, "Extra1", new Vector3(-0.45f, 0f, 0.45f)).transform;
            Transform s4 = Node(slots, "Extra2", new Vector3(0f, 0f, 0.1f)).transform;
            MakeFood(root, FoodType.Tray, 0.1f, 2.2f);
            var tray = root.AddComponent<ServingTray>();
            EditorAssetUtil.SetArray(tray, "m_Slots", new Object[] { s0, s1, s2, s3, s4 });
            EditorAssetUtil.Set(tray, "m_SnapPoint", slots);
            EditorAssetUtil.Set(tray, "m_AcceptRadius", 1.45f);
            EditorAssetUtil.Set(tray, "m_RingRadius", 1.2f);
            EditorAssetUtil.Set(tray, "m_DropPriority", 14);
            Key(root, "Tray");
            EditorAssetUtil.SavePrefab(root, $"{KitchenFolder}/ServingTray.prefab");
        }
    }
}
