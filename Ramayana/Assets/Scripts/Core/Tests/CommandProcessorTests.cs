// EditMode tests for CommandProcessor: validation, queuing during execution, late validation, and failure recovery.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JourneyToLanka.Core.Commands;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JourneyToLanka.Core.Tests
{
    public sealed class CommandProcessorTests
    {
        private sealed class ProbeCommand : ICommand
        {
            private readonly List<string> _log;
            private readonly string _name;

            public ProbeCommand(List<string> log, string name)
            {
                _log = log;
                _name = name;
            }

            public bool Allowed { get; set; } = true;

            public bool ThrowOnExecute { get; set; }

            public Action OnExecute { get; set; }

            public bool CanExecute()
            {
                return Allowed;
            }

            public void Execute()
            {
                _log.Add("exec:" + _name);
                OnExecute?.Invoke();
                if (ThrowOnExecute)
                {
                    throw new InvalidOperationException("probe failure");
                }
            }
        }

        private List<string> _log;
        private CommandProcessor _processor;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _processor = new CommandProcessor();
        }

        [Test]
        public void Submit_ValidCommand_ExecutesAndRaisesExecuted()
        {
            var a = new ProbeCommand(_log, "A");
            ICommand executed = null;
            _processor.Executed += command => executed = command;

            CommandSubmitResult result = _processor.Submit(a);

            Assert.AreEqual(CommandSubmitResult.Executed, result);
            CollectionAssert.AreEqual(new[] { "exec:A" }, _log);
            Assert.AreSame(a, executed);
        }

        [Test]
        public void Submit_InvalidCommand_IsRejectedWithoutExecuting()
        {
            var a = new ProbeCommand(_log, "A") { Allowed = false };
            ICommand rejected = null;
            _processor.Rejected += command => rejected = command;

            CommandSubmitResult result = _processor.Submit(a);

            Assert.AreEqual(CommandSubmitResult.Rejected, result);
            CollectionAssert.IsEmpty(_log);
            Assert.AreSame(a, rejected);
        }

        [Test]
        public void Submit_DuringExecution_QueuesAndRunsAfterCurrent()
        {
            var b = new ProbeCommand(_log, "B");
            var a = new ProbeCommand(_log, "A");
            CommandSubmitResult nested = CommandSubmitResult.Failed;
            a.OnExecute = () =>
            {
                nested = _processor.Submit(b);
                _log.Add("after-submit");
            };

            CommandSubmitResult result = _processor.Submit(a);

            Assert.AreEqual(CommandSubmitResult.Executed, result);
            Assert.AreEqual(CommandSubmitResult.Queued, nested);
            CollectionAssert.AreEqual(new[] { "exec:A", "after-submit", "exec:B" }, _log);
        }

        [Test]
        public void QueuedCommand_IsValidatedWhenItRuns()
        {
            var b = new ProbeCommand(_log, "B");
            var a = new ProbeCommand(_log, "A");
            a.OnExecute = () =>
            {
                _processor.Submit(b);
                b.Allowed = false;
            };
            ICommand rejected = null;
            _processor.Rejected += command => rejected = command;

            _processor.Submit(a);

            CollectionAssert.AreEqual(new[] { "exec:A" }, _log);
            Assert.AreSame(b, rejected);
        }

        [Test]
        public void ExceptionDuringExecute_ClearsQueue_AndProcessorRecovers()
        {
            var b = new ProbeCommand(_log, "B");
            var a = new ProbeCommand(_log, "A") { ThrowOnExecute = true };
            a.OnExecute = () => _processor.Submit(b);
            LogAssert.Expect(LogType.Exception, new Regex("probe failure"));

            CommandSubmitResult result = _processor.Submit(a);

            Assert.AreEqual(CommandSubmitResult.Failed, result);
            CollectionAssert.AreEqual(new[] { "exec:A" }, _log);
            Assert.AreEqual(0, _processor.PendingCount);
            Assert.IsFalse(_processor.IsProcessing);
            Assert.AreEqual(CommandSubmitResult.Executed, _processor.Submit(new ProbeCommand(_log, "C")));
        }

        [Test]
        public void Submit_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _processor.Submit(null));
        }
    }
}
