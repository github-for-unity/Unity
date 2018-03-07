﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Threading;
using GitHub.Unity;
using System.Threading.Tasks.Schedulers;
using System.IO;
using NSubstitute;
using GitHub.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using FluentAssertions;

namespace IntegrationTests
{
    class BaseTest
    {
        protected const int Timeout = 30000;

        public BaseTest()
        {
            Logger = LogHelper.GetLogger(GetType());
        }

        protected ILogging Logger { get; }

        protected ITaskManager TaskManager { get; set; }
        protected IProcessManager ProcessManager { get; set; }
        protected NPath TestBasePath { get; private set; }
        protected CancellationToken Token => TaskManager.Token;
        protected NPath TestApp => System.Reflection.Assembly.GetExecutingAssembly().Location.ToNPath().Combine("TestApp.exe");

        [TestFixtureSetUp]
        public void OneTimeSetup()
        {
            GitHub.Unity.Guard.InUnitTestRunner = true;
            LogHelper.LogAdapter = new MultipleLogAdapter(new FileLogAdapter($"..\\{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}-tasksystem-tests.log"));
            //LogHelper.TracingEnabled = true;
            TaskManager = new TaskManager();
            var syncContext = new ThreadSynchronizationContext(Token);
            TaskManager.UIScheduler = new SynchronizationContextTaskScheduler(syncContext);

            var env = new DefaultEnvironment();
            TestBasePath = NPath.CreateTempDirectory("integration-tests");
            env.FileSystem.SetCurrentDirectory(TestBasePath);

            var repo = Substitute.For<IRepository>();
            repo.LocalPath.Returns(TestBasePath);
            env.Repository = repo;

            var platform = new Platform(env);
            ProcessManager = new ProcessManager(env, platform.GitEnvironment, Token);
            var processEnv = platform.GitEnvironment;
            var path = new ProcessTask<NPath>(TaskManager.Token, new FirstLineIsPathOutputProcessor())
                .Configure(ProcessManager, env.IsWindows ? "where" : "which", "git")
                .Start().Result;
            env.GitExecutablePath = path ?? "git".ToNPath();
        }

        [TestFixtureTearDown]
        public void OneTimeTearDown()
        {
            TaskManager?.Dispose();
            try
            {
                TestBasePath.DeleteIfExists();
            }
            catch { }
        }

        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void Teardown()
        {
        }

        protected void StartTest(out Stopwatch watch, out ILogging logger, [CallerMemberName] string testName = "test")
        {
            watch = new Stopwatch();
            logger = LogHelper.GetLogger(testName);
            logger.Trace("Starting test");
        }

        protected void StartTrackTime(Stopwatch watch, ILogging logger = null, string message = "")
        {
            if (!String.IsNullOrEmpty(message))
                logger.Trace(message);
            watch.Reset();
            watch.Start();
        }

        protected void StopTrackTimeAndLog(Stopwatch watch, ILogging logger)
        {
            watch.Stop();
            logger.Trace($"Time: {watch.ElapsedMilliseconds}");
        }
    }

    [TestFixture]
    class ProcessTaskTests : BaseTest
    {
        [Test]
        public async Task ProcessReadsFromStandardInput()
        {
            var input = new List<string> {
                "Hello",
                "World\u001A"
            };

            var expectedOutput = "Hello";

            var procTask = new FirstNonNullLineProcessTask(Token, TestApp, @"-s 100 -i")
                .Configure(ProcessManager, true);

            procTask.OnStartProcess += proc =>
            {
                foreach (var item in input)
                {
                    proc.StandardInput.WriteLine(item);
                }
                proc.StandardInput.Close();
            };

            var chain = procTask
                .Finally((s, e, d) => d, TaskAffinity.Concurrent);

            var output = await chain.StartAsAsync();

            Assert.AreEqual(expectedOutput, output);
        }

