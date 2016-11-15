using UnityEngine;
using UnityEditor;
using System.Threading;
using System;


namespace GitHub.Unity
{
	class TestTask : ITask
	{
		[MenuItem("Assets/GitHub/Test Blocking Critical")]
		static void TestA()
		{
			Test(new TestTask(true));
		}


		[MenuItem("Assets/GitHub/Test Non-blocking Critical")]
		static void TestB()
		{
			Test(new TestTask(false));
		}


		static void Test(TestTask task)
		{
			EditorApplication.delayCall += () => Tasks.Add(task);
		}


		TestTask(bool shouldBlock)
		{
			Blocking = shouldBlock;
			Done = false;
			Progress = 0.0f;
		}


		public bool Blocking { get; protected set; }
		public float Progress { get; protected set; }
		public bool Done { get; protected set; }
		public bool Queued { get { return true; } }
		public bool Critical { get { return true; } }
		public bool Cached { get { return true; } }
		public Action<ITask> OnEnd { get; set; }
		public string Label { get { return "Test task"; } }


		public void Run()
		{
			Debug.LogFormat("{0} start", Label);

			Done = false;
			Progress = 0.0f;

			const int
				kSteps = 10,
				kStepSleep = 1000;

			for(int step = 0; !Done && step < kSteps; ++step)
			{
				Progress = step / (float)kSteps;
				Thread.Sleep (kStepSleep);
			}

			Progress = 1.0f;
			Done = true;

			Debug.LogFormat("{0} end", Label);

			if(OnEnd != null)
			{
				OnEnd(this);
			}
		}


		public void Abort()
		{
			Debug.LogFormat("Aborting {0}", Label);

			Done = true;
		}
	}
}
