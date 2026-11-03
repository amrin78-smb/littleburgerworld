using System;
using System.Collections.Generic;
using LittleBurgerWorld.Core;
using UnityEngine;

namespace LittleBurgerWorld.Activities
{
    /// <summary>
    /// Runs an <see cref="ActivityData"/> step by step. Driven by <see cref="Tick"/> so it works both in
    /// the scene and in plain unit tests.
    ///
    /// Anti-stuck guarantees:
    /// - Gameplay events are recorded; if the child did a step early, the step completes as soon as it starts.
    /// - Hints (hand + highlight) appear after a short idle time and repeat.
    /// - After several hints the game gently performs the action itself (<see cref="IActivityContext.Assist"/>).
    /// - Every non-wait step has a finite duration.
    /// </summary>
    public sealed class ActivityStepController
    {
        private const float k_HintRepeat = 5.5f;
        private const float k_SuccessPause = 0.45f;

        private readonly IActivityContext m_Context;
        private readonly List<GameplayEvent> m_History = new List<GameplayEvent>();
        private ActivityData m_Activity;
        private int m_Index = -1;
        private int m_Consumed;
        private float m_Timer;
        private float m_IdleTimer;
        private int m_HintCount;
        private bool m_Waiting;
        private bool m_HintVisible;
        private bool m_Running;
        private float m_StepDuration;

        public event Action<int, ActivityStep> StepStarted;
        public event Action<ActivityData> Completed;

        public ActivityData Activity => m_Activity;
        public int StepIndex => m_Index;
        public bool IsRunning => m_Running;
        public bool IsWaiting => m_Waiting;
        public int HintCount => m_HintCount;
        public ActivityStep CurrentStep =>
            m_Activity != null && m_Index >= 0 && m_Index < m_Activity.steps.Count ? m_Activity.steps[m_Index] : null;

        public ActivityStepController(IActivityContext context)
        {
            m_Context = context;
        }

        public void Start(ActivityData activity)
        {
            Stop();
            if (activity == null) return;
            m_Activity = activity;
            m_History.Clear();
            m_Consumed = 0;
            m_Index = -1;
            m_Running = true;
            GameEvents.Gameplay += OnGameplay;
            Advance();
        }

        public void Stop()
        {
            if (m_Running) GameEvents.Gameplay -= OnGameplay;
            m_Running = false;
            m_Waiting = false;
            HideHint();
            m_Context?.SetBoardFilter(null);
            m_Context?.SetPrompt(null, null);
        }

        /// <summary>Restarts the current activity from the beginning (replay button).</summary>
        public void Restart()
        {
            ActivityData a = m_Activity;
            Stop();
            Start(a);
        }

        private void Advance()
        {
            if (!m_Running) return;
            HideHint();
            m_Waiting = false;
            m_Index++;
            if (m_Activity == null || m_Index >= m_Activity.steps.Count)
            {
                Finish();
                return;
            }
            Enter(m_Activity.steps[m_Index]);
        }

        private void Enter(ActivityStep step)
        {
            m_Timer = 0f;
            m_IdleTimer = 0f;
            m_HintCount = 0;
            m_StepDuration = Mathf.Max(0.05f, step.duration);
            StepStarted?.Invoke(m_Index, step);

            switch (step.type)
            {
                case ActivityStepType.PrepareKitchen:
                    m_Context.ResetKitchen();
                    m_StepDuration = Mathf.Max(0.6f, step.duration);
                    break;
                case ActivityStepType.CallCustomer:
                    m_Context.CallCustomer(step.characterId, step.requestItems);
                    m_StepDuration = Mathf.Max(0.3f, step.duration);
                    break;
                case ActivityStepType.ShowObject:
                    m_Context.ShowObject(step.targetKey);
                    break;
                case ActivityStepType.HighlightObject:
                    m_Context.Highlight(step.targetKey, true);
                    break;
                case ActivityStepType.DemonstrateDrag:
                    m_Context.ShowHint(step);
                    m_HintVisible = true;
                    break;
                case ActivityStepType.PlayAnimation:
                    m_Context.PlayAnimation(step.targetKey, step.animation);
                    break;
                case ActivityStepType.PlayAudio:
                    m_Context.PlaySound(step);
                    break;
                case ActivityStepType.Wait:
                    break;
                case ActivityStepType.CompleteActivity:
                    Finish();
                    return;
                default:
                    BeginWait(step);
                    return;
            }
        }

