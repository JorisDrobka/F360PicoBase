using System.Collections;

namespace Utility.Pooling
{
	/// <summary>
	/// Objects implementing IPoolComponent receive callbacks 
	/// when they are created / collected by the PrefabPool.
	/// </summary>
    interface IPoolComponent
    {
        /// <summary>
        /// Called when the object is being requested and about to enter the game.
        /// </summary>
        void Activate();

        /// <summary>
        /// Called when the object is being killed.
        /// </summary>
        void Deactivate();

        /// <summary>
        /// Called once at the end of the current frame, before Update is called for the first time. Analogous to Monobehaviour's Start()
        /// </summary>
        void PostActivate();
    }

	/// <summary>
	/// allows Pooled objects to define a timed deactivation method
	/// </summary>
	interface ITimedPoolComponent : IPoolComponent
	{
		/// <summary>
		/// Write a timed deactivation process - IPoolComponent.Deactivate() should 
		/// still be used for setting states & values
		/// </summary>
		IEnumerator ProcessTimedDeactivate(System.Action<ITimedPoolComponent> callback);
	}

}