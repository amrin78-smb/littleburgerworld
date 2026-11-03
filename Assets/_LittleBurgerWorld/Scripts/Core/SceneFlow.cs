using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LittleBurgerWorld.Core
{
    /// <summary>Scene loading with a soft colour fade. Lives on the persistent GameManager.</summary>
    public sealed class SceneFlow : MonoBehaviour
    {
        private static readonly Color k_FadeColor = new Color(1f, 0.93f, 0.82f, 1f); // warm cream

        private CanvasGroup m_Fade;
        private bool m_Loading;

        public bool IsLoading => m_Loading;
        public event Action<string> SceneLoaded;

        private void Awake() => BuildOverlay();

        private void BuildOverlay()
        {
            if (m_Fade != null) return;
            var go = new GameObject("FadeOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            m_Fade = go.GetComponent<CanvasGroup>();
            m_Fade.alpha = 0f;
            m_Fade.blocksRaycasts = false;

            var img = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            img.transform.SetParent(go.transform, false);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            img.GetComponent<Image>().color = k_FadeColor;
        }

        public void Load(string sceneName)
        {
            if (m_Loading || string.IsNullOrEmpty(sceneName)) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            m_Loading = true;
            BuildOverlay();
            m_Fade.blocksRaycasts = true; // no stray taps while loading
            yield return Fade(0f, 1f, 0.3f);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[SceneFlow] Scene '{sceneName}' is not in the build settings.");
            }
            else
            {
                while (!op.isDone) yield return null;
            }
            yield return null;
            SceneLoaded?.Invoke(sceneName);
            yield return Fade(1f, 0f, 0.35f);
            m_Fade.blocksRaycasts = false;
            m_Loading = false;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                m_Fade.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            m_Fade.alpha = to;
        }
    }
}
