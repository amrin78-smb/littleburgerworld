using LittleBurgerWorld.Core;
using LittleBurgerWorld.Customers;
using LittleBurgerWorld.Food;
using LittleBurgerWorld.Interaction;
using UnityEngine;

namespace LittleBurgerWorld.Kitchen
{
    /// <summary>
    /// Coordinates the kitchen stations: decides where tapped food flies (so children who cannot drag
    /// yet can still play), resets work surfaces gently, and applies Guided Play restrictions.
    /// </summary>
    public sealed class KitchenManager : MonoBehaviour
    {
        public static KitchenManager Instance { get; private set; }

        [SerializeField] private BurgerAssemblyController m_Board;
        [SerializeField] private GrillStation m_Grill;
        [SerializeField] private FryStation m_Fryer;
        [SerializeField] private FriesBoxHolder m_FriesBox;
        [SerializeField] private DrinkStation m_Drinks;
        [SerializeField] private ServingTray m_Tray;
        [SerializeField] private IceCreamStation m_IceCream;

        public BurgerAssemblyController Board => m_Board;
        public GrillStation Grill => m_Grill;
        public FryStation Fryer => m_Fryer;
        public FriesBoxHolder FriesBox => m_FriesBox;
        public DrinkStation Drinks => m_Drinks;
        public ServingTray Tray => m_Tray;
        public IceCreamStation IceCream => m_IceCream;

        private void Awake()
        {
            Instance = this;
            FoodItem.AutoSendHandler = AutoSend;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (FoodItem.AutoSendHandler == (System.Func<FoodItem, bool>)AutoSend) FoodItem.AutoSendHandler = null;
        }

        /// <summary>Where should this food go next? Returns true if it flew somewhere.</summary>
        public bool AutoSend(FoodItem item)
        {
            IDropTarget target = FindAutoTarget(item);
            return target != null && item.SendTo(target);
        }

        public IDropTarget FindAutoTarget(FoodItem item)
        {
            if (item == null) return null;
            AnimalCustomerController customer = CustomerManager.Instance != null ? CustomerManager.Instance.Current : null;
            switch (item.Type)
            {
                case FoodType.Ingredient:
                    if (item.IsRawIngredient) return m_Grill != null && m_Grill.CanAccept(item) ? m_Grill : null;
                    return m_Board != null && m_Board.CanAccept(item) ? m_Board : null;
                case FoodType.FriesBatch:
                    return m_FriesBox != null && m_FriesBox.CanAccept(item) ? m_FriesBox : null;
                case FoodType.Burger:
                case FoodType.Fries:
                case FoodType.Milkshake:
                    if (m_Tray != null && item.Container != (IFoodContainer)m_Tray && m_Tray.CanAccept(item)) return m_Tray;
                    if (customer != null && customer.CanAccept(item)) return customer;
                    return null;
                case FoodType.Tray:
                case FoodType.IceCream:
                    return customer != null && customer.CanAccept(item) ? customer : null;
            }
            return null;
        }

        /// <summary>Gently tidies every station (Free Play reset button, start of a guided activity).</summary>
        public void ResetWorkSurfaces()
        {
            if (m_Board != null) m_Board.Clear();
            if (m_Grill != null) m_Grill.Clear();
            if (m_Fryer != null) m_Fryer.Clear();
            if (m_FriesBox != null) m_FriesBox.Clear();
            if (m_Drinks != null) m_Drinks.Clear();
            if (m_IceCream != null) m_IceCream.Clear();
            if (m_Tray != null)
            {
                m_Tray.Clear();
                if (m_Tray.TrayItem != null && (m_Tray.transform.position - m_Tray.TrayItem.HomePosition).sqrMagnitude > 0.01f)
                    m_Tray.ReturnToCounter();
            }
            FoodManager.VanishAll(f => !(f.Container is AnimalCustomerController));
            GameEvents.Raise(GameplayEventType.WorkSurfaceReset);
        }

        /// <summary>Guided Play: the board only accepts this ingredient (null = anything).</summary>
        public void SetExpectedIngredient(string ingredientId)
        {
            if (m_Board != null) m_Board.ExpectedIngredientId = ingredientId;
        }

        public void SetGuidedLock(bool locked)
        {
            if (m_Board != null) m_Board.LockStack = locked;
            if (!locked) SetExpectedIngredient(null);
        }
    }
}
