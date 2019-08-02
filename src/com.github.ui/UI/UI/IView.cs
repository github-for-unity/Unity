using System;
using System.Collections.Generic;
using Unity.VersionControl.Git;
using UnityEngine;

namespace GitHub.Unity
{
    interface IView : IUIEmpty, IUIProgress
    {
        void OnEnable();
        void OnDisable();
        void Refresh();
        void Redraw();
        void Refresh(CacheType type);
        void ReceivedEvent(CacheType type);
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
        Dictionary<CacheType, int> RefreshEvents { get; }
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
