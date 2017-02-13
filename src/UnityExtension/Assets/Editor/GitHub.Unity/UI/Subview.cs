using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace GitHub.Unity
{
    abstract class Subview : IView
    {
        private const string NullParentError = "Subview parent is null";

        protected IView parent;

        public virtual void Refresh()
        {}

        public abstract void OnGUI();

        public void Redraw()
        {
            parent.Redraw();
        }

        public void Close()
        {
          parent.Close();
        }

        public void Show(IView parentView)
        {
            Debug.Assert(parentView != null, NullParentError);

            parent = parentView;
            OnShow();
        }

        public virtual void OnSelectionChange()
        {}

        protected virtual void OnShow()
        {}

        protected virtual void OnHide()
        {}

        private void OnEnable()
        {
            if (parent != null)
            {
                OnShow();
            }
        }

        private void OnDisable()
        {
            OnHide();
        }

        public Rect Position { get { return parent.Position; } }
    }
}
