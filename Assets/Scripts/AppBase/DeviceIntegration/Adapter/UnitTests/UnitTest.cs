using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//@cond PRIVATE

namespace DeviceBridge.Tests
{

    public abstract class UnitTest
    {
        private System.Action<bool> callback;

        protected DeviceAdapter adapter { get; private set; }
        protected bool logAll { get; private set; }

        public void Begin(DeviceAdapter adapter, bool logAll=false, System.Action<bool> whenDone=null)
        {
            this.adapter = adapter;
            this.callback = whenDone;
            this.logAll = logAll;

            beginUnitTest();
        }

        public abstract void beginUnitTest();


        protected void finishTest(bool success, string errorMsg="")
        {
            if(success)
            {
                if(logAll)
                {
                    Debug.Log("UnitTest<" + this.GetType() + "> succeded!");
                }
            }
            else
            {
                Debug.LogError("UnitTest<" + this.GetType() + "> failed! message=\n" + errorMsg);
            }
            callback?.Invoke(success);
        }
    }

}


/// @endcond