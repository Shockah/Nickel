using daisyowl.text;
using FSPRO;
using System;
using System.Numerics;

namespace Nickel.ModSettings;

public readonly struct StepperModSettingConfiguration<T> where T : struct
{
	public required Func<string> Title { get; init; }
	public required Func<T> Getter { get; init; }
	public required Action<T> Setter { get; init; }
	public required Func<T, T?> PreviousValue { get; init; }
	public required Func<T, T?> NextValue { get; init; }
	public Func<T, string>? ValueFormatter { get; init; }
	public Func<Rect, double>? ValueWidth { get; init; }
	public Action? OnClick { get; init; }
}

public static class StepperModSettingConfiguration
{
	public readonly struct NumericConfiguration<T> where T : INumber<T>
	{
		public required Func<string> Title { get; init; }
		public required Func<T> Getter { get; init; }
		public required Action<T> Setter { get; init; }
		public Func<T, string>? ValueFormatter { get; init; }
		public Func<Rect, double>? ValueWidth { get; init; }
		public Action? OnClick { get; init; }
		public T? MinValue { get; init; }
		public T? MaxValue { get; init; }
		public T? Step { get; init; }
	}

	public static StepperModSettingConfiguration<T> Numeric<T>(NumericConfiguration<T> configuration) where T : struct, INumber<T>
		=> new()
		{
			Title = configuration.Title,
			Getter = configuration.Getter,
			Setter = configuration.Setter,
			PreviousValue = value =>
			{
				var newValue = configuration.Step is { } step ? value - step : value - T.One;
				if (configuration.MinValue is { } min)
				{
					if (value <= min)
						return null;
					if (newValue < min)
						return min;
				}
				return newValue;
			},
			NextValue = value =>
			{
				var newValue = configuration.Step is { } step ? value + step : value + T.One;
				if (configuration.MaxValue is { } max)
				{
					if (value >= max)
						return null;
					if (newValue > max)
						return max;
				}
				return newValue;
			},
			ValueFormatter = configuration.ValueFormatter,
			ValueWidth = configuration.ValueWidth,
		};
}

public sealed class StepperModSetting<T> : BaseModSetting, OnMouseDown where T : struct
{
	public UIKey StepperLeftKey { get; private set; }
	public UIKey StepperRightKey { get; private set; }
	private readonly StepperModSettingConfiguration<T> Configuration;

	public StepperModSetting(StepperModSettingConfiguration<T> configuration)
	{
		this.Configuration = configuration;
	}

	public override void Prepare(G g, IModSettingsApi.IModSettingsRoute route, Func<UIKey> keyGenerator)
	{
		base.Prepare(g, route, keyGenerator);

		if (this.StepperLeftKey == 0)
			this.StepperLeftKey = keyGenerator();
		if (this.StepperRightKey == 0)
			this.StepperRightKey = keyGenerator();
	}

	public override Vec? Render(G g, Box box, bool dontDraw)
	{
		if (!dontDraw)
		{
			if (box.IsHover())
				Draw.Rect(box.rect.x, box.rect.y, box.rect.w, box.rect.h, Colors.menuHighlightBox.gain(0.5), BlendMode.Screen);

			var textColor = box.IsHover() ? Colors.textChoiceHoverActive : Colors.textMain;
			var value = this.Configuration.Getter();
			var valueText = this.Configuration.ValueFormatter is { } valueFormatter ? valueFormatter(value) : (value.ToString() ?? "<null>");
			var valueWidth = this.Configuration.ValueWidth?.Invoke(box.rect) ?? 44;

			Draw.Text(this.Configuration.Title(), box.rect.x + 10, box.rect.y + 5, DB.thicket, textColor);
			Draw.Text(valueText, (int)(box.rect.x2 - 10 - 18 - valueWidth / 2), box.rect.y + 5, DB.thicket, textColor, align: TAlign.Center);

			if (this.Configuration.PreviousValue(value) is not null)
				SharedArt.ButtonSprite(g, new Rect(box.rect.w - 10 - 18 * 2 - valueWidth, -1, 18, 21), this.StepperLeftKey, StableSpr.buttons_selectShip, StableSpr.buttons_selectShip_on, boxColor: Colors.buttonBoxNormal, flipX: true, onMouseDown: this, noHover: Input.gamepadIsActiveInput);
			if (this.Configuration.NextValue(value) is not null)
				SharedArt.ButtonSprite(g, new Rect(box.rect.w - 10 - 18, -1, 18, 21), this.StepperLeftKey, StableSpr.buttons_selectShip, StableSpr.buttons_selectShip_on, boxColor: Colors.buttonBoxNormal, flipX: false, onMouseDown: this, noHover: Input.gamepadIsActiveInput);
		}

		return new(box.rect.w, 20);
	}

	public void OnMouseDown(G g, Box b)
	{
		var value = this.Configuration.Getter();
		Audio.Play(Event.Click);

		if (b.key == this.StepperLeftKey)
		{
			if (this.Configuration.PreviousValue(value) is not { } newValue)
				return;
			this.Configuration.Setter(newValue);
		}
		else if (b.key == this.StepperRightKey)
		{
			if (this.Configuration.NextValue(value) is not { } newValue)
				return;
			this.Configuration.Setter(newValue);
		}
		else if (b.key == this.Key)
		{
			this.Configuration.OnClick?.Invoke();
		}
	}
}
