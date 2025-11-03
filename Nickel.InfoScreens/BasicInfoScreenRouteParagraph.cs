using daisyowl.text;

namespace Nickel.InfoScreens;

internal sealed class BasicInfoScreenRouteParagraph(string text) : IInfoScreensApi.IBasicInfoScreenRoute.IParagraph
{
	public string Text { get; set; } = text;
	public Font? Font { get; set; }
	public Color? Color { get; set; }
	public int? MaxWidth { get; set; }

	public IInfoScreensApi.IBasicInfoScreenRoute.IParagraph SetText(string value)
	{
		this.Text = value;
		return this;
	}

	public IInfoScreensApi.IBasicInfoScreenRoute.IParagraph SetFont(Font? value)
	{
		this.Font = value;
		return this;
	}

	public IInfoScreensApi.IBasicInfoScreenRoute.IParagraph SetColor(Color? value)
	{
		this.Color = value;
		return this;
	}

	public IInfoScreensApi.IBasicInfoScreenRoute.IParagraph SetMaxWidth(int? value)
	{
		this.MaxWidth = value;
		return this;
	}
}
