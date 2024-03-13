using System.Text;

namespace Nickel;

/// <summary>
/// A tooltip, mimicking one that would be used for a game glossary.
/// </summary>
/// <param name="key">The key used for deduplicating tooltips - if two tooltips have the same key, only the first one will be rendered.</param>
public sealed class GlossaryTooltip(string key) : TTGlossary(key)
{
	/// <summary>The icon to show next to the title. If a title is not provided, this icon will not show.</summary>
	public Spr? Icon = null;
	
	/// <summary>The color for the icon. If provided, the icon's texture is multiplied by this color.</summary>
	public Color? IconColor = null;
	
	/// <summary>The color for the title text.</summary>
	public Color? TitleColor = null;
	
	/// <summary>The title. If not provided, the icon (<see cref="Icon"/>) will not show.</summary>
	public string? Title = null;
	
	/// <summary>The description.</summary>
	public string? Description = null;
	
	/// <summary>Whether the provided icon is wide - double the width (18 px) than usual (9 px). This setting offsets the title to make space for the wide icon.</summary>
	public bool IsWideIcon = false;
	
	/// <summary>Whether the icon should be flipped horizontally.</summary>
	public bool FlipIconX = false;
	
	/// <summary>Whether the icon should be flipped vertically.</summary>
	public bool FlipIconY = false;

	/// <inheritdoc/>
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
