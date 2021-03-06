﻿using System;
using System.Runtime.Remoting.Messaging;
using Miruken.Infrastructure;

namespace Miruken.Callback
{
    public interface IResolving {}

    public partial class Handler
    {
        object IProtocolAdapter.Dispatch(IMethodCallMessage message)
        {
            var handleMethod = new HandleMethod(message);
            var protocol     = handleMethod.TargetType;

            bool broadcast  = false,
                 useResolve = typeof(IResolving).IsAssignableFrom(protocol);

            var semantics = GetSemantics(this);
            if (semantics != null)
            {
                broadcast  = semantics.HasOption(CallbackOptions.Broadcast);
                useResolve = useResolve || semantics.HasOption(CallbackOptions.Resolve);
            }

            var callback = useResolve
                         ? new ResolveMethod(handleMethod, broadcast)
                         : (object)handleMethod;

            var handled = Handle(callback, broadcast && !useResolve);
            if (!handled && (semantics == null ||
                !semantics.HasOption(CallbackOptions.BestEffot)))
                throw new MissingMethodException();

            return handled ? handleMethod.Result 
                 : ReflectionHelper.GetDefault(handleMethod.ResultType);
        }

        private static CallbackSemantics GetSemantics(IHandler handler)
        {
            var semantics = new CallbackSemantics();
            return handler.Handle(semantics, true) ? semantics : null;
        }

        private bool TryHandleMethod(object callback, bool greedy, IHandler composer)
        {
            var handleMethod = callback as HandleMethod;
            if (handleMethod == null) return false;
            var handled = Surrogate != null && handleMethod.InvokeOn(Surrogate, composer);
            if (!handled || greedy)
                handled = handleMethod.InvokeOn(this, composer) || handled;
            return handled;
        }

        private static bool TryResolveMethod(object callback, IHandler composer)
        {
            var resolveMethod = callback as ResolveMethod;
            return resolveMethod != null && resolveMethod.InvokeResolve(composer);
        }

        public static IHandler Composer => HandleMethod.Composer;

        public static void Unhandled()
        {
            HandleMethod.Unhandled = true;
        }

        public static Ret Unhandled<Ret>()
        {
            HandleMethod.Unhandled = true;
            return default (Ret);
        }
    }
}
