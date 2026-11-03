namespace LittleBurgerWorld.Core
{
    /// <summary>
    /// Implemented by objects that can perform their action on the child's behalf when a guided
    /// step has been hinted several times without success. Guarantees an activity never gets stuck.
    /// </summary>
    public interface IAssistable
    {
        /// <param name="itemId">Item the step is about (may be null).</param>
        /// <param name="targetKey">Where it should end up (may be null).</param>
        /// <returns>True if something was done.</returns>
        bool Assist(string itemId, string targetKey);
    }
}
