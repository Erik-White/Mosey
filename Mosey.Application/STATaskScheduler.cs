﻿//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: StaTaskScheduler.cs
//
//--------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace System.Threading.Tasks.Schedulers
{
    /// <summary>Provides a scheduler that uses STA threads.</summary>
    public sealed class StaTaskScheduler : TaskScheduler, IDisposable
    {
        /// <summary>Stores the queued tasks to be executed by our pool of STA threads.</summary>
        private readonly BlockingCollection<Task> _tasks;
        /// <summary>The STA threads used by the scheduler.</summary>
        private readonly List<Thread> _threads;

        /// <summary>Initializes a new instance of the StaTaskScheduler class with the specified concurrency level.</summary>
        /// <param name="concurrencyLevel">The number of threads that should be created and used by this scheduler.</param>
        /// <param name="disableComObjectEagerCleanup">Whether automatic cleanup of runtime callable wrappers (RCW) should be disabled.</param>
        public StaTaskScheduler(int concurrencyLevel, bool disableComObjectEagerCleanup = false)
        {
            // Validate arguments
            if (concurrencyLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrencyLevel));
            }

            // Initialize the tasks collection
            _tasks = new BlockingCollection<Task>();

            // Create the threads to be used by this scheduler
            _threads = Enumerable.Range(0, concurrencyLevel).Select(i =>
            {
                var thread = new Thread(() =>
                {
                    // Continually get the next task and try to execute it.
                    // This will continue until the scheduler is disposed and no more tasks remain.
                    foreach (var t in _tasks.GetConsumingEnumerable())
                    {
                        TryExecuteTask(t);
                    }
                });
                if (disableComObjectEagerCleanup)
                {
                    thread.DisableComObjectEagerCleanup();
                }

                thread.IsBackground = true;
                thread.TrySetApartmentState(ApartmentState.STA);
                return thread;
            }).ToList();

            // Start all of the threads
            _threads.ForEach(t => t.Start());
        }

        /// <summary>Queues a Task to be executed by this scheduler.</summary>
        /// <param name="task">The task to be executed.</param>
        protected override void QueueTask(Task task) =>
            // Push it into the blocking collection of tasks
            _tasks.Add(task);

        /// <summary>Provides a list of the scheduled tasks for the debugger to consume.</summary>
        /// <returns>An enumerable of all tasks currently scheduled.</returns>
        protected override IEnumerable<Task> GetScheduledTasks() =>
            // Serialize the contents of the blocking collection of tasks for the debugger
            _tasks.ToArray();

        /// <summary>Determines whether a Task may be inlined.</summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">Whether the task was previously queued.</param>
        /// <returns>true if the task was successfully inlined; otherwise, false.</returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // Try to inline if the current thread is STA
            return
                Thread.CurrentThread.GetApartmentState() == ApartmentState.STA &&
                TryExecuteTask(task);
        }

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
        public override int MaximumConcurrencyLevel => _threads.Count;

        /// <summary>
        /// Cleans up the scheduler by indicating that no more tasks will be queued.
        /// This method blocks until all threads successfully shutdown.
        /// </summary>
        public void Dispose()
        {
            if (_tasks is not null)
            {
                // Indicate that no new tasks will be coming in
                _tasks.CompleteAdding();

                // Wait for all threads to finish processing tasks
                foreach (var thread in _threads)
                {
                    thread.Join();
                }

                // Cleanup
                _tasks.Dispose();
            }
        }
    }
}