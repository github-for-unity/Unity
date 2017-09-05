using System;
using UnityEngine;

namespace GitHub.Unity
{
    interface IView
    {
        void OnEnable();
        void OnDisable();
        void Refresh();
        void Redraw();
        Rect Position { get; }

        void Finish(bool result);
        event Action<bool> OnClose;
        IRepository Repository { get; }
        bool HasRepository { get; }
        IApplicationManager Manager { get; }
        bool IsBusy { get; }
    }
}
