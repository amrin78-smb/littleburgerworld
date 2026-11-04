namespace LittleBurgerWorld.Audio
{
    /// <summary>Every sound effect the game can play. Clips are mapped in <see cref="AudioLibrary"/>.</summary>
    public enum SfxId
    {
        None = 0,
        Pop,            // ingredient selected
        Plop,           // ingredient dropped
        Snap,           // ingredient snapped into place
        Whoosh,         // item flies
        Sizzle,         // grill loop
        Flip,           // patty flip
        Ding,           // something finished cooking
        Bubble,         // fryer loop
        FriesReady,     // fries prepared
        Pour,           // milkshake filling loop
        Cream,          // whipped cream
        Straw,          // straw added
        Jingle,         // burger completed
        Celebration,    // activity / milkshake completed
        ThankYou,       // customer served
        Munch,          // customer eating
        Happy,          // customer happy squeal
        ButtonTap,      // UI tap
        Sparkle,        // magic sparkle
        BalloonPop,
        Star,           // star earned
        DoorChime,      // customer enters
        Boing,          // gentle "not here" bounce (never negative)
        Footstep,
        Squeeze,        // sauce bottle
        Scrub,          // washing-up sponge
        Capsule,        // toy capsule machine
        Chime,          // theme / surprise reveal
        Rain            // rainy-day loop
    }
}
