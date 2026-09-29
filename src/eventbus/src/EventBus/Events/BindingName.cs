using System;

namespace Light.EventBus.Events
{
    /// <summary>
    /// Sets the name of the broker entity (exchange/topic) an integration event is published to, and —
    /// through the transport's consumer definition — the default receive endpoint (queue) name of consumers of it.
    /// </summary>
    /// <remarks>
    /// The attribute is <b>not inherited</b>: it applies only to the type it is declared on. Types deriving from an
    /// attributed event do not pick up its binding name, so two different event types never silently share one entity.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class BindingNameAttribute : Attribute
    {
        /// <param name="bindingName">The binding name. Must not be null, empty or whitespace.</param>
        /// <exception cref="ArgumentException"><paramref name="bindingName"/> is null, empty or whitespace.</exception>
        public BindingNameAttribute(string bindingName)
        {
            if (string.IsNullOrWhiteSpace(bindingName))
            {
                throw new ArgumentException("Binding name must not be null, empty or whitespace.", nameof(bindingName));
            }

            BindingName = bindingName;
        }

        public string BindingName { get; }
    }
}
