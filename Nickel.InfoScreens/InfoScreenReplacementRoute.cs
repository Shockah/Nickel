using Newtonsoft.Json;

namespace Nickel.InfoScreens;

internal sealed class InfoScreenReplacementRoute : Route
{
	[JsonIgnore]
	public required InfoScreenEntry? Entry { get; init; }
	
	public required Route Route { get; init; }
	public required Route? OriginalRoute { get; init; }
	
	public override bool TryCloseSubRoute(G g, Route r, object? arg)
	{
		if (this.Route.TryCloseSubRoute(g, r, arg))
			return true;
		if (r != this.Route)
			return false;
		
		if (this.Entry is not null)
			ModEntry.Instance.OnClose(g, this.Entry);
		if (g.state.route is Combat combat)
			combat.routeOverride = this.OriginalRoute;
		else if (this.OriginalRoute is not null)
			g.state.route = this.OriginalRoute;
		return true;
	}

	public override void Render(G g)
		=> this.Route.Render(g);
}
