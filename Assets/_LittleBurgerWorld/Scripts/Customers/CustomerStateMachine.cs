using System;
using System.Collections.Generic;

namespace LittleBurgerWorld.Customers
{
    public enum CustomerState
    {
        Idle,
        Entering,
        Waiting,
        ReceivingFood,
        Eating,
        Celebrating,
        Leaving
    }

    /// <summary>
    /// Pure customer state machine (unit tested). There is deliberately no "angry" or "impatient"
    /// state: a customer who gets no food simply keeps waiting happily.
    /// </summary>
    public sealed class CustomerStateMachine
    {
        private static readonly Dictionary<CustomerState, CustomerState[]> s_Allowed =
            new Dictionary<CustomerState, CustomerState[]>
            {
                { CustomerState.Idle, new[] { CustomerState.Entering } },
                { CustomerState.Entering, new[] { CustomerState.Waiting, CustomerState.Leaving } },
                { CustomerState.Waiting, new[] { CustomerState.ReceivingFood, CustomerState.Leaving } },
                { CustomerState.ReceivingFood, new[] { CustomerState.Eating } },
                { CustomerState.Eating, new[] { CustomerState.Celebrating } },
                { CustomerState.Celebrating, new[] { CustomerState.Leaving, CustomerState.Waiting } },
                { CustomerState.Leaving, new[] { CustomerState.Idle } }
            };

        public CustomerState State { get; private set; } = CustomerState.Idle;
        public event Action<CustomerState, CustomerState> Changed;

        public bool CanTransition(CustomerState to)
        {
            return s_Allowed.TryGetValue(State, out CustomerState[] next) && Array.IndexOf(next, to) >= 0;
        }

        public bool TryTransition(CustomerState to)
        {
            if (!CanTransition(to)) return false;
            CustomerState from = State;
            State = to;
            Changed?.Invoke(from, to);
            return true;
        }

        /// <summary>True when the customer can be handed food.</summary>
        public bool CanReceiveFood => State == CustomerState.Waiting;

        public void ForceReset()
        {
            CustomerState from = State;
            State = CustomerState.Idle;
            if (from != CustomerState.Idle) Changed?.Invoke(from, CustomerState.Idle);
        }
    }
}
