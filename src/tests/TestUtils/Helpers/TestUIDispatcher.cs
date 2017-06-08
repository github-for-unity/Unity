using System;
using GitHub.Unity;

namespace TestUtils
{
    class TestUIDispatcher : BaseUIDispatcher
    {
        private readonly Func<bool> callback;

        public TestUIDispatcher(Func<bool> callback = null) : base()
        {
            this.callback = callback;
        }

        protected override void Run(Action<bool> onClose)
        {
            bool ret = true;
            if (callback != null)
            {
                ret = callback();
            }
            onClose(ret);
            base.Run(onClose);
        }
    }
}