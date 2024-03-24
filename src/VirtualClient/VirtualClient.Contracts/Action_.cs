namespace VirtualClient.Contracts
{
    using System;
    using System.Reflection;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Encapsulates a method that has no parameters and does not return a value
    /// and that has an identifier associated.
    /// </summary>
    public class Action_
    {
        private Action action;

        /// <summary>
        /// Initializes the <see cref="Action_"/> instance.
        /// </summary>
        public Action_(Action task)
            : this(Guid.NewGuid().ToString(), task)
        {
        }

        /// <summary>
        /// Initializes the <see cref="Action_"/> instance.
        /// </summary>
        public Action_(string id, Action task)
        {
            id.ThrowIfNullOrWhiteSpace(nameof(id));
            task.ThrowIfNull(nameof(task));

            this.Id = id;
            this.action = task;
        }

        /// <summary>
        /// An identifier for the anonymous task/operation.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the method represented by the delegate.
        /// </summary>
        public MethodInfo Method
        {
            get
            {
                return this.action.Method;
            }
        }

        /// <summary>
        /// Gets the class instance on which the current delegate invokes the instance method.
        /// </summary>
        public object Target
        {
            get
            {
                return this.action.Target;
            }
        }

        /// <summary>
        /// Invokes the anonymous task/operation logic.
        /// </summary>
        public void Invoke()
        {
            this.action.Invoke();
        }
    }
}
