using System;
using System.Reflection;

namespace Nickel;

/// <summary>
/// A type that was automatically generated at runtime, allowing hooking any of its methods.
/// </summary>
public interface IHookable
{
	/// <summary>
	/// Register a hook for one of the object's methods.
	/// </summary>
	/// <param name="method">The method to hook.</param>
	/// <param name="delegate">The delegate to hook the method with.</param>
	/// <param name="priority">The priority to call the delegate hook in. Higher priorities get called first.</param>
	/// <typeparam name="THookDelegate">The type of the delegate to hook the method with.</typeparam>
	void RegisterMethodHook<THookDelegate>(MethodInfo method, THookDelegate @delegate, double priority = 0)
		where THookDelegate : Delegate;

	/// <summary>
	/// Unregister a hook for one of the object's methods.
	/// </summary>
	/// <param name="method">The method to unhook.</param>
	/// <param name="delegate">The delegate to unhook from the method.</param>
	/// <typeparam name="THookDelegate">The type of the delegate to unhook from the method.</typeparam>
	void UnregisterMethodHook<THookDelegate>(MethodInfo method, THookDelegate @delegate)
		where THookDelegate : Delegate;
}