        [Test]
        public async Task ProcessOnStartOnEndTaskOrder()
        {
            var values = new List<string>();
            string process1Value = null;
            string process2Value = null;

            var process1Task = new FirstNonNullLineProcessTask(Token, TestApp, @"-s 100 -d process1")
                .Configure(ProcessManager, true).Then((b, s) => {
                    process1Value = s;
                    values.Add(s);
                });

            var process2Task = new FirstNonNullLineProcessTask(Token, TestApp, @"-s 100 -d process2")
                .Configure(ProcessManager, true).Then((b, s) => {
                    process2Value = s;
                    values.Add(s);
                });

            var combinedTask = process1Task
                .Then(process2Task);

            combinedTask.OnStart += task => {
                values.Add("OnStart");
            };

            combinedTask.OnEnd += (task, success, ex) => {
                values.Add("OnEnd");
            };

            await combinedTask
                .StartAsAsync();

            Assert.AreEqual(process1Value, "process1");
            Assert.AreEqual(process2Value, "process2");
            Assert.True(values.SequenceEqual(new []{ "process1", "OnStart", "process2", "OnEnd" }));
        }

        [Test]
        public async Task ProcessReturningErrorThrowsException()
        {
            var success = false;
            Exception thrown = null;
            var output = new List<string>();
            var expectedOutput = new List<string> { "one name" };

            var task = new FirstNonNullLineProcessTask(Token, TestApp, @"-s 100 -d ""one name""").Configure(ProcessManager)
                .Catch(ex => thrown = ex)
                .Then((s, d) => output.Add(d))
                .Then(new FirstNonNullLineProcessTask(Token, TestApp, @"-e kaboom -r -1").Configure(ProcessManager))
                .Catch(ex => thrown = ex)
                .Then((s, d) => output.Add(d))
                .Finally((s, e) => success = s, TaskAffinity.Concurrent);

            await task.StartAndSwallowException();

            Assert.IsFalse(success);
            CollectionAssert.AreEqual(expectedOutput, output);
            Assert.IsNotNull(thrown);
            Assert.AreEqual("kaboom", thrown.Message);
        }

        [Test]
        public async Task NestedProcessShouldChainCorrectly()
        {
            var expected = new List<string>() { "BeforeProcess", "ProcessOutput", "ProcessFinally" };

            var results = new List<string>() { };

            await new ActionTask(Token, _ => {
                results.Add("BeforeProcess");
            })
                .Then(new FirstNonNullLineProcessTask(Token, TestApp, @"-s 1000 -d ""ok""")
                    .Configure(ProcessManager)
                    .Then(new FuncTask<int>(Token, (b, i) => {
                        results.Add("ProcessOutput");
                        return 1234;
                    }))
                    .Finally((b, exception) => {
                        results.Add("ProcessFinally");
                    }, TaskAffinity.Concurrent))
                .StartAsAsync();

            CollectionAssert.AreEqual(expected, results);
        }
    }

    [TestFixture]
    class SchedulerTests : BaseTest
    {
        private ActionTask GetTask(TaskAffinity affinity, int id, Action<int> body)
        {
            return new ActionTask(Token, _ => body(id)) { Affinity = affinity };
        }

        /// <summary>
        /// This exemplifies that running a bunch of tasks that don't depend on anything on the concurrent (default) scheduler
        /// run in any order
        /// </summary>
        [Test]
        public void ConcurrentSchedulerDoesNotGuaranteeOrdering()
        {
            var runningOrder = new List<int>();
            var rand = new Randomizer();
            var tasks = new List<ActionTask>();
            for (int i = 1; i < 11; i++)
            {
                tasks.Add(GetTask(TaskAffinity.Concurrent, i, id => { new ManualResetEventSlim().Wait(rand.Next(100, 200)); lock (runningOrder) runningOrder.Add(id); }));
            }

            TaskManager.Schedule(tasks.Cast<ITask>().ToArray());
            Task.WaitAll(tasks.Select(x => x.Task).ToArray());
            //Console.WriteLine(String.Join(",", runningOrder.Select(x => x.ToString()).ToArray()));
            Assert.AreEqual(10, runningOrder.Count);
        }

