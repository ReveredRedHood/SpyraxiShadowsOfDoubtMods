using System;
using System.Collections.Generic;
using System.Linq;

namespace PresetEdit
{
    public static class GameVersionCompatibility
    {
        public static List<(
            Type presetType,
            string propertyName,
            Type propertyType
        )> GetIncompatibleEntries(
            List<(Type presetType, string propertyName, Type propertyType)> values
        )
        {
            var result = new List<(Type presetType, string propertyName, Type propertyType)>();
            foreach (var value in values)
            {
                var (presetType, propertyName, propertyType) = value;
                var props = presetType.GetProperties().Where(prop => prop.GetSetMethod() != null);
                if (
                    !props.Any(
                        prop => prop.Name == propertyName && prop.PropertyType == propertyType
                    )
                )
                {
                    result.Add(value);
                }
            }
            return values;
        }
    }
}
