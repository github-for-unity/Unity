using System;
using UnityEngine;

namespace GitHub.Unity
{
    interface IView
    {
        void Refresh();
        void Redraw();
        Rect Position { get; }

        void Finish(bool result);
        event Action<bool> OnClose;
        IRepository Repository { get; }
        IApplicationManager Manager { get; }
    }
}
