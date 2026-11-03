using UnityEngine;

namespace LittleBurgerWorld.Core
{
    /// <summary>Where the guided hint hand should point for this station right now (e.g. the patty on the grill).</summary>
    public interface IHintTarget
    {
        Vector3 HintPosition { get; }
    }
}
