    #region JsonHelper
    public static class JsonHelper
    {
        public static object GetPropertyValue(JObject obj, string property, Type type)
        {
            var propertyExists = obj.TryGetValue(property, out var value);
            var resultValue = propertyExists ? value.ToObject(type) : null;

            return resultValue;
        }

        public static JObject Deserialize(string jsonString) => JObject.Parse(jsonString);
        public static string Serialize(object obj) => JsonConvert.SerializeObject(obj);
    }
    #endregion

    #region Mapper
    [AttributeUsage(AttributeTargets.Property)]
    public class NotMappedAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Property)]
    public class MappedFromJsonField : Attribute
    {
        public string JsonField { get; set; }
        public MappedFromJsonField(string jsonField)
        {
            JsonField = jsonField;
        }
    }
    public static class DataTableExtensions
    {
        public static IList<T> ToList<T>(this DataTable table) where T : new()
        {
            IList<T> result = new List<T>();

            foreach (DataRow row in table.Rows)
            {
                var item = CreateItemFromRow<T>(row, typeof(T).GetProperties());
                result.Add(item);
            }

            return result;
        }

        public static IList<T> ToList<T>(this DataTable table, Dictionary<string, string> mappings) where T : new()
        {
            IList<T> result = new List<T>();

            foreach (DataRow row in table.Rows)
            {
                var item = CreateItemFromRow<T>(row, typeof(T).GetProperties(), mappings);
                result.Add(item);
            }

            return result;
        }

        private static T CreateItemFromRow<T>(DataRow row, IEnumerable<PropertyInfo> properties) where T : new()
        {
            var item = new T();
            foreach (var property in properties.Where(p => !Attribute.IsDefined(p, typeof(NotMappedAttribute))))
            {
                if (property.GetCustomAttributes(true)
                    .Any(p => Attribute.IsDefined(property, typeof(MappedFromJsonField))))
                {
                    var jsonField = property.GetCustomAttribute<MappedFromJsonField>().JsonField;

                    if (row[jsonField] != Convert.DBNull)
                    {
                        var jsonObject = JsonHelper.Deserialize((string)row[jsonField]);
                        var value = JsonHelper.GetPropertyValue(jsonObject, property.Name, property.PropertyType);

                        property.SetValue(item, value, null);
                    }
                    else
                    {
                        property.SetValue(item, null, null);
                    }
                }
                else
                {
                    var value = property.PropertyType.IsEnum ?
                        EnumElement.ToEnum(property.PropertyType, row[property.Name]) :
                        row[property.Name] == Convert.DBNull ? null : row[property.Name];

                    property.SetValue(item, value, null);
                }
            }
            return item;
        }

        private static T CreateItemFromRow<T>(DataRow row, IEnumerable<PropertyInfo> properties, IReadOnlyDictionary<string, string> mappings) where T : new()
        {
            var item = new T();
            foreach (var property in properties.Where(p => !Attribute.IsDefined(p, typeof(NotMappedAttribute))))
            {
                if (!mappings.ContainsKey(property.Name)) continue;
                if (property.GetCustomAttributes(true)
                    .Any(p => Attribute.IsDefined(property, typeof(MappedFromJsonField))))
                {
                    var jsonField = property.GetCustomAttribute<MappedFromJsonField>().JsonField;

                    if (row[jsonField] != Convert.DBNull)
                    {
                        var jsonObject = JsonHelper.Deserialize((string)row[jsonField]);
                        var value = JsonHelper.GetPropertyValue(jsonObject, property.Name, property.PropertyType);

                        property.SetValue(item, value, null);
                    }
                    else
                    {
                        property.SetValue(item, null, null);
                    }
                }
                else
                {
                    var value = property.PropertyType.IsEnum ?
                        EnumElement.ToEnum(property.PropertyType, row[property.Name]) :
                        row[property.Name] == Convert.DBNull ? null : row[property.Name];

                    property.SetValue(item, value, null);
                }
            }
            return item;
        }
    }
    #endregion
