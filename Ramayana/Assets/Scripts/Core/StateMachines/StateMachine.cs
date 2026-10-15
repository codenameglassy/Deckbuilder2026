// Runs one IState at a time for a context object. Plain C# so any owner can hold one and it is testable in EditMode.
// Transitions requested from inside Enter, Exit, Tick or a StateChanged handler are deferred until that call returns,
// so a state never keeps running code after it has been exited. If several are requested, the last one wins.
using System;

namespace JourneyToLanka.Core.StateMachines
{
    public sealed class StateMachine<TContext>
    {
        private const int MaxChainedTransitions = 32;

        private readonly TContext _context;
        private IState<TContext> _current;
        private ITickableState<TContext> _currentTickable;
        private IState<TContext> _pending;
        private bool _isBusy;

        public StateMachine(TContext context)
        {
            _context = context;
        }

        public event Action<IState<TContext>, IState<TContext>> StateChanged;

        public IState<TContext> CurrentState => _current;

        public void ChangeState(IState<TContext> next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _pending = next;

            if (!_isBusy)
            {
                ApplyPending();
            }
        }

        public void Tick(float deltaTime)
        {
            if (_currentTickable == null || _isBusy)
            {
                return;
            }

            _isBusy = true;
            try
            {
                _currentTickable.Tick(_context, deltaTime);
            }
            catch
            {
                _pending = null;
                throw;
            }
            finally
            {
                _isBusy = false;
            }

            ApplyPending();
        }

        public void Stop()
        {
            if (_isBusy)
            {
                throw new InvalidOperationException(
                    "StateMachine.Stop cannot be called from inside a state or a StateChanged handler.");
            }

            IState<TContext> previous = _current;
            _pending = null;

            if (previous == null)
            {
                return;
            }

            _current = null;
            _currentTickable = null;

            _isBusy = true;
            try
            {
                previous.Exit(_context);
                StateChanged?.Invoke(previous, null);
            }
            finally
            {
                _pending = null;
                _isBusy = false;
            }
        }

        private void ApplyPending()
        {
            if (_pending == null)
            {
                return;
            }

            _isBusy = true;
            try
            {
                int transitions = 0;
                while (_pending != null)
                {
                    transitions++;
                    if (transitions > MaxChainedTransitions)
                    {
                        throw new InvalidOperationException(
                            $"StateMachine<{typeof(TContext).Name}> exceeded {MaxChainedTransitions} chained transitions. " +
                            "Two states are probably handing control back and forth from Enter.");
                    }

                    IState<TContext> previous = _current;
                    IState<TContext> next = _pending;
                    _pending = null;

                    previous?.Exit(_context);

                    _current = next;
                    _currentTickable = next as ITickableState<TContext>;

                    next.Enter(_context);
                    StateChanged?.Invoke(previous, next);
                }
            }
            finally
            {
                _pending = null;
                _isBusy = false;
            }
        }
    }
}
