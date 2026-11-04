using LittleBurgerWorld.Customers;
using LittleBurgerWorld.Core;
using LittleBurgerWorld.Food;
using UnityEngine;
using A = LittleBurgerWorld.EditorTools.ArtLibrary;

namespace LittleBurgerWorld.EditorTools
{
    public static partial class PrefabBuilder
    {
        public enum EarStyle { Round, Long, Pointy, Floppy, Big }
        public enum TailStyle { None, Long, Short, Thin, Puff }

        public struct CharacterLook
        {
            public string Name;
            public Material Fur;
            public Material FurLight;
            public Material Accent;
            public EarStyle Ears;
            public bool Snout;
            /// <summary>Ear colour (null = Fur).</summary>
            public Material EarFur;
            /// <summary>Arms and legs colour (null = Fur), e.g. a panda's black limbs.</summary>
            public Material LimbFur;
            /// <summary>Colour of a patch around the right eye (dog) – null for none.</summary>
            public Material EyeSpot;
            /// <summary>Dark patches around both eyes (panda) – null for none.</summary>
            public Material EyePatches;
            /// <summary>Forehead stripes (tabby cat) – null for none.</summary>
            public Material Stripes;
            public bool Whiskers;
            public bool Trunk;
            public TailStyle Tail;
            /// <summary>Overall size multiplier (elephant is a little bigger).</summary>
            public float Scale;
        }

