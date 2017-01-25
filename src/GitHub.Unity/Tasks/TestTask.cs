using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GitHub.Unity.Logging;
using UnityEditor;

namespace GitHub.Unity
{
    class TestTask : ITask
    {
        private bool reconnecting = false;

        private TestTask(bool shouldBlock)
        {
            Blocking = shouldBlock;
            Done = false;
            Progress = 0.0f;
        }

        public static TestTask Parse(IDictionary<string, object> data)
        {
            return new TestTask(false) { reconnecting = true, Done = false };
        }

        [MenuItem("Assets/GitHub/Test Blocking Critical")]
        public static void TestA()
        {
            Test(new TestTask(true));
        }

        [MenuItem("Assets/GitHub/Test Non-blocking Critical")]
        public static void TestB()
        {
            Test(new TestTask(false));
        }

        public void Run()
        {
            Logger.Debug("{0} {1}", Label, reconnecting ? "reconnect" : "start");

            Done = false;
            Progress = 0.0f;

            if (OnBegin != null)
            {
                OnBegin(this);
            }

            const int kSteps = 20, kStepSleep = 1000;

            for (var step = 0; !Done && step < kSteps; ++step)
            {
                Progress = step / (float)kSteps;
                Thread.Sleep(kStepSleep);
            }

            Progress = 1.0f;
            Done = true;

            Logger.Debug("{0} end", Label);

            if (OnEnd != null)
            {
                OnEnd(this);
            }
        }

        public void Abort()
        {
            Logger.Debug("Aborting {0}", Label);

            Done = true;
        }

        public void Disconnect()
        {
            Abort();
        }

        public void Reconnect()
        {}

        public void WriteCache(TextWriter cache)
        {
            Logger.Debug("Writing cache for {0}", Label);
            cache.WriteLine("{");
            cache.WriteLine("\"{0}\": \"{1}\"", Tasks.TypeKey, CachedTask.TestTask);
            cache.WriteLine("}");
        }

        private static void Test(TestTask task)
        {
            EditorApplication.delayCall += () => Tasks.Add(task);
        }

        public bool Blocking { get; protected set; }
        public float Progress { get; protected set; }
        public bool Done { get; protected set; }

        public TaskQueueSetting Queued
        {
            get { return TaskQueueSetting.Queue; }
        }

        public bool Critical
        {
            get { return true; }
        }

        public bool Cached
        {
            get { return true; }
        }

        public Action<ITask> OnBegin { get; set; }
        public Action<ITask> OnEnd { get; set; }

        public string Label
        {
            get { return "Test task"; }
        }
    }
}
