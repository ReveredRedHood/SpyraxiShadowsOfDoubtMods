using System;
using System.Collections.Generic;

namespace PresetEdit;
// Custom comparer for the Product class
public class PresetComparer : IEqualityComparer<object> {
    // Products are equal if their names and product numbers are equal.
    public new bool Equals(object x, object y) {

        //Check whether the compared objects reference the same data.
        if (Object.ReferenceEquals(x, y)) return true;

        //Check whether any of the compared objects is null.
        if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
            return false;

        // Get the preset names
        var xName = Helpers.GetPresetKey(x);
        var yName = Helpers.GetPresetKey(y);
        if (xName == null || xName == default || yName == null || yName == default) {
            throw new System.ArgumentException("Comparison must be between two SoCustomComparison objects.");
        }

        //Check whether the products' properties are equal.
        Plugin.Log.LogInfo($"Are these the same? {xName} and {yName}");
        return xName == yName;
    }

    // If Equals() returns true for a pair of objects
    // then GetHashCode() must return the same value for these objects.
    public int GetHashCode(object preset) {
        // Check whether the object is null
        if (Object.ReferenceEquals(preset, null)) return 0;

        // Get hash code for the Name field if it is not null.
        var name = Helpers.GetPresetKey(preset);
        int hashProductName = name == null ? 0 : name.GetHashCode();

        return hashProductName;
    }
}