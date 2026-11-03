using System.Collections.Generic;
using UnityEngine;

namespace LittleBurgerWorld.Activities
{
    /// <summary>
    /// What the activity engine needs from the game. Implemented by <see cref="ActivityManager"/>;
    /// tests can provide a fake to run activities without a scene.
    /// </summary>
    public interface IActivityContext
    {
        void ResetKitchen();
        void CallCustomer(string characterId, IList<string> request);
        /// <summary>Restricts the burger board to one ingredient (null clears).</summary>
        void SetBoardFilter(string ingredientId);
        void SetPrompt(Sprite icon);
        void ShowHint(ActivityStep step);
        void HideHint();
        /// <summary>Performs the step's action on the child's behalf. True if something happened.</summary>
        bool Assist(ActivityStep step);
        void PlayAnimation(string targetKey, string animation);
        void PlaySound(ActivityStep step);
        void ShowObject(string targetKey);
        void Highlight(string targetKey, bool on);
        void StepSucceeded(ActivityStep step);
        /// <summary>True while the child is actively doing something (dragging) – hints wait.</summary>
        bool IsChildBusy { get; }
    }
}
