using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Light.Extensions
{
    public static class DataTableHelper
    {
        /// <summary>
        /// Convert DataTable to some Type
        /// </summary>
        public static IEnumerable<T> ConvertToType<T>(DataTable dt)
        {
            // Resolve matching properties per column once, instead of per row x column.
            PropertyInfo[] typeProperties = typeof(T).GetProperties();
            var columnProperties = dt.Columns
                .Cast<DataColumn>()
                .Select(column => (Column: column, Properties: typeProperties.Where(p => p.Name == column.ColumnName).ToArray()))
                .Where(x => x.Properties.Length > 0)
                .ToArray();

            foreach (DataRow dr in dt.Rows)
            {
                T obj = Activator.CreateInstance<T>();

                foreach (var (column, properties) in columnProperties)
                {
                    foreach (PropertyInfo pro in properties)
                    {
                        pro.SetValue(obj, dr[column], null);
                    }
                }

                yield return obj;
            }
        }

        /// <summary>
        /// Load list values to DataTable
        /// </summary>
        public static DataTable Load<T>(IList<T> values, params Type[] excludeTypes)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            var table = new DataTable("table", "table");

            foreach (PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);

            foreach (T item in values)
            {
                DataRow row = table.NewRow();

                foreach (PropertyDescriptor prop in properties)
                {
                    if (excludeTypes.Any() && excludeTypes.Any(x => x == prop.PropertyType))
                    {
                        continue;
                    }
                    else
                    {
                        row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                    }
                }

                table.Rows.Add(row);
            }

            return table;
        }

        public static List<Dictionary<string, object>> ConvertToObjects(DataTable dt)
        {
            var objects = new List<Dictionary<string, object>>();

            var intType = "(int)";
            var dateTimeType = "(date)";
            var boolType = "(bool)";

            foreach (DataRow row in dt.Rows)
            {
                var obj = new Dictionary<string, object>();

                foreach (DataColumn column in dt.Columns)
                {
                    var propName = column.ColumnName;
                    var propValue = row[column];

                    if (propName.Contains(intType))
                    {
                        propName = propName.Replace(intType, "");
                        obj[propName] = Convert.ToInt64(propValue);
                    }
                    else if (propName.Contains(dateTimeType))
                    {
                        propName = propName.Replace(dateTimeType, "");
                        obj[propName] = Convert.ToDateTime(propValue);
                    }
                    else if (propName.Contains(boolType))
                    {
                        propName = propName.Replace(boolType, "");
                        obj[propName] = Convert.ToBoolean(propValue);
                    }
                    else
                    {
                        obj[propName] = propValue;
                    }
                }

                objects.Add(obj);
            }

            return objects;
        }
    }
}
