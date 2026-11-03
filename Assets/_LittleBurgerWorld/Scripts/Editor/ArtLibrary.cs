using System;
using System.Collections.Generic;
using LittleBurgerWorld.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleBurgerWorld.EditorTools
{
    /// <summary>
    /// Materials (pastel, soft, toy-like URP/Lit) and baked procedural meshes.
    /// Re-running updates assets in place so references and GUIDs are preserved.
    /// </summary>
    public static class ArtLibrary
    {
        public const string MaterialFolder = EditorAssetUtil.Root + "/Materials";
        public const string MeshFolder = EditorAssetUtil.Root + "/Art/Meshes";

        private static readonly Dictionary<string, Material> s_Materials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Mesh> s_Meshes = new Dictionary<string, Mesh>();

        public static void ClearCache()
        {
            s_Materials.Clear();
            s_Meshes.Clear();
        }

        // ------------------------------------------------------------------ materials
        public static Material Lit(string name, Color color, float smoothness = 0.28f, Texture texture = null,
            Vector2? tiling = null, Color? emission = null)
        {
            string key = "M_" + name;
            if (s_Materials.TryGetValue(key, out Material cached) && cached != null) return cached;
            string path = $"{MaterialFolder}/{key}.mat";
            EditorAssetUtil.EnsureFolder(MaterialFolder);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = key };
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_EnvironmentReflections", 0f);
            m.SetFloat("_SpecularHighlights", smoothness > 0.4f ? 1f : 0f);
            if (smoothness <= 0.4f) m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); else m.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            if (texture != null)
            {
                m.SetTexture("_BaseMap", texture);
                m.SetTextureScale("_BaseMap", tiling ?? Vector2.one);
            }
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                m.DisableKeyword("_EMISSION");
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            s_Materials[key] = m;
            return m;
        }

        /// <summary>Unlit, alpha-blended, vertex-coloured material (particles, rings, pictures, sprites in 3D).</summary>
        public static Material Sprite(string name, Texture texture, Color? tint = null)
        {
            string key = "M_" + name;
            if (s_Materials.TryGetValue(key, out Material cached) && cached != null) return cached;
            string path = $"{MaterialFolder}/{key}.mat";
            EditorAssetUtil.EnsureFolder(MaterialFolder);
            Shader shader = Shader.Find("Sprites/Default");
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = key };
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.mainTexture = texture;
            m.SetColor("_Color", tint ?? Color.white);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            s_Materials[key] = m;
            return m;
        }

        // Named palette materials ---------------------------------------------------------
        public static Material Bun => Lit("Bun", Hex("#F0B55C"), 0.3f);
        public static Material BunTop => Lit("BunTop", Hex("#E8993D"), 0.42f);
        public static Material BunInside => Lit("BunInside", Hex("#FCE3B0"), 0.15f);
        public static Material Patty => Lit("Patty", Hex("#EE9A93"), 0.25f);
        public static Material GrillMark => Lit("GrillMark", Hex("#4A2A1A"), 0.2f);
        public static Material Cheese => Lit("Cheese", Hex("#F7B731"), 0.45f);
        public static Material Lettuce => Lit("Lettuce", Hex("#8CCB5E"), 0.35f);
        public static Material Tomato => Lit("Tomato", Hex("#E4503F"), 0.5f);
        public static Material TomatoInner => Lit("TomatoInner", Hex("#F58A70"), 0.4f);
        public static Material Seed => Lit("Seed", Hex("#FFF4DA"), 0.3f);
        public static Material Pickle => Lit("Pickle", Hex("#6FAF4A"), 0.4f);
        public static Material PickleInner => Lit("PickleInner", Hex("#B5DB80"), 0.3f);
        public static Material Fries => Lit("Fries", Hex("#F7C948"), 0.3f);
        public static Material Potato => Lit("Potato", Hex("#C9955A"), 0.2f);
        public static Material FriesBox => Lit("FriesBox", Hex("#E96A52"), 0.3f);
        public static Material CupWhite => Lit("CupWhite", Hex("#FFF9F1"), 0.35f);
        public static Material Liquid => Lit("Liquid", Hex("#F48FB1"), 0.6f);
        public static Material Cream => Lit("Cream", Hex("#FFFDF8"), 0.25f);
        public static Material Straw => Lit("Straw", Hex("#79BFA0"), 0.5f);

        public static Material CheeseYellow => Lit("CheeseYellow", Hex("#F5A623"));
        public static Material TomatoCoral => Lit("TomatoCoral", Hex("#E96A52"));
        public static Material Mint => Lit("Mint", Hex("#79BFA0"));
        public static Material MintLight => Lit("MintLight", Hex("#A9DCC4"));
        public static Material Sky => Lit("Sky", Hex("#83B6E8"));
        public static Material SkyLight => Lit("SkyLight", Hex("#CFE6FA"), 0.6f);
        public static Material CreamMat => Lit("Cream_Toy", Hex("#F5D9B5"));
        public static Material WarmBrown => Lit("WarmBrown", Hex("#A66A42"));
        public static Material SoftPink => Lit("SoftPink", Hex("#F2A8B7"));
        public static Material Lavender => Lit("Lavender", Hex("#B9A6E8"));
        public static Material White => Lit("White", Hex("#FFF9F1"));
        public static Material Dark => Lit("Dark", Hex("#3E2A20"), 0.6f);
        public static Material GrillTop => Lit("GrillTop", Hex("#5B6773"), 0.4f);
        public static Material GrillBar => Lit("GrillBar", Hex("#3B444E"), 0.5f);
        public static Material Glow => Lit("Glow", Hex("#FFB066"), 0.2f, null, null, Hex("#FF9F43") * 0.8f);
        public static Material Oil => Lit("Oil", Hex("#F6C35B"), 0.7f, null, null, Hex("#F6C35B") * 0.25f);
        public static Material Wood => Lit("Wood", Hex("#E2B887"), 0.2f);
        public static Material WoodDark => Lit("WoodDark", Hex("#C08E5E"), 0.2f);
        public static Material Leaf => Lit("Leaf", Hex("#6DBE7A"), 0.3f);
        public static Material LeafDark => Lit("LeafDark", Hex("#4FA466"), 0.3f);
        public static Material Terracotta => Lit("Terracotta", Hex("#E08A6A"), 0.2f);
        public static Material Glass => Lit("Glass", Hex("#CFE8FA"), 0.85f);
        public static Material Floor => Lit("Floor", Color.white, 0.15f, IconLibrary.GetTexture("Floor_Checker"), new Vector2(4.5f, 4.5f));
        public static Material Wall => Lit("Wall", Color.white, 0.1f, IconLibrary.GetTexture("Wall_Dots"), new Vector2(4f, 1.5f));
        public static Material Ground => Lit("Ground", Hex("#B9E8C2"), 0.1f);
        public static Material Path => Lit("Path", Hex("#F1E3CF"), 0.1f);

        public static Material BearFur => Lit("BearFur", Hex("#A0663F"), 0.15f);
        public static Material BearLight => Lit("BearLight", Hex("#E7C39A"), 0.15f);
        public static Material BunnyFur => Lit("BunnyFur", Hex("#FFF4F2"), 0.15f);
        public static Material Blush => Lit("Blush", Hex("#F6A3B4"), 0.1f);
        public static Material EyeShine => Lit("EyeShine", Color.white, 0.9f, null, null, Color.white * 0.4f);
        public static Material BlobShadow => Sprite("BlobShadow", IconLibrary.Get("fx_soft") != null ? IconLibrary.Get("fx_soft").texture : null,
            new Color(0.32f, 0.18f, 0.1f, 0.42f));
        public static Mesh BlobQuad => Mesh("BlobQuad", () => MeshFactory.FlatQuad(1f, 1f, "BlobQuad"));
        public static Material Balloon => Lit("Balloon", Color.white, 0.65f);

        private static Color Hex(string h) => EditorAssetUtil.Hex(h);

        // ------------------------------------------------------------------ meshes
        public static Mesh Mesh(string name, Func<Mesh> build)
        {
            if (s_Meshes.TryGetValue(name, out Mesh cached) && cached != null) return cached;
            Mesh m = build();
            m.name = name;
            Mesh saved = EditorAssetUtil.CreateOrReplace(m, $"{MeshFolder}/{name}.asset");
            s_Meshes[name] = saved;
            return saved;
        }

        /// <summary>Returns a mesh built earlier in this run (or saved by a previous run).</summary>
        public static Mesh GetMesh(string name)
        {
            if (s_Meshes.TryGetValue(name, out Mesh cached) && cached != null) return cached;
            Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshFolder}/{name}.asset");
            if (m == null) Debug.LogError("[LBW Setup] Mesh not built yet: " + name);
            return m;
        }

        /// <summary>A smile arc in the XY plane (facing -Z), curving downwards.</summary>
        public static Mesh SmileArc => Mesh("SmileArc", () => MeshFactory.Combine("SmileArc",
            (MeshFactory.Torus(0.12f, 0.025f, 16, 6, 0.4f), Matrix4x4.Rotate(Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(0f, 90f, 0f)))));

        public static Mesh RoundedBox(float x, float y, float z, float r, int seg = 3)
        {
            string name = $"RBox_{x:0.##}x{y:0.##}x{z:0.##}_r{r:0.##}".Replace('.', 'p');
            return Mesh(name, () => MeshFactory.RoundedBox(new Vector3(x, y, z), r, seg));
        }

        public static Mesh Disc(float radius, float height, float bevel, int segments = 32)
        {
            string name = $"Disc_{radius:0.##}_{height:0.##}_{bevel:0.##}".Replace('.', 'p');
            return Mesh(name, () => MeshFactory.Lathe(MeshFactory.RoundedDiscProfile(radius, height, bevel), segments));
        }

        public static Mesh SphereMesh => Mesh("Sphere_r0p5", () => MeshFactory.Sphere(0.5f, 24, 16));
        public static Mesh LowSphere => Mesh("Sphere_low_r0p5", () => MeshFactory.Sphere(0.5f, 10, 7));

        public static Mesh Capsule(float radius, float height) =>
            Mesh($"Capsule_{radius:0.##}_{height:0.##}".Replace('.', 'p'), () => MeshFactory.Capsule(radius, height));
    }
}