        /// <summary>Builds an original toy-like animal character with a procedural rig and request bubble.</summary>
        public static GameObject BuildCharacter(CharacterLook look, CharacterData data, string path)
        {
            var root = new GameObject("Character_" + look.Name);
            root.layer = FoodItem.InteractableLayer;
            var capsule = root.AddComponent<CapsuleCollider>();
            float cs = look.Scale > 0f ? look.Scale : 1f;
            capsule.center = new Vector3(0f, 1.15f * cs, 0f);
            capsule.radius = 0.7f * cs;
            capsule.height = 2.3f * cs;

            float scale = look.Scale > 0f ? look.Scale : 1f;
            Material earFur = look.EarFur != null ? look.EarFur : look.Fur;
            Material limbFur = look.LimbFur != null ? look.LimbFur : look.Fur;
            BlobShadow(root.transform, Vector3.zero, 1.6f * scale, 1.4f * scale);
            Transform model = Node(root.transform, "Model", Vector3.zero).transform;
            model.localScale = Vector3.one * scale;
            GameObject body = Part(model, "Body", A.SphereMesh, look.Fur, new Vector3(0f, 0.8f, 0f), null, new Vector3(1.0f, 1.05f, 0.9f));
            Part(model, "Belly", A.SphereMesh, look.FurLight, new Vector3(0f, 0.74f, 0.27f), null, new Vector3(0.62f, 0.68f, 0.38f), false);
            // Bow tie
            Part(model, "BowL", A.SphereMesh, look.Accent, new Vector3(-0.11f, 1.2f, 0.4f), new Vector3(0f, 0f, 90f), new Vector3(0.16f, 0.2f, 0.1f), false);
            Part(model, "BowR", A.SphereMesh, look.Accent, new Vector3(0.11f, 1.2f, 0.4f), new Vector3(0f, 0f, 90f), new Vector3(0.16f, 0.2f, 0.1f), false);
            Part(model, "BowKnot", A.SphereMesh, look.Accent, new Vector3(0f, 1.2f, 0.43f), null, Vector3.one * 0.09f, false);

            // Head
            Transform head = Node(model, "Head", new Vector3(0f, 1.28f, 0f)).transform;
            Part(head, "Skull", A.SphereMesh, look.Fur, new Vector3(0f, 0.46f, 0f), null, new Vector3(1.12f, 1.0f, 1.0f));
            Transform earL, earR;
            if (look.Ears == EarStyle.Round)
            {
                earL = Node(head, "EarL", new Vector3(-0.38f, 0.86f, 0f)).transform;
                earR = Node(head, "EarR", new Vector3(0.38f, 0.86f, 0f)).transform;
                foreach (Transform e in new[] { earL, earR })
                {
                    Part(e, "Ear", A.SphereMesh, earFur, new Vector3(0f, 0.08f, 0f), null, new Vector3(0.34f, 0.32f, 0.2f));
                    if (look.EarFur == null)
                        Part(e, "Inner", A.SphereMesh, A.Blush, new Vector3(0f, 0.08f, 0.07f), null, new Vector3(0.2f, 0.19f, 0.08f), false);
                }
            }
            else if (look.Ears == EarStyle.Pointy)
            {
                earL = Node(head, "EarL", new Vector3(-0.32f, 0.8f, 0f), new Vector3(0f, 0f, 16f)).transform;
                earR = Node(head, "EarR", new Vector3(0.32f, 0.8f, 0f), new Vector3(0f, 0f, -16f)).transform;
                foreach (Transform e in new[] { earL, earR })
                {
                    Part(e, "Ear", A.SoftCone, earFur, Vector3.zero, null, new Vector3(0.36f, 0.4f, 0.24f));
                    Part(e, "Inner", A.SoftCone, A.Blush, new Vector3(0f, 0.02f, 0.06f), null, new Vector3(0.2f, 0.27f, 0.1f), false);
                }
            }
            else if (look.Ears == EarStyle.Floppy)
            {
                earL = Node(head, "EarL", new Vector3(-0.5f, 0.74f, 0f), new Vector3(0f, 0f, -18f)).transform;
                earR = Node(head, "EarR", new Vector3(0.5f, 0.74f, 0f), new Vector3(0f, 0f, 18f)).transform;
                foreach (Transform e in new[] { earL, earR })
                    Part(e, "Ear", A.SphereMesh, earFur, new Vector3(0f, -0.24f, 0f), null, new Vector3(0.26f, 0.56f, 0.16f));
            }
            else if (look.Ears == EarStyle.Big)
            {
                earL = Node(head, "EarL", new Vector3(-0.5f, 0.52f, -0.06f), new Vector3(0f, 25f, 0f)).transform;
                earR = Node(head, "EarR", new Vector3(0.5f, 0.52f, -0.06f), new Vector3(0f, -25f, 0f)).transform;
                float side = -1f;
                foreach (Transform e in new[] { earL, earR })
                {
                    Part(e, "Ear", A.SphereMesh, earFur, new Vector3(side * 0.3f, 0f, 0f), null, new Vector3(0.72f, 0.8f, 0.14f));
                    Part(e, "Inner", A.SphereMesh, A.Blush, new Vector3(side * 0.3f, 0f, 0.06f), null, new Vector3(0.5f, 0.58f, 0.06f), false);
                    side = 1f;
                }
            }
            else
            {
                earL = Node(head, "EarL", new Vector3(-0.2f, 0.88f, 0f), new Vector3(0f, 0f, 10f)).transform;
                earR = Node(head, "EarR", new Vector3(0.2f, 0.88f, 0f), new Vector3(0f, 0f, -10f)).transform;
                foreach (Transform e in new[] { earL, earR })
                {
                    Part(e, "Ear", A.Capsule(0.12f, 0.9f), look.Fur, new Vector3(0f, 0.4f, 0f), null, new Vector3(1.1f, 1f, 0.7f));
                    Part(e, "Inner", A.Capsule(0.07f, 0.7f), A.Blush, new Vector3(0f, 0.42f, 0.07f), null, new Vector3(1f, 1f, 0.5f), false);
                }
                Part(earR, "Bow", A.SphereMesh, look.Accent, new Vector3(0.05f, 0.12f, 0.06f), new Vector3(0f, 0f, 30f), new Vector3(0.22f, 0.14f, 0.12f), false);
            }
            if (look.Trunk)
            {
                // Soft trunk: a chain of shrinking balls curling forward, with a little upturned tip.
                Vector3[] pts = { new Vector3(0f, 0.36f, 0.46f), new Vector3(0f, 0.2f, 0.58f), new Vector3(0f, 0.04f, 0.66f), new Vector3(0f, -0.06f, 0.76f), new Vector3(0f, -0.06f, 0.88f) };
                SmoothChain(head, "Trunk", pts, 0.25f, 0.15f, look.Fur, 14);
                Part(head, "TrunkTip", A.SphereMesh, look.FurLight, new Vector3(0f, -0.06f, 0.965f), new Vector3(90f, 0f, 0f), new Vector3(0.15f, 0.04f, 0.15f), false);
            }
            else if (look.Snout)
            {
                Part(head, "Snout", A.SphereMesh, look.FurLight, new Vector3(0f, 0.3f, 0.42f), null, new Vector3(0.44f, 0.32f, 0.3f));
                Part(head, "Nose", A.SphereMesh, A.Dark, new Vector3(0f, 0.4f, 0.57f), null, new Vector3(0.17f, 0.12f, 0.1f), false);
            }
            else
            {
                Part(head, "Nose", A.SphereMesh, A.Blush, new Vector3(0f, 0.36f, 0.5f), null, new Vector3(0.12f, 0.09f, 0.08f), false);
            }
            if (look.EyePatches != null)
            {
                Part(head, "PatchL", A.SphereMesh, look.EyePatches, new Vector3(-0.22f, 0.53f, 0.39f), new Vector3(0f, 0f, -28f), new Vector3(0.27f, 0.34f, 0.12f), false);
                Part(head, "PatchR", A.SphereMesh, look.EyePatches, new Vector3(0.22f, 0.53f, 0.39f), new Vector3(0f, 0f, 28f), new Vector3(0.27f, 0.34f, 0.12f), false);
                // White eye rims so the eyes still read on the dark patches.
                Part(head, "RimL", A.SphereMesh, A.White, new Vector3(-0.21f, 0.56f, 0.415f), null, new Vector3(0.18f, 0.23f, 0.08f), false);
                Part(head, "RimR", A.SphereMesh, A.White, new Vector3(0.21f, 0.56f, 0.415f), null, new Vector3(0.18f, 0.23f, 0.08f), false);
            }
            if (look.EyeSpot != null)
                Part(head, "EyeSpot", A.SphereMesh, look.EyeSpot, new Vector3(0.22f, 0.58f, 0.37f), null, new Vector3(0.32f, 0.32f, 0.14f), false);
            if (look.Stripes != null)
            {
                Part(head, "Stripe0", A.Capsule(0.035f, 0.2f), look.Stripes, new Vector3(0f, 0.86f, 0.36f), new Vector3(-30f, 0f, 0f), Vector3.one, false);
                Part(head, "Stripe1", A.Capsule(0.035f, 0.16f), look.Stripes, new Vector3(-0.13f, 0.83f, 0.34f), new Vector3(-30f, 0f, 14f), Vector3.one, false);
                Part(head, "Stripe2", A.Capsule(0.035f, 0.16f), look.Stripes, new Vector3(0.13f, 0.83f, 0.34f), new Vector3(-30f, 0f, -14f), Vector3.one, false);
            }
            if (look.Whiskers)
            {
                for (int side = -1; side <= 1; side += 2)
                    for (int i = 0; i < 3; i++)
                        Part(head, $"Whisker{(side < 0 ? "L" : "R")}{i}", A.Capsule(0.012f, 0.34f), A.Dark,
                            new Vector3(side * 0.36f, 0.36f + (1 - i) * 0.055f, 0.43f), new Vector3(0f, -side * 20f, 90f + side * (i - 1) * 10f), Vector3.one, false);
            }
            Transform eyeL = Part(head, "EyeL", A.SphereMesh, A.Dark, new Vector3(-0.21f, 0.56f, 0.42f), null, new Vector3(0.14f, 0.19f, 0.09f), false).transform;
            Transform eyeR = Part(head, "EyeR", A.SphereMesh, A.Dark, new Vector3(0.21f, 0.56f, 0.42f), null, new Vector3(0.14f, 0.19f, 0.09f), false).transform;
            foreach (Transform e in new[] { eyeL, eyeR })
            {
                Part(e, "Shine", A.LowSphere, A.EyeShine, new Vector3(-0.15f, 0.22f, 0.42f), null, new Vector3(0.38f, 0.28f, 0.3f), false);
            }
            Vector3 mouthPos = look.Trunk ? new Vector3(0f, 0.17f, 0.52f) : new Vector3(0f, look.Snout ? 0.27f : 0.3f, look.Snout ? 0.6f : 0.5f);
            Transform mouth = Part(head, "Mouth", A.SmileArc, A.Dark, mouthPos,
                new Vector3(0f, 180f, 0f), Vector3.one * (look.Snout ? 0.75f : 0.6f), false).transform;
            Part(head, "CheekL", A.SphereMesh, A.Blush, new Vector3(-0.38f, 0.32f, 0.36f), null, new Vector3(0.17f, 0.1f, 0.08f), false);
            Part(head, "CheekR", A.SphereMesh, A.Blush, new Vector3(0.38f, 0.32f, 0.36f), null, new Vector3(0.17f, 0.1f, 0.08f), false);

            // Party hat (shown on birthday days by AnimalCustomerController)
            GameObject hat = Node(head, "PartyHat", new Vector3(look.Ears == EarStyle.Long ? 0f : 0.1f, 0.92f, 0f), new Vector3(0f, 0f, look.Ears == EarStyle.Long ? 0f : -14f));
            Part(hat.transform, "Cone", A.SoftCone, A.Lit("PartyHatPink", EditorAssetUtil.Hex("#FF6F91"), 0.4f), Vector3.zero, null, new Vector3(0.36f, 0.5f, 0.36f), false);
            Part(hat.transform, "Stripe", A.Mesh("PartyHatBand", () => MeshFactory.Torus(0.14f, 0.025f, 20, 6)), A.CheeseYellow, new Vector3(0f, 0.12f, 0f), null, null, false);
            Part(hat.transform, "PomPom", A.SphereMesh, A.CheeseYellow, new Vector3(0f, 0.52f, 0f), null, Vector3.one * 0.13f, false);
            hat.SetActive(false);

            // Arms (pivot at shoulder) and legs (pivot at hip)
            Transform armL = Node(model, "ArmL", new Vector3(-0.47f, 1.08f, 0.02f)).transform;
            Transform armR = Node(model, "ArmR", new Vector3(0.47f, 1.08f, 0.02f)).transform;
            foreach (Transform a in new[] { armL, armR })
            {
                Part(a, "Arm", A.Capsule(0.14f, 0.62f), limbFur, new Vector3(0f, -0.26f, 0f));
                Part(a, "Paw", A.SphereMesh, look.LimbFur != null ? limbFur : look.FurLight, new Vector3(0f, -0.52f, 0.02f), null, Vector3.one * 0.24f, false);
            }
            Transform legL = Node(model, "LegL", new Vector3(-0.26f, 0.4f, 0f)).transform;
            Transform legR = Node(model, "LegR", new Vector3(0.26f, 0.4f, 0f)).transform;
            foreach (Transform l in new[] { legL, legR })
            {
                Part(l, "Leg", A.Capsule(0.17f, 0.46f), limbFur, new Vector3(0f, -0.17f, 0f));
                Part(l, "Foot", A.SphereMesh, look.LimbFur != null ? limbFur : look.FurLight, new Vector3(0f, -0.36f, 0.08f), null, new Vector3(0.3f, 0.14f, 0.38f));
            }
            switch (look.Tail)
            {
                case TailStyle.Long: // cat: curls up behind
                    Vector3[] tail = { new Vector3(0.05f, 0.45f, -0.45f), new Vector3(0.18f, 0.52f, -0.72f), new Vector3(0.32f, 0.72f, -0.86f), new Vector3(0.38f, 0.98f, -0.84f), new Vector3(0.32f, 1.18f, -0.74f) };
                    SmoothChain(model, "Tail", tail, 0.12f, 0.1f, look.Fur, 14);
                    if (look.Stripes != null) Part(model, "TailTip", A.SphereMesh, look.Stripes, tail[tail.Length - 1], null, Vector3.one * 0.22f, false);
                    break;
                case TailStyle.Short: // dog: perky, wags with the body
                    Part(model, "Tail", A.Capsule(0.09f, 0.4f), earFur, new Vector3(0f, 0.62f, -0.52f), new Vector3(-50f, 0f, 0f), Vector3.one, false);
                    break;
                case TailStyle.Thin: // elephant: thin with a tuft
                    Part(model, "Tail", A.Capsule(0.04f, 0.4f), look.Fur, new Vector3(0f, 0.5f, -0.5f), new Vector3(-25f, 0f, 0f), Vector3.one, false);
                    Part(model, "Tuft", A.SphereMesh, A.Dark, new Vector3(0f, 0.33f, -0.58f), null, Vector3.one * 0.09f, false);
                    break;
                case TailStyle.Puff:
                    Part(model, "Tail", A.SphereMesh, limbFur, new Vector3(0f, 0.5f, -0.46f), null, Vector3.one * 0.22f, false);
                    break;
            }
            Transform hold = Node(model, "HoldPoint", new Vector3(0f, 1.0f, 0.78f)).transform;

            // Picture request bubble
            GameObject bubble = Node(root.transform, "RequestBubble", new Vector3(0f, 3.05f * scale, 0f));
            var bg = Node(bubble.transform, "Bubble", Vector3.zero).AddComponent<SpriteRenderer>();
            bg.sprite = IconLibrary.Get("ui_bubble");
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(1.3f, 1.05f);
            bg.sortingOrder = 10;
            var dots = Node(bubble.transform, "Dots", new Vector3(-0.32f, -0.78f, 0.01f)).AddComponent<SpriteRenderer>();
            dots.sprite = IconLibrary.Get("ui_thought_dots");
            dots.transform.localScale = Vector3.one * 0.55f;
            dots.sortingOrder = 10;
            var icons = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                var sr = Node(bubble.transform, "Icon" + i, new Vector3(0f, 0.05f, -0.02f)).AddComponent<SpriteRenderer>();
                sr.transform.localScale = Vector3.one * 0.66f;
                sr.sortingOrder = 11;
                icons[i] = sr;
            }
            var rb = bubble.AddComponent<RequestBubble>();
            EditorAssetUtil.Set(rb, "m_Background", bg);
            EditorAssetUtil.SetArray(rb, "m_Icons", icons);
            EditorAssetUtil.Set(rb, "m_IconSpacing", 0.68f);

