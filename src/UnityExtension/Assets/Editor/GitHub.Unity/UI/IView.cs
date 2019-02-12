using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        bool IsBusy { get; set; }
        bool IsRefreshing { get; }
        bool HasFocus { get; }
        HashSet<CacheType> RefreshEvents { get; }
    }

    interface IUIEmpty
    {
        void DoEmptyUI();
    }

    interface IUIProgress
    {
        void DoProgressUI();
        void UpdateProgress(IProgress progress);
    }
}
