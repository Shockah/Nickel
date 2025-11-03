namespace Nickel.InfoScreens;

internal sealed class InfoScreenReplacementRoute(InfoScreenEntry entry, Route route, Route? originalRoute) : Route
{
	public readonly InfoScreenEntry Entry = entry;
	
	public override bool TryCloseSubRoute(G g, Route r, object? arg)
	{
		if (r.TryCloseSubRoute(g, r, arg))
			return true;
		if (r != route)
			return false;
		
		ModEntry.Instance.OnClose(g, this.Entry);
		if (g.state.route is Combat combat)
			combat.routeOverride = originalRoute;
		else if (originalRoute is not null)
			g.state.route = originalRoute;
		return true;
	}

	public override void Render(G g)
		=> route.Render(g);
}
