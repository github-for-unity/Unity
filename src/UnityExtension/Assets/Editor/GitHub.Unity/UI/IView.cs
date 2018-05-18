using System;
using UnityEngine;

namespace GitHub.Unity
{
    interface IView : IUIEmpty, IUIProgress
    {
        void OnEnable();
        void OnDisable();
        void Refresh();
        void Redraw();
        void DoneRefreshing();
        Rect Position { get; }

        void Finish(bool result);
        IRepository Repository { get; }
        bool HasRepository { get; }
        IUser User { get; }
        bool HasUser { get; }
        IApplicationManager Manager { get; }
        bool IsBusy { get; }
        bool IsRefreshing { get; }
        bool HasFocus { get; }
    }

    interface IUIEmpty
    {
        void DoEmptyGUI();
    }

    interface IUIProgress
    {
        void DoProgressGUI();
        void UpdateProgress(IProgress progress);
    }
}
