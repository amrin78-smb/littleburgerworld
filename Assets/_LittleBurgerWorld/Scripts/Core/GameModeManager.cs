namespace LittleBurgerWorld.Core
{
    /// <summary>Holds the selected game mode across scene loads.</summary>
    public sealed class GameModeManager
    {
        public GameMode CurrentMode { get; private set; } = GameMode.FreePlay;

        /// <summary>Activity the guided mode should open straight into (optional).</summary>
        public string PendingActivityId { get; set; }

        public void SetMode(GameMode mode)
        {
            bool changed = mode != CurrentMode;
            CurrentMode = mode;
            if (changed) GameEvents.RaiseModeChanged(mode);
        }

        public bool IsGuided => CurrentMode == GameMode.Guided;
        public bool IsFreePlay => CurrentMode == GameMode.FreePlay;
    }
}
