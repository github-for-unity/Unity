using System;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using Rackspace.Threading;
using UnityEditor;
using UnityEngine;

namespace GitHub.Unity
{
    class LoadingView : Subview
    {
        private static readonly Vector2 viewSize = new Vector2(300, 250);

        private const string WindowTitle = "Loading...";
        private const string Header = "";


        public override void InitializeView(IView parent)
        {
            base.InitializeView(parent);
            Title = WindowTitle;
            Size = viewSize;
        }

        public override void OnGUI()
        {}

        public override bool IsBusy
        {
            get { return false; }
        }
    }
}
