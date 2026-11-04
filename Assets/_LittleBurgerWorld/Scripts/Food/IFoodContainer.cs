namespace LittleBurgerWorld.Food
{
    /// <summary>Something that holds food: burger board, grill spot, tray, counter, customer's paws.</summary>
    public interface IFoodContainer
    {
        /// <summary>The item is leaving (picked up or sent elsewhere).</summary>
        void Release(FoodItem item);
        /// <summary>Gives the container first chance to react to a tap (e.g. flip a patty). True = handled.</summary>
        bool HandleTap(FoodItem item);
        /// <summary>False while an item must stay put (cooking, being eaten, guided lock).</summary>
        bool AllowPickup(FoodItem item);
        /// <summary>
        /// A drag from this container ended nowhere: take the item back into its old place if that place is
        /// still free (restoring the station's state). False = it stays a loose item.
        /// </summary>
        bool TakeBack(FoodItem item) => false;
    }
}