        /// <summary>
        /// This exemplifies that running a bunch of tasks that depend on other things on the concurrent (default) scheduler
        /// run in dependency order. Each group of tasks depends on a task on the previous group, so the first group
        /// runs first, then the second group of tasks, then the third. Run order within each group is not guaranteed
        /// </summary>
        [Test]
        public void ConcurrentSchedulerWithDependencyOrdering()
        {
            var count = 3;
            var runningOrder = new List<int>();
            var rand = new Randomizer();
            var startTasks = new List<ActionTask>();
            var i = 1;
            for (var start = i; i < start + count; i++)
            {
                startTasks.Add(GetTask(TaskAffinity.Concurrent, i,
                    id => { new ManualResetEventSlim().Wait(rand.Next(100, 200)); lock (runningOrder) runningOrder.Add(id); }));
            }

            var midTasks = new List<ActionTask>();
            for (var start = i; i < start + count; i++)
            {
                midTasks.Add(startTasks[i - 4].Then(
                        GetTask(TaskAffinity.Concurrent, i,
                            id => { new ManualResetEventSlim().Wait(rand.Next(100, 200)); lock (runningOrder) runningOrder.Add(id); }))
                    );;
            }

            var endTasks = new List<ActionTask>();
            for (var start = i; i < start + count; i++)
            {
                endTasks.Add(midTasks[i - 7].Then(
                    GetTask(TaskAffinity.Concurrent, i,
                        id => { new ManualResetEventSlim().Wait(rand.Next(100, 200)); lock (runningOrder) runningOrder.Add(id); }
                        )));
            }

            foreach (var t in endTasks)
                t.Start();
            Task.WaitAll(endTasks.Select(x => x.Task).ToArray());

            CollectionAssert.AreEquivalent(Enumerable.Range(1, 3), runningOrder.Take(3));
            CollectionAssert.AreEquivalent(Enumerable.Range(4, 3), runningOrder.Skip(3).Take(3));
            CollectionAssert.AreEquivalent(Enumerable.Range(7, 3), runningOrder.Skip(6).Take(3));
        }

        [Test]
        public void ExclusiveSchedulerGuaranteesOrdering()
        {
            var runningOrder = new List<int>();
            var tasks = new List<ActionTask>();
            var rand = new Randomizer();
            for (int i = 1; i < 11; i++)
            {
                tasks.Add(GetTask(TaskAffinity.Exclusive, i, id => { new ManualResetEventSlim().Wait(rand.Next(100, 200)); lock (runningOrder) runningOrder.Add(id); }));
            }

            TaskManager.Schedule(tasks.Cast<ITask>().ToArray());
            Task.WaitAll(tasks.Select(x => x.Task).ToArray());
            Assert.AreEqual(Enumerable.Range(1, 10), runningOrder);
        }

        [Test]
        public void UISchedulerGuaranteesOrdering()
        {
            var runningOrder = new List<int>();
            var tasks = new List<ActionTask>();
            var rand = new Randomizer();
            for (int i = 1; i < 11; i++)
            {
                tasks.Add(GetTask(TaskAffinity.UI, i, id => { new ManualResetEventSlim().Wait(rand.Next(100, 200)); lock (runningOrder) runningOrder.Add(id); }));
            }

            TaskManager.Schedule(tasks.Cast<ITask>().ToArray());
            Task.WaitAll(tasks.Select(x => x.Task).ToArray());
            Assert.AreEqual(Enumerable.Range(1, 10), runningOrder);
        }

        [Test]
        public async void NonUITasksAlwaysRunOnDifferentThreadFromUITasks()
        {
            var output = new Dictionary<int, int>();
            var tasks = new List<ITask>();
            var seed = Randomizer.RandomSeed;

            var uiThread = 0;
            await new ActionTask(Token, _ => uiThread = Thread.CurrentThread.ManagedThreadId) { Affinity = TaskAffinity.UI }
                .StartAsAsync();

            for (int i = 1; i < 100; i++)
            {
                tasks.Add(GetTask(i % 2 == 0 ? TaskAffinity.Concurrent : TaskAffinity.Exclusive, i,
                    id => { lock (output) output.Add(id, Thread.CurrentThread.ManagedThreadId); })
                    .Start());
            }

            Task.WaitAll(tasks.Select(x => x.Task).ToArray());
            CollectionAssert.DoesNotContain(output.Values, uiThread);
        }


