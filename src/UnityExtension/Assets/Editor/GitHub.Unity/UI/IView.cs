using UnityEngine;

namespace GitHub.Unity
{
    interface IView
    {
        void Refresh();
        void Redraw();
        void OnGUI();
        void Close();
        Rect Position { get; }
    }
}
