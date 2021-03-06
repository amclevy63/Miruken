﻿namespace Miruken.Callback
{
	public class CascadeHandler : Handler
	{
		private readonly IHandler _handlerA;
		private readonly IHandler _handlerB;

		internal CascadeHandler(IHandler handlerA, IHandler handlerB)
		{
			_handlerA = handlerA;
			_handlerB = handlerB;
		}

		protected override bool HandleCallback(
            object callback, bool greedy, IHandler composer)
		{
		    var handled = base.HandleCallback(callback, greedy, composer);
			return greedy
				? handled | _handlerA.Handle(callback, true, composer) 
                   | _handlerB.Handle(callback, true, composer)
				: handled || _handlerA.Handle(callback, false, composer)
                  || _handlerB.Handle(callback, false, composer);
		}
	}
}