            var rig = root.AddComponent<ProceduralCharacterRig>();
            rig.model = model;
            rig.body = body.transform;
            rig.head = head;
            rig.earLeft = earL;
            rig.earRight = earR;
            rig.armLeft = armL;
            rig.armRight = armR;
            rig.legLeft = legL;
            rig.legRight = legR;
            rig.eyeLeft = eyeL;
            rig.eyeRight = eyeR;
            rig.mouth = mouth;
            rig.holdPoint = hold;

            var ctrl = root.AddComponent<AnimalCustomerController>();
            EditorAssetUtil.Set(ctrl, "m_Data", data);
            EditorAssetUtil.Set(ctrl, "m_Rig", rig);
            EditorAssetUtil.Set(ctrl, "m_Bubble", rb);
            EditorAssetUtil.Set(ctrl, "m_PartyHat", hat);
            EditorAssetUtil.Set(ctrl, "m_AcceptRadius", 1.6f);
            EditorAssetUtil.Set(ctrl, "m_RingRadius", 1.1f);
            EditorAssetUtil.Set(ctrl, "m_DropPriority", 20);
            Key(root, "Customer");

            return EditorAssetUtil.SavePrefab(root, path);
        }

        /// <summary>Smooth tube from overlapping spheres along a Catmull-Rom curve (trunks, tails).</summary>
        private static void SmoothChain(Transform parent, string name, Vector3[] pts, float r0, float r1, Material mat, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float seg = t * (pts.Length - 1);
                int k = Mathf.Min(pts.Length - 2, Mathf.FloorToInt(seg));
                float u = seg - k;
                Vector3 p0 = pts[Mathf.Max(0, k - 1)], p1 = pts[k], p2 = pts[k + 1], p3 = pts[Mathf.Min(pts.Length - 1, k + 2)];
                Vector3 p = 0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u + (-p0 + 3f * p1 - 3f * p2 + p3) * u * u * u);
                Part(parent, name + i, A.SphereMesh, mat, p, null, Vector3.one * Mathf.Lerp(r0, r1, t) * 2f, i == 0);
            }
        }

        /// <summary>Static Benny for the main menu (no customer behaviour, just the rig).</summary>
        public static GameObject BuildMascot(GameObject characterPrefab, string path)
        {
            GameObject go = Object.Instantiate(characterPrefab);
            go.name = "Mascot_Benny";
            Object.DestroyImmediate(go.GetComponent<AnimalCustomerController>());
            Object.DestroyImmediate(go.GetComponent<LittleBurgerWorld.Core.SceneKey>());
            Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
            Transform bubble = go.transform.Find("RequestBubble");
            if (bubble != null) Object.DestroyImmediate(bubble.gameObject);
            return EditorAssetUtil.SavePrefab(go, path);
        }
    }
}
