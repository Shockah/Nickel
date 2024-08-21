using System;

namespace Nanoray.ServiceLocator;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class InjectableAttribute : Attribute;
