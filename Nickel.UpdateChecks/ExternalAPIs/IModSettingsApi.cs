using System;

namespace Nickel.UpdateChecks;

public interface IModSettingsApi
{
	void RegisterModSettings(IModSetting settings);

	public interface IModSetting
	{
		UIKey Key { get; }

		void Prepare(G g, IModSettingsRoute route, Func<UIKey> keyGenerator);
		Vec? Render(G g, Box box, bool dontDraw);
	}

	public interface IModSettingsRoute
	{
		Route AsRoute { get; }

		void CloseRoute(G g);
		void OpenSubroute(G g, Route route);
	}
}
