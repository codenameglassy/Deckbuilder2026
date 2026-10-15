// One pipeline for player input and AI decisions. Commands submitted while another is running are queued and run
// in submission order after it, which is how effects that trigger further actions resolve.
// Each owner (one combat, for example) creates its own processor; it is not a service.
// Subscribers to Executed can record commands for replay. An exception in a command is logged, the remaining
// queue is dropped because it was likely built on the failed command's results, and the processor stays usable.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JourneyToLanka.Core.Commands
{
    public sealed class CommandProcessor
    {
        private readonly Queue<ICommand> _queue = new Queue<ICommand>();
        private bool _isProcessing;

        public event Action<ICommand> Executed;

        public event Action<ICommand> Rejected;

        public bool IsProcessing => _isProcessing;

        public int PendingCount => _queue.Count;

        public CommandSubmitResult Submit(ICommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (_isProcessing)
            {
                _queue.Enqueue(command);
                return CommandSubmitResult.Queued;
            }

            CommandSubmitResult result = CommandSubmitResult.Failed;
            _isProcessing = true;
            try
            {
                result = Run(command) ? CommandSubmitResult.Executed : CommandSubmitResult.Rejected;

                while (_queue.Count > 0)
                {
                    Run(_queue.Dequeue());
                }
            }
            catch (Exception exception)
            {
                _queue.Clear();
                Debug.LogException(exception);
            }
            finally
            {
                _isProcessing = false;
            }

            return result;
        }

        public void ClearPending()
        {
            _queue.Clear();
        }

        private bool Run(ICommand command)
        {
            if (!command.CanExecute())
            {
                Rejected?.Invoke(command);
                return false;
            }

            command.Execute();
            Executed?.Invoke(command);
            return true;
        }
    }
}
