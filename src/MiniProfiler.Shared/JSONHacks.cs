// Defining these attributes here so we don't have a ton of #if defs around every property

#if !NET6_0_OR_GREATER
namespace System.Text.Json.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    internal sealed class JsonIgnore : Attribute { }
}
#endif
