using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeviceBridge
{

    /// @brief
    /// Creates and configures the DeviceAdapter for current platform.
    /// Override this to define how plugins are set up.
    /// 
    public abstract class AdapterBuilder
    {
        /// @cond PRIVATE
        protected string mIdentifier;
        protected DeviceFeature mFeatures;
        protected IPermissionsRationale mPermissionsRationale;
        /// @endcond


        /// @brief
        /// Define name of Unity activity/process on the plugin-side. 
        ///
        public virtual AdapterBuilder setActivityIdentifier(string identifier)
        {
            this.mIdentifier = identifier;
            return this;
        }

        /// @brief
        /// Define which features of the device are used by Unity.
        ///
        public virtual AdapterBuilder setFeatures(DeviceFeature features)
        {
            this.mFeatures = features;
            return this;
        }

        
        /// @brief
        /// Set a handler for OS permission callbacks so your UI can react accordingly
        /// when user declines a needed permission.
        ///
        public AdapterBuilder setPermissionsRationale(IPermissionsRationale rationale)
        {
            mPermissionsRationale = rationale;
            return this;
        }

        /// @brief
        /// checks current platform and returns matching adapter.
        ///
        public abstract DeviceAdapter build(MonoBehaviour coroutineHost);
    }

}
