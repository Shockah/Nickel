using System;

namespace Nickel.ModSettings;

public sealed class TwoColumnModSetting : BaseModSetting, IModSettingsApi.ITwoColumnModSetting
{
	public required IModSettingsApi.IModSetting Left { get; set; }
	public required IModSettingsApi.IModSetting Right { get; set; }
	public Func<Rect, double>? LeftWidth { get; set; }
	public Func<Rect, double>? RightWidth { get; set; }
	public Func<Rect, double>? Spacing { get; set; }
	public IModSettingsApi.VerticalAlignmentOrFill Alignment { get; set; } = IModSettingsApi.VerticalAlignmentOrFill.Fill;

	public TwoColumnModSetting()
	{
		this.OnMenuOpen += (g, route) =>
		{
			this.Left?.RaiseOnMenuOpen(g, route);
			this.Right?.RaiseOnMenuOpen(g, route);
		};
		this.OnMenuClose += g =>
		{
			this.Left?.RaiseOnMenuClose(g);
			this.Right?.RaiseOnMenuClose(g);
		};
	}

	IModSettingsApi.ITwoColumnModSetting IModSettingsApi.ITwoColumnModSetting.SetLeft(IModSettingsApi.IModSetting value)
	{
		this.Left = value;
		return this;
	}

	IModSettingsApi.ITwoColumnModSetting IModSettingsApi.ITwoColumnModSetting.SetRight(IModSettingsApi.IModSetting value)
	{
		this.Right = value;
		return this;
	}

	IModSettingsApi.ITwoColumnModSetting IModSettingsApi.ITwoColumnModSetting.SetLeftWidth(Func<Rect, double>? value)
	{
		this.LeftWidth = value;
		return this;
	}

	IModSettingsApi.ITwoColumnModSetting IModSettingsApi.ITwoColumnModSetting.SetRightWidth(Func<Rect, double>? value)
	{
		this.RightWidth = value;
		return this;
	}

	IModSettingsApi.ITwoColumnModSetting IModSettingsApi.ITwoColumnModSetting.SetSpacing(Func<Rect, double>? value)
	{
		this.Spacing = value;
		return this;
	}

	IModSettingsApi.ITwoColumnModSetting IModSettingsApi.ITwoColumnModSetting.SetAlignment(IModSettingsApi.VerticalAlignmentOrFill value)
	{
		this.Alignment = value;
		return this;
	}

	public override Vec? Render(G g, Box box, bool dontDraw)
	{
		int leftWidth;
		int rightWidth;
		int spacing;

		if (this.LeftWidth is null && this.RightWidth is null)
		{
			spacing = (int)(this.Spacing?.Invoke(box.rect) ?? 0);
			leftWidth = (int)((box.rect.w - spacing) / 2);
			rightWidth = (int)(box.rect.w - spacing) - leftWidth;
		}
		else if (this.LeftWidth is not null && this.RightWidth is not null)
		{
			leftWidth = (int)this.LeftWidth(box.rect);
			rightWidth = (int)this.RightWidth(box.rect);
			spacing = (int)(box.rect.w - leftWidth - rightWidth);
		}
		else if (this.LeftWidth is not null)
		{
			leftWidth = (int)this.LeftWidth(box.rect);
			spacing = (int)(this.Spacing?.Invoke(box.rect) ?? 0);
			rightWidth = (int)(box.rect.w - leftWidth - spacing);
		}
		else if (this.RightWidth is not null)
		{
			rightWidth = (int)this.RightWidth(box.rect);
			spacing = (int)(this.Spacing?.Invoke(box.rect) ?? 0);
			leftWidth = (int)(box.rect.w - rightWidth - spacing);
		}
		else
		{
			throw new ArgumentException($"A {this.GetType()} cannot have all three {nameof(this.LeftWidth)}, {nameof(this.RightWidth)} and {nameof(this.Spacing)} properties set");
		}
		
		var sizingBox = g.Push(null, new Rect(box.rect.x, box.rect.y, leftWidth, 0));
		var nullableLeftSize = this.Left.Render(g, sizingBox, dontDraw: true);
		g.Pop();
		
		sizingBox = g.Push(null, new Rect(box.rect.x + spacing + leftWidth, box.rect.y, rightWidth, 0));
		var nullableRightSize = this.Right.Render(g, sizingBox, dontDraw: true);
		g.Pop();

		if (nullableLeftSize is null && nullableRightSize is null)
			return null;
		
		var maxHeight = dontDraw ? (int)Math.Max(nullableLeftSize?.y ?? 0, nullableRightSize?.y ?? 0) : box.rect.h;

		if (!dontDraw)
		{
			if (nullableLeftSize is { } leftSize)
			{
				var yOffset = this.Alignment switch
				{
					IModSettingsApi.VerticalAlignmentOrFill.Top => 0,
					IModSettingsApi.VerticalAlignmentOrFill.Center => (maxHeight - leftSize.y) / 2,
					IModSettingsApi.VerticalAlignmentOrFill.Bottom => maxHeight - leftSize.y,
					IModSettingsApi.VerticalAlignmentOrFill.Fill => 0,
					_ => throw new ArgumentOutOfRangeException()
				};
			
				var childBox = g.Push(this.Left.Key, new Rect(0, yOffset, leftSize.x, this.Alignment == IModSettingsApi.VerticalAlignmentOrFill.Fill ? maxHeight : leftSize.y));
				this.Left.Render(g, childBox, dontDraw: false);
				g.Pop();
			}
			
			if (nullableRightSize is { } rightSize)
			{
				var yOffset = this.Alignment switch
				{
					IModSettingsApi.VerticalAlignmentOrFill.Top => 0,
					IModSettingsApi.VerticalAlignmentOrFill.Center => (maxHeight - rightSize.y) / 2,
					IModSettingsApi.VerticalAlignmentOrFill.Bottom => maxHeight - rightSize.y,
					IModSettingsApi.VerticalAlignmentOrFill.Fill => 0,
					_ => throw new ArgumentOutOfRangeException()
				};
			
				var childBox = g.Push(this.Right.Key, new Rect(leftWidth + spacing, yOffset, rightSize.x, this.Alignment == IModSettingsApi.VerticalAlignmentOrFill.Fill ? maxHeight : rightSize.y));
				this.Right.Render(g, childBox, dontDraw: false);
				g.Pop();
			}
		}

		return new Vec(box.rect.w, maxHeight);
	}
}