        private void BeginWait(ActivityStep step)
        {
            m_Waiting = true;
            m_Context.SetPrompt(step.promptIcon, step.PromptTextKey);
            if (step.type == ActivityStepType.WaitForDrop && IsBoardKey(step.targetKey) && !string.IsNullOrEmpty(step.itemId))
                m_Context.SetBoardFilter(step.itemId);
            else
                m_Context.SetBoardFilter(null);
            if (!string.IsNullOrEmpty(step.voiceKey) || step.sound != Audio.SfxId.None) m_Context.PlaySound(step);

            // Already done ahead of time?
            for (int i = m_Consumed; i < m_History.Count; i++)
            {
                if (!Matches(step, m_History[i])) continue;
                m_Consumed = i + 1;
                Satisfy(step);
                return;
            }
        }

        private static bool IsBoardKey(string key) => key == "Board";

        public void Tick(float dt)
        {
            if (!m_Running) return;
            ActivityStep step = CurrentStep;
            if (step == null) return;
            m_Timer += dt;

            if (!m_Waiting)
            {
                if (m_Timer >= m_StepDuration)
                {
                    if (step.type == ActivityStepType.HighlightObject) m_Context.Highlight(step.targetKey, false);
                    Advance();
                }
                return;
            }

            if (m_Context.IsChildBusy)
            {
                m_IdleTimer = 0f;
                if (m_HintVisible) HideHint();
                return;
            }

            m_IdleTimer += dt;
            float delay = m_HintCount == 0 ? Mathf.Max(0.5f, step.hintDelay) : k_HintRepeat;
            if (m_IdleTimer < delay) return;
            m_IdleTimer = 0f;

            if (step.autoAssistAfterHints > 0 && m_HintCount >= step.autoAssistAfterHints)
            {
                HideHint();
                m_Context.Assist(step);
                m_HintCount = 1; // keep hinting if the assist could not act yet
                return;
            }
            m_HintCount++;
            m_Context.ShowHint(step);
            m_HintVisible = true;
        }

        private void HideHint()
        {
            if (!m_HintVisible) return;
            m_HintVisible = false;
            m_Context?.HideHint();
        }

        private void OnGameplay(GameplayEvent e)
        {
            if (!m_Running) return;
            m_History.Add(e);
            ActivityStep step = CurrentStep;
            if (!m_Waiting || step == null) return;
            if (!Matches(step, e)) return;
            m_Consumed = m_History.Count;
            Satisfy(step);
        }

        private void Satisfy(ActivityStep step)
        {
            m_Waiting = false;
            HideHint();
            m_Context.SetPrompt(null, null);
            m_Context.SetBoardFilter(null);
            m_Context.StepSucceeded(step);
            // Short pause so the child sees the result before the next prompt appears;
            // Tick() advances once the pause has elapsed (the step is no longer waiting).
            m_Timer = 0f;
            m_StepDuration = k_SuccessPause;
        }

        /// <summary>Pure matching rule (unit tested).</summary>
        public static bool Matches(ActivityStep step, GameplayEvent e)
        {
            if (step == null) return false;
            bool itemOk = string.IsNullOrEmpty(step.itemId) || e.ItemId == step.itemId || e.ExtraId == step.itemId;
            bool targetOk = string.IsNullOrEmpty(step.targetKey) || e.TargetKey == step.targetKey;
            switch (step.type)
            {
                case ActivityStepType.WaitForSelection:
                    return e.Type == GameplayEventType.ItemPicked && itemOk;
                case ActivityStepType.WaitForDrop:
                    switch (e.Type)
                    {
                        case GameplayEventType.IngredientStacked:
                        case GameplayEventType.PattyOnGrill:
                        case GameplayEventType.TrayItemAdded:
                        case GameplayEventType.CustomerServed:
                        case GameplayEventType.ItemDropped:
                            return itemOk && targetOk && !string.IsNullOrEmpty(e.TargetKey);
                    }
                    return false;
                case ActivityStepType.WaitForTap:
                    return e.Type == GameplayEventType.ObjectTapped && targetOk;
                case ActivityStepType.WaitForEvent:
                    return e.Type == step.eventType && itemOk;
            }
            return false;
        }

        private void Finish()
        {
            if (!m_Running) return;
            ActivityData a = m_Activity;
            Stop();
            m_Index = a != null ? a.steps.Count : 0;
            Completed?.Invoke(a);
        }
    }
}
