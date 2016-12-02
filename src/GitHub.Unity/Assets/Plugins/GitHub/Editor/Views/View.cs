using UnityEngine;
using UnityEditor;


namespace GitHub.Unity
{
	interface IView
	{
		Rect position { get; }
		void OnGUI();
		void Repaint();
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


		protected abstract void OnShow();
		protected abstract void OnHide();


		public abstract void OnGUI();


		public void Repaint()
		{
			parent.Repaint();
		}
	}
}
