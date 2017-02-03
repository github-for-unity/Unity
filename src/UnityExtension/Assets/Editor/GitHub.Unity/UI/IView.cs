using UnityEngine;

namespace GitHub.Unity
{
    interface IView
    {
        void Refresh();
        void Redraw();
        void OnGUI();
        Rect Position { get; }
    }
}