        [Test]
        public async void ChainingOnDifferentSchedulers()
        {
            var output = new Dictionary<int, KeyValuePair<int, int>>();
            var tasks = new List<ITask>();
            var seed = Randomizer.RandomSeed;

            var uiThread = 0;
            await new ActionTask(Token, _ => uiThread = Thread.CurrentThread.ManagedThreadId) { Affinity = TaskAffinity.UI }
                .StartAsAsync();

            for (int i = 1; i < 100; i++)
            {
                tasks.Add(
                    GetTask(TaskAffinity.UI, i,
                        id => { lock (output) output.Add(id, KeyValuePair.Create(Thread.CurrentThread.ManagedThreadId, -1)); })
                    .Then(
                    GetTask(i % 2 == 0 ? TaskAffinity.Concurrent : TaskAffinity.Exclusive, i,
                        id => { lock (output) output[id] = KeyValuePair.Create(output[id].Key, Thread.CurrentThread.ManagedThreadId); })
                    ).
                    Start());
            }

            Task.WaitAll(tasks.Select(x => x.Task).ToArray());
            //Console.WriteLine(String.Join(",", output.Select(x => x.Key.ToString()).ToArray()));
            foreach (var t in output)
            {
                Assert.AreEqual(uiThread, t.Value.Key, $"Task {t.Key} pass 1 should have been on ui thread {uiThread} but ran instead on {t.Value.Key}");
                Assert.AreNotEqual(t.Value.Key, t.Value.Value, $"Task {t.Key} pass 2 should not have been on ui thread {uiThread}");
            }
        }
    }

    [TestFixture]
    class Chains : BaseTest
    {
        [Test]
        public async Task ThrowingInterruptsTaskChainButAlwaysRunsFinallyAndCatch()
        {
            var success = false;
            string thrown = "";
            Exception finallyException = null;
            var output = new List<string>();
            var expectedOutput = new List<string> { "one name" };

            var task =
                new FuncTask<string>(Token, _ => "one name") { Affinity = TaskAffinity.UI }
                .Then((s, d) => output.Add(d))
                .Then(_ => { throw new Exception("an exception"); })
                .Catch(ex => thrown = ex.Message)
                .Then(new FuncTask<string>(Token, _ => "another name") { Affinity = TaskAffinity.Exclusive })
                .ThenInUI((s, d) => output.Add(d))
                .Finally((s, e) =>
                {
                    success = s;
                    finallyException = e;
                }, TaskAffinity.Concurrent);

            await task.StartAndSwallowException();

            Assert.IsFalse(success);
            CollectionAssert.AreEqual(expectedOutput, output);
            Assert.IsNotNull(finallyException);
        }

        [Test]
        public async Task FinallyReportsException()
        {
            var success = false;
            Exception finallyException = null;
            var output = new List<string>();
            var expectedOutput = new List<string> { "one name" };

            var task =
                new FuncTask<string>(Token, _ => "one name") { Affinity = TaskAffinity.UI }
                .Then((s, d) => output.Add(d))
                .Then(_ => { throw new Exception("an exception"); })
                .Then(new FuncTask<string>(Token, _ => "another name") { Affinity = TaskAffinity.Exclusive })
                .ThenInUI((s, d) => output.Add(d))
                .Finally((s, e) =>
                {
                    success = s;
                    finallyException = e;
                }, TaskAffinity.Concurrent);

            await task.StartAndSwallowException();

            Assert.IsFalse(success);
            CollectionAssert.AreEqual(expectedOutput, output);
            Assert.IsNotNull(finallyException);
            Assert.AreEqual("an exception", finallyException.Message);
        }

