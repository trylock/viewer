using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.Core
{
    /// <summary>
    /// Retry builds a task which can retry an operation. You can specify how many times it will attempt
    /// to run the operation, delay between each attempt and which exceptions trigger another attempt.
    /// </summary>
    /// <remarks>
    /// See <see cref="Retry"/> for easy to use factories to build a retry task from syncrhonnous
    /// functions and methods as well as a factory function to create a retry task from an
    /// asyncrhonnous operation.
    /// </remarks>
    /// <example>
    /// This example tries to load content of a file 4 times with 2s between each attempt. It will
    /// try to load the file again if it throws IOException which indicates that another process has
    /// the file locked.
    /// <code>
    /// var result = await Retry
    ///     .Async(() => File.ReadAllBytes())
    ///     .WithAttempts(4)
    ///     .WithDelay(TimeSpan.FromSeconds(2))
    ///     .WhenExactly&lt;IOException&gt;()
    /// </code>
    /// Notice that the Retry object is awaitable. You can also get its task directly using the
    /// <see cref="Task"/> property. It will not begin the operation unless you get the task or
    /// try to await the Retry object.
    ///
    /// > [!NOTE]
    /// > The Retry object is immutable. Each method returns a new object and with it a new task.
    /// </example>
    /// <typeparam name="TResult">Type of the value returned from <see cref="Task"/></typeparam>
    public class Retry<TResult>
    {
        private readonly Func<Task<TResult>> _operation;
        private readonly TimeSpan _delay;
        private readonly int _maxAttemptCount;
        private readonly Predicate<Exception> _exceptionPredicate;
        private readonly CancellationToken _token;
        private Task<TResult> _task;

        private Retry(
            Func<Task<TResult>> operation, 
            int maxAttemptCount, 
            TimeSpan delay,
            CancellationToken token,
            Predicate<Exception> predicate)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
            _maxAttemptCount = maxAttemptCount;
            _delay = delay;
            _token = token;
            _exceptionPredicate = predicate;
        }

        /// <summary>
        /// Set a new retry delay. This is the minimal time between 2 attempts.
        /// </summary>
        /// <param name="delay">New minimal time between 2 attemts</param>
        /// <returns>New modified retry object</returns>
        public Retry<TResult> WithDelay(TimeSpan delay)
        {
            return new Retry<TResult>(_operation, _maxAttemptCount, delay, _token, _exceptionPredicate);
        }

        /// <summary>
        /// Set a new maximal number of attempts.
        /// </summary>
        /// <param name="count">Maximal number of attemts</param>
        /// <returns>New modified retry object</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is less than 0
        /// </exception>
        public Retry<TResult> WithAttempts(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            return new Retry<TResult>(_operation, count, _delay, _token, _exceptionPredicate);
        }

        /// <summary>
        /// Set a new cancellation token of the task.
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <returns></returns>
        public Retry<TResult> WithCancellationToken(CancellationToken token)
        {
            return new Retry<TResult>(_operation, _maxAttemptCount, _delay, _token, _exceptionPredicate);
        }

        /// <summary>
        /// Set a new condition. If the operation throws an exception of type
        /// <typeparamref name="TException"/> exactly (i.e., GetType() of the exception returns
        /// <c>typeof(TException)</c>)
        /// </summary>
        /// <typeparam name="TException">
        /// Type of the expected exception (i.e., exception which will trigget another attempt)
        /// </typeparam>
        /// <returns>New modified retry object</returns>
        public Retry<TResult> WhenExactly<TException>() where TException : Exception
        {
            return new Retry<TResult>(
                _operation, 
                _maxAttemptCount,
                _delay, 
                _token,
                exception => exception.GetType() == typeof(TException));
        }

        /// <summary>
        /// Set a new condition. This is almost the same as <see cref="WhenExactly{TException}()"/>.
        /// Additionally, you can specify a general predicate (it can check exception message
        /// for example)
        /// </summary>
        /// <typeparam name="TException">Type of the exception</typeparam>
        /// <param name="predicate">Predicate which checks the exception</param>
        /// <returns>New modified retry object</returns>
        public Retry<TResult> WhenExactly<TException>(Predicate<TException> predicate)
            where TException : Exception
        {
            return new Retry<TResult>(
                _operation,
                _maxAttemptCount,
                _delay, 
                _token,
                exception => exception.GetType() == typeof(TException) &&
                             predicate((TException) exception));
        }

        /// <summary>
        /// Set a new condition. If the operation throws exception assignable to a variable of type
        /// <typeparamref name="TException"/> (i.e., <c>exception is TException</c>), another
        /// attempt will be made. 
        /// </summary>
        /// <typeparam name="TException">Type of the exception</typeparam>
        /// <returns>New modified retry object</returns>
        public Retry<TResult> When<TException>() where TException : Exception
        {
            return new Retry<TResult>(
                _operation,
                _maxAttemptCount,
                _delay,
                _token,
                exception => exception is TException);
        }

        /// <summary>
        /// Create a new retry object with default settings.
        /// </summary>
        /// <param name="operation">Factory which creates a new task.</param>
        /// <returns>New retry object</returns>
        public static Retry<TResult> Async(Func<Task<TResult>> operation)
        {
            return new Retry<TResult>(
                operation, 
                maxAttemptCount: 2, 
                delay: TimeSpan.FromSeconds(1), 
                token: CancellationToken.None,
                predicate: _ => true);
        }

        /// <summary>
        /// Start the task if it hasn't been started yet. Otherwise, return the running task.
        /// </summary>
        public Task<TResult> Task => _task ?? (_task = Run());

        private async Task<TResult> Run()
        {
            for (var i = 0; i < _maxAttemptCount - 1; ++i)
            {
                _token.ThrowIfCancellationRequested();

                try
                {
                    var task = _operation();
                    return await task;
                }
                catch (Exception e) when (_exceptionPredicate(e))
                {
                    await System.Threading.Tasks.Task.Delay(_delay);
                }
            }

            // the last attempt
            return await _operation();
        }
        
        public TaskAwaiter<TResult> GetAwaiter() => Task.GetAwaiter();
    }

    /// <summary>
    /// Static factories for <see cref="Retry{TResult}"/> which automatically infer template types.
    /// </summary>
    public static class Retry
    {
        /// <summary>
        /// Create a new retry task from a synchronnous operation which does not have a return type.
        /// </summary>
        /// <param name="operation">Operation to retry</param>
        /// <returns>Retry task</returns>
        public static Retry<bool> Async(Action operation)
        {
            return Retry<bool>.Async(() =>
            {
                operation();
                return Task.FromResult(true);
            });
        }

        /// <summary>
        /// Create a new retry task from a synchronnous operation with a return value.
        /// </summary>
        /// <typeparam name="T">Type of the return value.</typeparam>
        /// <param name="operation">Operation which computes the value.</param>
        /// <returns>Retry task of the operation</returns>
        public static Retry<T> Async<T>(Func<T> operation)
        {
            return Retry<T>.Async(() =>
            {
                var result = operation();
                return Task.FromResult(result);
            });
        }

        /// <summary>
        /// Create a new retry task from an asynchronnous operation with a return value.
        /// </summary>
        /// <typeparam name="T">Type of the return value</typeparam>
        /// <param name="operation">Factory function which creates the task.</param>
        /// <returns>Restry task</returns>
        public static Retry<T> Async<T>(Func<Task<T>> operation)
        {
            return Retry<T>.Async(operation);
        }
    }
}
