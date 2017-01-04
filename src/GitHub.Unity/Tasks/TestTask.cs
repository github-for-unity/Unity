using UnityEngine;
using UnityEditor;
using System.Threading;
using System;
using System.IO;
using System.Collections.Generic;


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


        public static TestTask Parse(IDictionary<string, object> data)
        {
            return new TestTask(false)
            {
                reconnecting = true,
                Done = false
            };
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
        public TaskQueueSetting Queued { get { return TaskQueueSetting.Queue; } }
        public bool Critical { get { return true; } }
        public bool Cached { get { return true; } }
        public Action<ITask> OnBegin { get; set; }
        public Action<ITask> OnEnd { get; set; }
        public string Label { get { return "Test task"; } }


        bool reconnecting = false;


        public void Run()
        {
            Debug.LogFormat("{0} {1}", Label, reconnecting ? "reconnect" : "start");

            Done = false;
            Progress = 0.0f;

            if(OnBegin != null)
            {
                OnBegin(this);
            }

            const int
                kSteps = 20,
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


        public void Disconnect()
        {
            Abort();
        }


        public void Reconnect()
        {}


        public void WriteCache(TextWriter cache)
        {
            Debug.LogFormat("Writing cache for {0}", Label);

            cache.Write(
@"{{
    ""{0}"": ""{1}""
}}",
                Tasks.TypeKey,      CachedTask.TestTask
            );
        }
    }
}
