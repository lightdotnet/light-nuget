using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Light.Extensions
{
    public static class AttributeExtensions
    {
        /// <summary>
        /// Get first value from attribute
        /// </summary>
        public static T? GetAttribute<T>(this MemberInfo memberInfo)
            where T : Attribute =>
            memberInfo.GetCustomAttributes(typeof(T), true).FirstOrDefault() as T;

        /// <summary>
        /// Get [DisplayName] value attribute
        /// </summary>
        public static string? GetDisplayName(this MemberInfo memberInfo) =>
            memberInfo.GetAttribute<DisplayNameAttribute>()?.DisplayName;

        /// <summary>
        /// Get [Description] value attribute
        /// </summary>
        public static string? GetDescription(this MemberInfo memberInfo) =>
            memberInfo.GetAttribute<DescriptionAttribute>()?.Description;

        /// <summary>
        /// Get value of Name from [Display(Name={value})] attribute
        /// </summary>
        public static string? GetNameOfDisplay(this MemberInfo memberInfo) =>
            memberInfo.GetAttribute<DisplayAttribute>()?.Name;

        /// <summary>
        /// Get value of Description from [Display(Name={Description})] attribute
        /// </summary>
        public static string? GetDescriptionOfDisplay(this MemberInfo memberInfo) =>
            memberInfo.GetAttribute<DisplayAttribute>()?.Description;
    }
}
