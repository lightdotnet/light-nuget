using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Light.Extensions
{
    public static class ObjectHelper
    {
        /// <summary>
        /// Get Name of Property in an Object
        /// </summary>
        public static string GetPropertyName<T>(Expression<Func<T, object?>> expr)
        {
            if (expr.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }
            else if (expr.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression unaryMemberExpression)
            {
                return unaryMemberExpression.Member.Name;
            }

            throw new ArgumentException($"The provided expression contains a {expr.GetType().Name} which is not supported. Only simple member accessors (fields, properties) of an object are supported.");
        }

        /// <summary>
        /// Get all properties values of an object
        /// </summary>
        public static IDictionary<string, object> GetValues(this object obj)
        {
            var result = new Dictionary<string, object>();

            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
            {
                string name = descriptor.Name;
                object? value = descriptor.GetValue(obj);

                if (value != null)
                {
                    result[name] = value;
                }
            }

            return result;
        }


        /// <summary>
        /// Get all values from const or static props of object
        /// </summary>
        public static IDictionary<string, object> GetStaticValues(this Type typeOfObj)
        {
            // get props in class
            var fields = typeOfObj.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            var result = new Dictionary<string, object>();

            foreach (FieldInfo fi in fields)
            {
                var propName = fi.Name;
                var propValue = fi.GetValue(null);

                if (propValue != null)
                {
                    result[propName] = propValue;
                }
            }

            return result;
        }

        /// <summary>
        /// check object type is a list
        /// </summary>
		public static bool IsList(this object obj)
        {
            return obj != null
                && obj is IList
                && obj.GetType().IsGenericType
                && obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        /// <summary>
        /// check object type is a dictionary
        /// </summary>
        public static bool IsDictionary(this object obj)
        {
            return obj != null
                && obj is IDictionary
                && obj.GetType().IsGenericType
                && obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
        }
    }
}
