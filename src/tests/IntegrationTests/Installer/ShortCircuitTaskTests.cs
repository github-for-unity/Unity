using System.Threading;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;

namespace IntegrationTests
{
    [TestFixture]
    class ShortCircuitTaskTests : BaseTaskManagerTest
    {
        [Test]
        public void ShouldSkipSecondTask()
        {
            InitializeTaskManager();
            Logger.Trace("ShouldSkipSecondTask");

            var calledFirst = false;
            var first = new FuncTask<string>(CancellationToken.None, () => {
                Logger.Trace("Returning First");
                calledFirst = true;
                return "First";
            });

            var calledSecond = false;
            var second = new FuncTask<string>(CancellationToken.None, () => {
                Logger.Trace("Returning Second");
                calledSecond = true;
                return "Second";
            });

            var shortCircuitTask = new ShortCircuitTask<string>(CancellationToken.None, second);

            var result = first
                .Then(shortCircuitTask).Start().Result;

            result.Should().Be("First");
            calledFirst.Should().BeTrue();
            calledSecond.Should().BeFalse();
        }

        [Test]
        public void ShouldRunSecondTask()
        {
            InitializeTaskManager();
            Logger.Trace("ShouldRunSecondTask");

            var calledFirst = false;
            var first = new FuncTask<string>(CancellationToken.None, () => {
                Logger.Trace("Returning First");
                calledFirst = true;
                return null;
            });

            var calledSecond = false;
            var second = new FuncTask<string>(CancellationToken.None, () => {
                Logger.Trace("Returning Second");
                calledSecond = true;
                return "Second";
            });

            var shortCircuitTask = new ShortCircuitTask<string>(CancellationToken.None, second);

            var result = first
                .Then(shortCircuitTask).Start().Result;

            result.Should().Be("Second");
            calledFirst.Should().BeTrue();
            calledSecond.Should().BeTrue();
        }
    }
}