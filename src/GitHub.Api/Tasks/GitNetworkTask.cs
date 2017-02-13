using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Api;

namespace GitHub.Unity
{
    class GitNetworkTask : GitTask
    {
        private readonly ICredentialManager credentialManager;
        private readonly IUIDispatcher uiDispatcher;

        public GitNetworkTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
            ICredentialManager credentialManager, IUIDispatcher uiDispatcher,
            Action<string> onSuccess, Action onFailure)
            : base(environment, processManager, resultDispatcher, onSuccess, onFailure)
        {
            Guard.ArgumentNotNull(credentialManager, nameof(credentialManager));
            Guard.ArgumentNotNull(uiDispatcher, nameof(uiDispatcher));

            this.credentialManager = credentialManager;
            this.uiDispatcher = uiDispatcher;
        }

        public override async void Run()
        {
            await RunAsync(new CancellationToken());
        }

        public override async Task<bool> RunAsync(CancellationToken cancel)
        {
            if (string.IsNullOrEmpty(Environment.GitInstallPath))
            {
                TaskResultDispatcher.ReportFailure(FailureSeverity.Moderate, this, Localization.NoGitError);
                Abort();
            }

            string remoteUrl = null;
            var task = new GitTask(Environment, ProcessManager, null, x => remoteUrl = x, null);
            task.SetArguments("remote get-url origin-http");

            var ret = await task.RunAsync(cancel);
            if (!ret || remoteUrl == null)
            {
                Logger.Debug("Could not get url of 'origin' remote");
                return false;
            }

            var host = HostAddress.Create(remoteUrl);
            var cred = await credentialManager.Load(host);

            if (cred == null)
            {
                ret = await uiDispatcher.RunUI("authentication");
            }

            return await base.RunAsync(cancel);
        }
    }

    interface IUIDispatcher
    {
        Task<bool> RunUI(string ui);
    }

    class BaseUIDispatcher : IUIDispatcher
    {
        private static readonly ILogging logger = Logging.GetLogger<BaseUIDispatcher>();

        public event Action<bool> OnClose;

        public async Task<bool> RunUI(string ui)
        {
            Action onClose = () => { RaiseOnClose(); };
            var ret = await TaskExt.FromEvent<Action<bool>, bool>(
                getHandler: (completeAction, cancelAction, rejectAction) =>
                    (eventArgs) => completeAction(eventArgs),
                subscribe: eventHandler =>
                    this.OnClose += eventHandler,
                unsubscribe: eventHandler =>
                    this.OnClose -= eventHandler,
                initiate: (completeAction, cancelAction, rejectAction) =>
                    { Run(onClose); },
                token: CancellationToken.None);

            logger.Debug("Authentication done");
            return ret;
        }

        protected virtual void Run(Action onClose)
        {
        }

        protected void RaiseOnClose()
        {
            OnClose?.Invoke(true);
        }
    }
}