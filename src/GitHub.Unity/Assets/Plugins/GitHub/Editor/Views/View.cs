using UnityEngine;
using UnityEditor;


namespace GitHub.Unity
{
	interface IView
	{
		Rect position { get; }
		void Refresh();
		void Repaint();
		void OnGUI();
	}


	abstract class Subview : ScriptableObject, IView
	{
		const string NullParentError = "Subview parent is null";


		protected IView parent;


		public Rect position
		{
			get
			{
				return parent.position;
			}
		}


		public void Show(IView parent)
		{
			System.Diagnostics.Debug.Assert(parent != null, NullParentError);

			this.parent = parent;
			OnShow();
		}


		void OnEnable()
		{
			if (parent != null)
			{
				OnShow();
			}
		}


		void OnDisable()
		{
			OnHide();
		}


		protected virtual void OnShow() {}
		protected virtual void OnHide() {}


		public virtual void Refresh() {}
		public virtual void OnSelectionChange() {}
		public abstract void OnGUI();


		public void Repaint()
		{
			parent.Repaint();
		}
	}
}
