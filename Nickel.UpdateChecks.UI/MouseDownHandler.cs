using System;

namespace Nickel.UpdateChecks.UI;

internal sealed record MouseDownHandler(Action Delegate) : OnMouseDown, OnMouseDownRight
{
	public void OnMouseDown(G _1, Box _2)
		=> this.Delegate();

	public void OnMouseDownRight(G _1, Box _2)
		=> this.Delegate();
}
