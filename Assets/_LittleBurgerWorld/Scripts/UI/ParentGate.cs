using System;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBurgerWorld.UI
{
    /// <summary>
    /// Simple grown-up check before the parent area: a small sum with three answer buttons.
    /// A wrong answer just shows a new question – nothing bad happens.
    /// </summary>
    public sealed class ParentGate : MonoBehaviour
    {
        [SerializeField] private Text m_Question;
        [SerializeField] private SquishyButton[] m_Answers;
        [SerializeField] private Text[] m_AnswerLabels;
        [SerializeField] private SquishyButton m_Close;

        private int m_Correct;
        private Action m_OnPassed;
        private System.Random m_Rng = new System.Random();

        public int CorrectAnswer => m_Correct;

        private void Awake()
        {
            for (int i = 0; i < m_Answers.Length; i++)
            {
                int idx = i;
                if (m_Answers[i] != null) m_Answers[i].OnClick.AddListener(() => Choose(idx));
            }
            if (m_Close != null) m_Close.OnClick.AddListener(Hide);
        }

        public void Show(Action onPassed)
        {
            m_OnPassed = onPassed;
            gameObject.SetActive(true);
            NewQuestion();
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>Seeds the question generator (tests).</summary>
        public void SetSeed(int seed) => m_Rng = new System.Random(seed);

        private void NewQuestion()
        {
            int a = m_Rng.Next(3, 10);
            int b = m_Rng.Next(3, 10);
            m_Correct = a + b;
            if (m_Question != null)
            {
                string prefix = Core.Localization.Get("parent_gate_question");
                m_Question.text = $"{prefix}\n{a} + {b} = ?";
            }
            int correctSlot = m_Rng.Next(0, m_Answers.Length);
            var used = new System.Collections.Generic.HashSet<int> { m_Correct };
            for (int i = 0; i < m_Answers.Length; i++)
            {
                int value = m_Correct;
                if (i != correctSlot)
                {
                    do { value = m_Correct + m_Rng.Next(-4, 5); } while (used.Contains(value) || value < 2);
                    used.Add(value);
                }
                if (i < m_AnswerLabels.Length && m_AnswerLabels[i] != null) m_AnswerLabels[i].text = value.ToString();
                m_Values[i] = value;
            }
        }

        private readonly int[] m_Values = new int[8];

        public void Choose(int index)
        {
            if (index < 0 || index >= m_Answers.Length) return;
            if (m_Values[index] == m_Correct)
            {
                Hide();
                Action cb = m_OnPassed;
                m_OnPassed = null;
                cb?.Invoke();
            }
            else
            {
                NewQuestion();
            }
        }

        /// <summary>Index of the button holding the right answer (tests).</summary>
        public int CorrectIndex()
        {
            for (int i = 0; i < m_Answers.Length; i++) if (m_Values[i] == m_Correct) return i;
            return -1;
        }
    }
}
