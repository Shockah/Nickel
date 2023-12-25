// Copyright (c) 2008 Daniel Grunwald
// 
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Nickel;

/// <summary>
/// A class for managing a weak event.
/// </summary>
public sealed class WeakEvent<TEventArgs>
{
    private readonly struct EventEntry
    {
        public readonly FastSmartWeakEventForwarderProvider.ForwarderDelegate Forwarder;
        public readonly MethodInfo TargetMethod;
        public readonly WeakReference? TargetReference;

        public EventEntry(FastSmartWeakEventForwarderProvider.ForwarderDelegate forwarder, MethodInfo targetMethod, WeakReference? targetReference)
        {
            this.Forwarder = forwarder;
            this.TargetMethod = targetMethod;
            this.TargetReference = targetReference;
        }
    }

    private static class FastSmartWeakEventForwarderProvider
    {
        private static readonly MethodInfo? GetTargetMethod = typeof(WeakReference).GetMethod("get_Target");
        private static readonly Type[] ForwarderParameters = { typeof(WeakReference), typeof(object), typeof(TEventArgs) };
        private static readonly Dictionary<MethodInfo, ForwarderDelegate> Forwarders = new();

        internal delegate bool ForwarderDelegate(WeakReference? wr, object? sender, TEventArgs e);

        internal static ForwarderDelegate GetForwarder(MethodInfo method)
        {
            lock (Forwarders)
            {
                if (Forwarders.TryGetValue(method, out var d))
                    return d;
            }

            if (method.DeclaringType is not { } declaringType)
                throw new ArgumentException("Event handler method has an unknown declaring type.");
            if (declaringType.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length != 0)
                throw new ArgumentException("Cannot create weak event to anonymous method with closure.");
            var parameters = method.GetParameters();

            Debug.Assert(GetTargetMethod is not null);

            DynamicMethod dm = new("FastSmartWeakEvent", typeof(bool), ForwarderParameters, method.DeclaringType, skipVisibility: true);
            ILGenerator il = dm.GetILGenerator();

            if (!method.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.EmitCall(OpCodes.Callvirt, GetTargetMethod, null);
                il.Emit(OpCodes.Dup);
                Label label = il.DefineLabel();
                il.Emit(OpCodes.Brtrue, label);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ret);
                il.MarkLabel(label);
                // The castclass here is required for the generated code to be verifiable.
                // We can leave it out because we know this cast will always succeed
                // (the instance/method pair was taken from a delegate).
                // Unverifiable code is fine because private reflection is only allowed under FullTrust
                // anyways.
                //il.Emit(OpCodes.Castclass, method.DeclaringType);
            }
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            // This castclass here is required to prevent creating a hole in the .NET type system.
            // You can remove this cast if you trust add FastSmartWeakEvent.Raise callers to do
            // the right thing, but the small performance increase (about 5%) usually isn't worth the risk.
            il.Emit(OpCodes.Castclass, parameters[1].ParameterType);

            il.EmitCall(OpCodes.Call, method, null);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ret);

            ForwarderDelegate fd = dm.CreateDelegate<ForwarderDelegate>();
            lock (Forwarders)
            {
                Forwarders[method] = fd;
            }
            return fd;
        }
    }

    private readonly List<EventEntry> EventEntries = new();

    private void RemoveDeadEntries()
        => EventEntries.RemoveAll(ee => ee.TargetReference is not null && !ee.TargetReference.IsAlive);

    public void Add(EventHandler<TEventArgs> eh)
    {
        if (EventEntries.Count == EventEntries.Capacity)
            RemoveDeadEntries();
        MethodInfo targetMethod = eh.Method;
        WeakReference? target = eh.Target is null ? null : new WeakReference(eh.Target);
        EventEntries.Add(new(FastSmartWeakEventForwarderProvider.GetForwarder(targetMethod), targetMethod, target));
    }

    public void Remove(EventHandler<TEventArgs> eh)
    {
        for (int i = EventEntries.Count - 1; i >= 0; i--)
        {
            EventEntry entry = EventEntries[i];
            if (entry.TargetReference is null)
            {
                if (eh.Target is null && entry.TargetMethod == eh.Method)
                {
                    EventEntries.RemoveAt(i);
                    break;
                }
            }
            else
            {
                object? target = entry.TargetReference.Target;
                if (target is null)
                {
                    EventEntries.RemoveAt(i);
                }
                else if (target == eh.Target && entry.TargetMethod == eh.Method)
                {
                    EventEntries.RemoveAt(i);
                    break;
                }
            }
        }
    }

    public void Raise(object? sender, TEventArgs e)
    {
        bool needsCleanup = false;
        foreach (EventEntry ee in EventEntries.ToList())
            needsCleanup |= ee.Forwarder(ee.TargetReference, sender, e);
        if (needsCleanup)
            RemoveDeadEntries();
    }
}
