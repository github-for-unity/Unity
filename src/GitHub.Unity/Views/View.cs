using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace GitHub.Unity
{
    interface IView
    {
        void Refresh();
        void Repaint();
        void OnGUI();
        Rect position { get; }
    }

    abstract class Subview : IView
    {
        private const string NullParentError = "Subview parent is null";

        protected IView parent;

        public virtual void Refresh()
        {}

        public abstract void OnGUI();

        public void Repaint()
        {
            parent.Repaint();
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

        public Rect position
        {
            get { return parent.position; }
        }
    }
}
