using System;

namespace Light.EventBus.Events
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class BindingNameAttribute : Attribute
    {
        public BindingNameAttribute(string bindingName)
        {
            BindingName = bindingName;
        }

        public string BindingName { get; }
    }
}
