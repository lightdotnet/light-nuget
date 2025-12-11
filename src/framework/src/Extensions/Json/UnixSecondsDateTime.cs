using System;
using System.Text.Json.Serialization;

namespace Light.Extensions.Json
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class UnixSecondsDateTimeAttribute : JsonConverterAttribute
    {
        public UnixSecondsDateTimeAttribute() : base(typeof(UnixSecondsToDateTimeConverter))
        { }
    }
}
