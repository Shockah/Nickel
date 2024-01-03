#if !NET7_0_OR_GREATER
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class RequiredMemberAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
public sealed class CompilerFeatureRequiredAttribute(
	string featureName
) : Attribute
{
	public string FeatureName { get; } = featureName;
}
#endif
