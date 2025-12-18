using System;

namespace Light.EventBus.Events
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class BindingNameAttribute : Attribute
    {
        public BindingNameAttribute(string bindingName)
        {
            BindingNameValue = bindingName;
        }

        public virtual string BindingName => BindingNameValue;

        protected string BindingNameValue { get; }
    }
}
