using System;

namespace GitHub.Unity
{
    public class LoginResult
    {
        public bool NeedTwoFA { get { return Data.Code == LoginResultCodes.CodeRequired || Data.Code == LoginResultCodes.CodeFailed; } }
        public bool Success { get { return Data.Code == LoginResultCodes.Success; } }
        public bool Failed { get { return Data.Code == LoginResultCodes.Failed; } }
        public string Message { get { return Data.Message; } }

        internal LoginResultData Data { get; set; }
        internal Action<bool, string> Callback { get; set; }
        internal Action<LoginResult> TwoFACallback { get; set; }

        internal LoginResult(LoginResultData data, Action<bool, string> callback, Action<LoginResult> twofaCallback)
        {
            this.Data = data;
            this.Callback = callback;
            this.TwoFACallback = twofaCallback;
        }
    }
}