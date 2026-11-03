using LittleBurgerWorld.Customers;
using LittleBurgerWorld.Food;
using UnityEngine;
using A = LittleBurgerWorld.EditorTools.ArtLibrary;

namespace LittleBurgerWorld.EditorTools
{
    public static partial class PrefabBuilder
    {
        public enum EarStyle { Round, Long }

        public struct CharacterLook
        {
            public string Name;
            public Material Fur;
            public Material FurLight;
            public Material Accent;
            public EarStyle Ears;
            public bool Snout;
        }

        /// <summary>Builds an original toy-like animal character with a procedural rig and request bubble.</summary>
        public static GameObject BuildCharacter(CharacterLook look, CharacterData data, string path)
        {
            var root = new GameObject("Character_" + look.Name);
            root.layer = FoodItem.InteractableLayer;
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 1.15f, 0f);
            capsule.radius = 0.7f;
            capsule.height = 2.3f;

            BlobShadow(root.transform, Vector3.zero, 1.6f, 1.4f);
            Transform model = Node(root.transform, "Model", Vector3.zero).transform;
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
                    Part(e, "Ear", A.SphereMesh, look.Fur, new Vector3(0f, 0.08f, 0f), null, new Vector3(0.34f, 0.32f, 0.2f));
                    Part(e, "Inner", A.SphereMesh, A.Blush, new Vector3(0f, 0.08f, 0.07f), null, new Vector3(0.2f, 0.19f, 0.08f), false);
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
            if (look.Snout)
            {
                Part(head, "Snout", A.SphereMesh, look.FurLight, new Vector3(0f, 0.3f, 0.42f), null, new Vector3(0.44f, 0.32f, 0.3f));
                Part(head, "Nose", A.SphereMesh, A.Dark, new Vector3(0f, 0.4f, 0.57f), null, new Vector3(0.17f, 0.12f, 0.1f), false);
            }
            else
            {
                Part(head, "Nose", A.SphereMesh, A.Blush, new Vector3(0f, 0.36f, 0.5f), null, new Vector3(0.12f, 0.09f, 0.08f), false);
            }
            Transform eyeL = Part(head, "EyeL", A.SphereMesh, A.Dark, new Vector3(-0.21f, 0.56f, 0.42f), null, new Vector3(0.14f, 0.19f, 0.09f), false).transform;
            Transform eyeR = Part(head, "EyeR", A.SphereMesh, A.Dark, new Vector3(0.21f, 0.56f, 0.42f), null, new Vector3(0.14f, 0.19f, 0.09f), false).transform;
            foreach (Transform e in new[] { eyeL, eyeR })
            {
                Part(e, "Shine", A.LowSphere, A.EyeShine, new Vector3(-0.15f, 0.22f, 0.42f), null, new Vector3(0.38f, 0.28f, 0.3f), false);
            }
            Transform mouth = Part(head, "Mouth", A.SmileArc, A.Dark, new Vector3(0f, look.Snout ? 0.27f : 0.3f, look.Snout ? 0.6f : 0.5f),
                new Vector3(0f, 180f, 0f), Vector3.one * (look.Snout ? 0.75f : 0.6f), false).transform;
            Part(head, "CheekL", A.SphereMesh, A.Blush, new Vector3(-0.38f, 0.32f, 0.36f), null, new Vector3(0.17f, 0.1f, 0.08f), false);
            Part(head, "CheekR", A.SphereMesh, A.Blush, new Vector3(0.38f, 0.32f, 0.36f), null, new Vector3(0.17f, 0.1f, 0.08f), false);

            // Arms (pivot at shoulder) and legs (pivot at hip)
            Transform armL = Node(model, "ArmL", new Vector3(-0.47f, 1.08f, 0.02f)).transform;
            Transform armR = Node(model, "ArmR", new Vector3(0.47f, 1.08f, 0.02f)).transform;
            foreach (Transform a in new[] { armL, armR })
            {
                Part(a, "Arm", A.Capsule(0.14f, 0.62f), look.Fur, new Vector3(0f, -0.26f, 0f));
                Part(a, "Paw", A.SphereMesh, look.FurLight, new Vector3(0f, -0.52f, 0.02f), null, Vector3.one * 0.24f, false);
            }
            Transform legL = Node(model, "LegL", new Vector3(-0.26f, 0.4f, 0f)).transform;
            Transform legR = Node(model, "LegR", new Vector3(0.26f, 0.4f, 0f)).transform;
            foreach (Transform l in new[] { legL, legR })
            {
                Part(l, "Leg", A.Capsule(0.17f, 0.46f), look.Fur, new Vector3(0f, -0.17f, 0f));
                Part(l, "Foot", A.SphereMesh, look.FurLight, new Vector3(0f, -0.36f, 0.08f), null, new Vector3(0.3f, 0.14f, 0.38f));
            }
            Transform hold = Node(model, "HoldPoint", new Vector3(0f, 1.0f, 0.78f)).transform;

            // Picture request bubble
            GameObject bubble = Node(root.transform, "RequestBubble", new Vector3(0f, 3.05f, 0f));
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
            EditorAssetUtil.Set(ctrl, "m_AcceptRadius", 1.6f);
            EditorAssetUtil.Set(ctrl, "m_RingRadius", 1.1f);
            EditorAssetUtil.Set(ctrl, "m_DropPriority", 20);
            Key(root, "Customer");

            return EditorAssetUtil.SavePrefab(root, path);
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
