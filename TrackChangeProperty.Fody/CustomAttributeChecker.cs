using System;
using System.Linq;
using Mono.Cecil;

public static class CustomAttributeChecker
{
    public static CustomAttribute GetTrackAttribute(this ICustomAttributeProvider definition)
    {
        
        var customAttributes = definition.CustomAttributes;
        var fullname = "TrackChangePropertyLib.TrackingAttribute";
     
        return customAttributes.FirstOrDefault(x => x.AttributeType.FullName == fullname);
    }

    public static bool ContainsTrackAttribute(this ICustomAttributeProvider definition)
    {
        return GetTrackAttribute(definition) != null;
    }

    public static bool IsCompilerGenerated(this ICustomAttributeProvider definition)
    {
        var customAttributes = definition.CustomAttributes;

        return customAttributes.Any(x => x.AttributeType.Name == "CompilerGeneratedAttribute");
    }

    public static void RemoveTrackAttribute(this ICustomAttributeProvider definition)
    {
        var customAttributes = definition.CustomAttributes;
        var fullname =  "TrackChangePropertyLib.TrackingAttribute";
        var timeAttribute = customAttributes.FirstOrDefault(x => x.AttributeType.FullName == fullname);

        if (timeAttribute != null)
        {
            customAttributes.Remove(timeAttribute);
        }

    }
}