        [Test]
        public async Task CatchAlwaysRunsBeforeFinally()
        {
            var success = false;
            Exception exception = null;
            Exception finallyException = null;
            var runOrder = new List<string>();
            var output = new List<string>();
            var expectedOutput = new List<string> { "one name" };

            var task =
                new FuncTask<string>(Token, _ => "one name") { Affinity = TaskAffinity.UI }
                .Then((s, d) => output.Add(d))
                .Then(_ => { throw new Exception("an exception"); })
                .Then(new FuncTask<string>(Token, _ => "another name") { Affinity = TaskAffinity.Exclusive })
                .Then((s, d) =>
                {
                    output.Add(d);
                    return "done";
                })
                .Catch(ex =>
                {
                    lock (runOrder)
                    {
                        exception = ex;
                        runOrder.Add("catch");
                    }
                })
                .Finally((s, e, d) =>
                {
                    lock (runOrder)
                    {
                        success = s;
                        finallyException = e;
                        runOrder.Add("finally");
                    }
                    return d;
                }, TaskAffinity.Concurrent);

            await task.StartAndSwallowException();

            Assert.IsFalse(success);
            CollectionAssert.AreEqual(expectedOutput, output);
            Assert.IsNotNull(exception);
            Assert.IsNotNull(finallyException);
            Assert.AreEqual("an exception", exception.Message);
            Assert.AreEqual("an exception", finallyException.Message);
            CollectionAssert.AreEqual(new List<string> { "catch", "finally" }, runOrder);
        }

        [Test]
        public async Task YouCanUseCatchAtTheEndOfAChain()
        {
            var success = false;
            Exception exception = null;
            var output = new List<string>();
            var expectedOutput = new List<string> { "one name" };

            var task =
                new FuncTask<string>(Token, _ => "one name") { Affinity = TaskAffinity.UI }
                .Then((s, d) => output.Add(d))
                .Then(_ => { throw new Exception("an exception"); })
                .Then(new FuncTask<string>(Token, _ => "another name") { Affinity = TaskAffinity.Exclusive })
                .ThenInUI((s, d) => output.Add(d))
                .Finally((_, __) => { }, TaskAffinity.Concurrent)
                .Catch(ex => { exception = ex; });

            await task.Start().Task;

            Assert.IsFalse(success);
            CollectionAssert.AreEqual(expectedOutput, output);
            Assert.IsNotNull(exception);
        }

        [Test]
        public async Task FinallyCanReturnData()
        {
            var success = false;
            Exception exception = null;
            Exception finallyException = null;
            var runOrder = new List<string>();
            var output = new List<string>();
            var expectedOutput = new List<string> { "one name", "another name", "done" };

            var task =
                new FuncTask<string>(Token, _ => "one name") { Affinity = TaskAffinity.UI }
                .Then((s, d) => output.Add(d))
                .Then(new FuncTask<string>(Token, _ => "another name") { Affinity = TaskAffinity.Exclusive })
                .Then((s, d) =>
                {
                    output.Add(d);
                    return "done";
                })
                .Catch(ex =>
                {
                    lock (runOrder)
                    {
                        exception = ex;
                        runOrder.Add("catch");
                    }
                })
                .Finally((s, e, d) =>
                {
                    lock (runOrder)
                    {
                        success = s;
                        output.Add(d);
                        finallyException = e;
                        runOrder.Add("finally");
                    }
                    return d;
                }, TaskAffinity.Concurrent)
                .ThenInUI((s, d) =>
                {
                    lock (runOrder)
                    {
                        runOrder.Add("boo");
                    }
                    return d;
                });

            var ret = await task.StartAsAsync();
            Assert.AreEqual("done", ret);
            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedOutput, output);
            Assert.IsNull(exception);
            Assert.IsNull(finallyException);
            CollectionAssert.AreEqual(new List<string> { "finally", "boo" }, runOrder);
        }

