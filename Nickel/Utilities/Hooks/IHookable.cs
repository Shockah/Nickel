using System;
using System.Reflection;

namespace Nickel;

public interface IHookable
{
	void RegisterMethodHook<THookDelegate>(MethodInfo method, THookDelegate @delegate, double priority)
		where THookDelegate : Delegate;

	void UnregisterMethodHook<THookDelegate>(MethodInfo method, THookDelegate @delegate)
		where THookDelegate : Delegate;
}
