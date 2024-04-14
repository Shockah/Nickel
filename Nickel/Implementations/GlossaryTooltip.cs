using System.Text;

namespace Nickel;

public sealed class GlossaryTooltip(string key) : TTGlossary(key)
{
	public Spr? Icon = null;
	public Color? IconColor = null;
	public Color? TitleColor = null;
	public string? Title = null;
	public string? Description = null;
	public bool IsWideIcon = false;
	public bool FlipIconX = false;
	public bool FlipIconY = false;

	public override Rect Render(G g, bool dontDraw)
	{
		var sb = new StringBuilder();
		if (!string.IsNullOrEmpty(this.Title))
		{
			if (this.Icon is not null)
			{
				sb.Append(GetIndent());
				if (this.IsWideIcon)
					sb.Append(GetIndent());
			}

			if (this.TitleColor is not null)
				sb.Append($"<c={this.TitleColor.Value.ToString()}>");
			sb.Append(this.Title.ToUpper());
			if (this.TitleColor is not null)
				sb.Append("</c>");
		}
		if (!string.IsNullOrEmpty(this.Description))
		{
			if (this.Icon is not null || !string.IsNullOrEmpty(this.Title))
				sb.Append('\n');
			sb.Append(this.Description);
		}

		var rect = Draw.Text(sb.ToString(), 0, 0, color: Colors.textMain, maxWidth: 100, dontDraw: true);
		if (!dontDraw)
		{
			var xy = g.Push(null, rect).rect.xy;
			if (this.Icon is { } icon)
				Draw.Sprite(icon, xy.x - 1, xy.y + 2, this.FlipIconX, flipY: this.FlipIconY, color: this.IconColor);
			Draw.Text(sb.ToString(), xy.x, xy.y + 4, color: Colors.textMain, maxWidth: 100);
			g.Pop();
		}
		return rect;
	}
}