        [Test]
        public async Task FinallyCanAlsoNotReturnData()
        {
            var success = false;
            Exception exception = null;
            Exception finallyException = null;
            var runOrder = new List<string>();
            var output = new List<string>();
            var expectedOutput = new List<string> { "one name", "another name", "done" };

            var task =
                new FuncTask<string>(Token, _ => "one name") { Affinity = TaskAffinity.UI }
                .Then((s, d) => output.Add(d))
                .Then(new FuncTask<string>(Token, _ => "another name") { Affinity = TaskAffinity.Exclusive })
                .Then((s, d) =>
                {
                    output.Add(d);
                    return "done";
                })
                .Finally((s, e, d) =>
                {
                    lock (runOrder)
                    {
                        success = s;
                        output.Add(d);
                        finallyException = e;
                        runOrder.Add("finally");
                    }
                }, TaskAffinity.Concurrent);

            await task.StartAsAsync();

            Assert.IsTrue(success);
            CollectionAssert.AreEqual(expectedOutput, output);
            Assert.IsNull(exception);
            Assert.IsNull(finallyException);
            CollectionAssert.AreEqual(new List<string> { "finally" }, runOrder);
        }
    }

    [TestFixture]
    class Exceptions : BaseTest
    {
        [Test]
        public async Task StartAndEndAreAlwaysRaised()
        {
            var runOrder = new List<string>();
            ITask task = new ActionTask(Token, _ => { throw new Exception(); });
            task.OnStart += _ => runOrder.Add("start");
            task.OnEnd += (_, __, ___) => runOrder.Add("end");
            // we want to run a Finally on a new task (and not in-thread) so that the StartAndSwallowException handler runs after this
            // one, proving that the exception is propagated after everything is done
            task = task.Finally((_, __) => {}, TaskAffinity.Concurrent);

            await task.StartAndSwallowException();
            CollectionAssert.AreEqual(new string[] { "start", "end" }, runOrder);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ExceptionPropagatesOutIfNoFinally()
        {
            var task = new ActionTask(Token, _ => { throw new InvalidOperationException(); })
                .Catch(_ => { });
            await task.StartAsAsync();
        }

        [Test]
        public async Task AllFinallyHandlersAreCalledOnException()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var task = new FuncTask<string>(TaskManager.Token, () => { throw new InvalidOperationException(); });
            bool exceptionThrown1, exceptionThrown2;
            exceptionThrown1 = exceptionThrown2 = false;

            task.Finally(success => exceptionThrown1 = !success);
            task.Finally((success, _) => exceptionThrown2 = !success);

            StartTrackTime(watch);
            var waitTask = task.Start().Task;
            var ret = await TaskEx.WhenAny(waitTask, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            Assert.AreEqual(ret, waitTask);

            exceptionThrown1.Should().BeTrue();
            exceptionThrown2.Should().BeTrue();
        }

        [Test]
        public async Task StartAsyncWorks()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var task = new FuncTask<int>(Token, _ => 1);

            StartTrackTime(watch);
            var waitTask = task.StartAsAsync();
            var retTask = await TaskEx.WhenAny(waitTask, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            Assert.AreEqual(retTask, waitTask);
            var ret = await waitTask;

            Assert.AreEqual(1, ret);
        }

        [Test]
        public async Task MultipleCatchStatementsCanHappen()
        {
            var runOrder = new List<string>();
            var exceptions = new List<Exception>();
            var task = new ActionTask(Token, _ => { throw new InvalidOperationException(); })
                .Catch(e => { runOrder.Add("1"); exceptions.Add(e); })
                .Then(_ => { throw new InvalidCastException(); })
                .Catch(e => { runOrder.Add("2"); exceptions.Add(e); })
                .Then(_ => { throw new ArgumentNullException(); })
                .Catch(e => { runOrder.Add("3"); exceptions.Add(e); })
                .Finally((b, e) => { }, TaskAffinity.Concurrent);
            await task.StartAndSwallowException();
            CollectionAssert.AreEqual(
                new string[] { typeof(InvalidOperationException).Name, typeof(InvalidOperationException).Name, typeof(InvalidOperationException).Name },
                exceptions.Select(x => x.GetType().Name).ToArray());
            CollectionAssert.AreEqual(
                new string[] { "1", "2", "3" },
                runOrder);
        }

        [Test]
        public async Task ContinueAfterException()
        {
            var runOrder = new List<string>();
            var exceptions = new List<Exception>();
            var task = new ActionTask(Token, _ => { throw new InvalidOperationException(); })
                .Catch(e => { runOrder.Add("1"); exceptions.Add(e); return true; })
                .Then(_ => { throw new InvalidCastException(); })
                .Catch(e => { runOrder.Add("2"); exceptions.Add(e); return true; })
                .Then(_ => { throw new ArgumentNullException(); })
                .Catch(e => { runOrder.Add("3"); exceptions.Add(e); return true; })
                .Finally((s, e) => { }, TaskAffinity.Concurrent);
            await task.StartAndSwallowException();
            CollectionAssert.AreEqual(
                new string[] { typeof(InvalidOperationException).Name, typeof(InvalidCastException).Name, typeof(ArgumentNullException).Name },
                exceptions.Select(x => x.GetType().Name).ToArray());
            CollectionAssert.AreEqual(
                new string[] { "1", "2", "3" },
                runOrder);
        }

        [Test]
        public async Task StartAwaitSafelyAwaits()
        {
            var task = new ActionTask(Token, _ => { throw new InvalidOperationException(); })
                .Catch(_ => { });
            await task.StartAwait(_ => { });
        }

        [Test]
        public async Task TaskOnFailureGetsCalledWhenExceptionHappensUpTheChain()
        {
            var runOrder = new List<string>();
            var exceptions = new List<Exception>();
            var task = new ActionTask(Token, _ => { throw new InvalidOperationException(); })
                .Then(_ => { runOrder.Add("1"); })
                .Catch(ex => exceptions.Add(ex))
                .Then(() => runOrder.Add("OnFailure"), runOptions: TaskRunOptions.OnFailure)
                .Finally((s, e) => { }, TaskAffinity.Concurrent);

            await task.StartAndSwallowException();

            CollectionAssert.AreEqual(
                new string[] { typeof(InvalidOperationException).Name },
                exceptions.Select(x => x.GetType().Name).ToArray());
            CollectionAssert.AreEqual(
                new string[] { "OnFailure" },
                runOrder);
        }
    }

    [TestFixture]
    class TaskToActionTask : BaseTest
    {
        [Test]
        public async Task CanWrapATask()
        {
            var runOrder = new List<string>();
            var task = new Task(() => runOrder.Add($"ran"));
            var act = new ActionTask(task) { Affinity = TaskAffinity.Exclusive };
            await act.Start().Task;
            CollectionAssert.AreEqual(new string[] { $"ran" }, runOrder);
        }

        private async Task<List<int>> GetData(List<int> v)
        {
            await TaskEx.Delay(10);
            v.Add(1);
            return v;
        }

        private async Task<List<int>> GetData2(List<int> v)
        {
            await TaskEx.Delay(10);
            v.Add(3);
            return v;
        }

        [Test]
        public async Task Inlining()
        {
            var runOrder = new List<string>();
            var act = new ActionTask(Token, _ => runOrder.Add($"started"))
                .Then(TaskEx.FromResult(1), TaskAffinity.Exclusive)
                .Then((_, n) => n + 1)
                .Then((_, n) => runOrder.Add(n.ToString()))
                .Then(TaskEx.FromResult(20f), TaskAffinity.Exclusive)
                .Then((_, n) => n + 1)
                .Then((_, n) => runOrder.Add(n.ToString()))
                .Finally((s, _) => runOrder.Add("done"), TaskAffinity.Concurrent)
            ;
            await act.StartAsAsync();
            CollectionAssert.AreEqual(new string[] { "started", "2", "21", "done" }, runOrder);
        }
    }

    [TestFixture]
    class Dependencies : BaseTest
    {
        class TestActionTask : ActionTask
        {
            public TestActionTask(CancellationToken token, Action<bool> action)
                : base(token, action)
            {}

            public TaskBase Test_GetTopMostTask()
            {
                return base.GetTopMostTask();
            }

            public TaskBase Test_GetTopMostTaskInCreatedState()
            {
                return base.GetTopMostTaskInCreatedState();
            }
        }

        [Test]
        public async Task GetTopMostTaskInCreatedState()
        {
            var task1 = new ActionTask(Token, () => { });
            await task1.StartAwait();
            var task2 = new TestActionTask(Token, _ => { });
            var task3 = new TestActionTask(Token, _ => { });

            task1.Then(task2).Then(task3);

            var top = task3.Test_GetTopMostTaskInCreatedState();
            Assert.AreSame(task2, top);
        }

        [Test]
        public void GetTopMostTask()
        {
            var task1 = new ActionTask(TaskEx.FromResult(true));
            var task2 = new TestActionTask(Token, _ => { });
            var task3 = new TestActionTask(Token, _ => { });

            task1.Then(task2).Then(task3);

            var top = task3.Test_GetTopMostTask();
            Assert.AreSame(task1, top);
        }

        [Test]
        public async Task MergingTwoChainsWorks()
        {
            var callOrder = new List<string>();
            var dependsOrder = new List<ITask>();

            var innerChainTask1 = new ActionTask(TaskEx.FromResult(LogAndReturnResult(callOrder, "chain2 completed1", true)));
            var innerChainTask2 = innerChainTask1.Then(_ =>
                {
                    callOrder.Add("chain2 FuncTask<string>");
                    return "1";
                });

            var innerChainTask3 = innerChainTask2
                .Finally((s, e, d) =>
                {
                    callOrder.Add("chain2 Finally");
                    return d;
                }, TaskAffinity.Concurrent);


            var outerChainTask1 = new FuncTask<int>(Token, _ =>
                {
                    callOrder.Add("chain1 FuncTask<int>");
                    return 1;
                });
            var outerChainTask2 = outerChainTask1.Then(innerChainTask3);

            var outerChainTask3 = outerChainTask2
                .Finally((s, e) =>
                {
                    callOrder.Add("chain1 Finally");
                }, TaskAffinity.Concurrent);

            await outerChainTask3.StartAwait();

            var dependsOn = outerChainTask3;
            while (dependsOn != null)
            {
                dependsOrder.Add(dependsOn);
                dependsOn = dependsOn.DependsOn;
            }

            Assert.AreEqual(innerChainTask3, outerChainTask2);
            CollectionAssert.AreEqual(new ITask[] { outerChainTask1, innerChainTask1, innerChainTask2, innerChainTask3, outerChainTask3 }, dependsOrder.Reverse<ITask>().ToArray());

            CollectionAssert.AreEqual(new string[] {
                "chain2 completed1",
                "chain1 FuncTask<int>",
                "chain2 FuncTask<string>",
                "chain2 Finally",
                "chain1 Finally"
            }, callOrder);
        }

        [Test]
        public async Task RunningDifferentTasksDependingOnPreviousResult()
        {
            var callOrder = new List<string>();

            var taskEnd = new ActionTask(Token, () => callOrder.Add("chain completed")) { Name = "Chain Completed" };
            var final = taskEnd.Finally((_, __) => { }, TaskAffinity.Concurrent);

            var taskStart = new FuncTask<bool>(Token, _ =>
                {
                    callOrder.Add("chain start");
                    return false;
                }) { Name = "Chain Start" }
                .Then(new ActionTask<bool>(Token, (_, __) =>
                {
                    callOrder.Add("failing");
                    throw new InvalidOperationException();
                }) { Name = "Failing" });

            taskStart.Then(new ActionTask(Token, () =>
            {
                callOrder.Add("on failure");
            }) { Name = "On Failure" }, runOptions: TaskRunOptions.OnFailure)
            .Then(taskEnd, taskIsTopOfChain: true);

            taskStart.Then(new ActionTask(Token, () =>
            {
                callOrder.Add("on success");
            }) { Name = "On Success" }, runOptions: TaskRunOptions.OnSuccess)
            .Then(taskEnd, taskIsTopOfChain: true);

            await final.StartAndSwallowException();

            Console.WriteLine(String.Join(",", callOrder.ToArray()));
            CollectionAssert.AreEqual(new string[] {
                "chain start",
                "failing",
                "on failure",
                "chain completed"
            }, callOrder);
        }

        private T LogAndReturnResult<T>(List<string> callOrder, string msg, T result)
        {
            callOrder.Add(msg);
            return result;
        }
    }

    static class KeyValuePair
    {
        public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
        {
            return new KeyValuePair<TKey, TValue>(key, value);
        }
    }

    static class AsyncExtensions
    {
        public static Task<T> StartAndSwallowException<T>(this ITask<T> task)
        {
            var tcs = new TaskCompletionSource<T>();
            task.Then((success, result) =>
            {
                tcs.SetResult(result);
            }, TaskAffinity.Concurrent);
            task.Start();
            return tcs.Task;
        }

        public static Task StartAndSwallowException(this ITask task)
        {
            var tcs = new TaskCompletionSource<bool>();
            task.Then(s => { tcs.SetResult(s); });
            task.Start();
            return tcs.Task;
        }
    }
}
