using System.Collections.Generic;
using LittleBurgerWorld.Core;
using UnityEngine;

namespace LittleBurgerWorld.Effects
{
    public enum FxType
    {
        Sparkle,
        Confetti,
        Steam,
        Hearts,
        Crumbs,
        Bubbles,
        Poof,
        Stars
    }

    /// <summary>
    /// Mobile-friendly particle effects. Each effect type is ONE looping particle system with no
    /// automatic emission; effects are produced with Emit() at any position, so there is no
    /// instantiation or pooling cost during play.
    /// </summary>
    public sealed class FxManager : MonoBehaviour
    {
        private static FxManager s_Instance;
        private readonly Dictionary<FxType, ParticleSystem> m_Systems = new Dictionary<FxType, ParticleSystem>();

        public static FxManager Instance
        {
            get
            {
                if (s_Instance == null && Application.isPlaying)
                {
                    s_Instance = new GameObject("[FxManager]").AddComponent<FxManager>();
                }
                return s_Instance;
            }
        }

        private void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
        }

        public static void Play(FxType type, Vector3 position, int count = -1, Color? color = null)
        {
            FxManager fx = Instance;
            if (fx == null) return;
            fx.Emit(type, position, count, color);
        }

        private void Emit(FxType type, Vector3 position, int count, Color? color)
        {
            ParticleSystem ps = GetSystem(type);
            if (ps == null) return;
            var p = new ParticleSystem.EmitParams { position = position, applyShapeToPosition = true };
            if (color.HasValue) p.startColor = color.Value;
            ps.Emit(p, count > 0 ? count : DefaultCount(type));
        }

        private static int DefaultCount(FxType t)
        {
            switch (t)
            {
                case FxType.Confetti: return 40;
                case FxType.Sparkle: return 14;
                case FxType.Steam: return 3;
                case FxType.Hearts: return 6;
                case FxType.Crumbs: return 8;
                case FxType.Bubbles: return 4;
                case FxType.Poof: return 10;
                case FxType.Stars: return 10;
            }
            return 8;
        }

        private ParticleSystem GetSystem(FxType type)
        {
            if (m_Systems.TryGetValue(type, out ParticleSystem existing) && existing != null) return existing;
            ParticleSystem ps = Create(type);
            m_Systems[type] = ps;
            return ps;
        }

        private ParticleSystem Create(FxType type)
        {
            GameConfig cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;
            var go = new GameObject("Fx_" + type);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = FadeOut();
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;

            Material mat = cfg != null ? cfg.sparkleMaterial : null;
            switch (type)
            {
                case FxType.Sparkle:
                case FxType.Stars:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.15f, type == FxType.Stars ? 0.45f : 0.3f);
                    main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.6f), Color.white);
                    main.gravityModifier = -0.05f;
                    shape.radius = 0.35f;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
                    mat = cfg != null ? cfg.sparkleMaterial : null;
                    break;
                case FxType.Confetti:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
                    main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                    main.gravityModifier = 0.9f;
                    main.startColor = Rainbow();
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 35f;
                    shape.radius = 0.2f;
                    shape.rotation = new Vector3(-90f, 0f, 0f);
                    ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
                    rot.enabled = true;
                    rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
                    mat = cfg != null ? cfg.confettiMaterial : null;
                    break;
                case FxType.Steam:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
                    main.startColor = new Color(1f, 1f, 1f, 0.55f);
                    main.gravityModifier = -0.15f;
                    shape.radius = 0.3f;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
                    mat = cfg != null ? cfg.softParticleMaterial : null;
                    break;
                case FxType.Hearts:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.6f), new Color(1f, 0.7f, 0.8f));
                    main.gravityModifier = -0.2f;
                    shape.radius = 0.4f;
                    mat = cfg != null ? cfg.heartMaterial : null;
                    break;
                case FxType.Crumbs:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.6f, 0.3f), new Color(0.95f, 0.8f, 0.45f));
                    main.gravityModifier = 1.5f;
                    shape.radius = 0.15f;
                    mat = cfg != null ? cfg.softParticleMaterial : null;
                    break;
                case FxType.Bubbles:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
                    main.startColor = new Color(1f, 0.97f, 0.8f, 0.9f);
                    main.gravityModifier = -0.3f;
                    shape.radius = 0.4f;
                    mat = cfg != null ? cfg.softParticleMaterial : null;
                    break;
                case FxType.Poof:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
                    main.startColor = new Color(1f, 1f, 1f, 0.85f);
                    shape.radius = 0.25f;
                    size.enabled = true;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.3f));
                    mat = cfg != null ? cfg.softParticleMaterial : null;
                    break;
            }

            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingFudge = -10f; // draw over the food it decorates
            if (mat != null) r.sharedMaterial = mat;
            ps.Play();
            return ps;
        }

        private static ParticleSystem.MinMaxGradient FadeOut()
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.65f), new GradientAlphaKey(0f, 1f) });
            return new ParticleSystem.MinMaxGradient(g);
        }

        private static ParticleSystem.MinMaxGradient Rainbow()
        {
            var g = new Gradient();
            g.SetKeys(new[]
            {
                new GradientColorKey(new Color(0.96f, 0.65f, 0.14f), 0f),
                new GradientColorKey(new Color(0.91f, 0.42f, 0.32f), 0.25f),
                new GradientColorKey(new Color(0.47f, 0.75f, 0.63f), 0.5f),
                new GradientColorKey(new Color(0.51f, 0.71f, 0.91f), 0.75f),
                new GradientColorKey(new Color(0.95f, 0.66f, 0.72f), 1f)
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
        }
    }
}
