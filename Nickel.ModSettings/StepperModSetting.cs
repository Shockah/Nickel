using daisyowl.text;
using FSPRO;
using System;

namespace Nickel.ModSettings;

public sealed class StepperModSetting<T> : BaseModSetting, OnMouseDown, IModSettingsApi.IStepperModSetting<T> where T : struct
{
	public required Func<string> Title { get; set; }
	public required Func<T> Getter { get; set; }
	public required Action<T> Setter { get; set; }
	public required Func<T, T?> PreviousValue { get; set; }
	public required Func<T, T?> NextValue { get; set; }
	public Func<T, string>? ValueFormatter { get; set; }
	public Func<Rect, double>? ValueWidth { get; set; }
	public Action<G, IModSettingsApi.IModSettingsRoute>? OnClick { get; set; }

	private UIKey StepperLeftKey;
	private UIKey StepperRightKey;

	IModSettingsApi.IStepperModSetting<T> IModSettingsApi.IStepperModSetting<T>.SetTitle(Func<string> value)
	{
		this.Title = value;
		return this;
	}

	IModSettingsApi.IStepperModSetting<T> IModSettingsApi.IStepperModSetting<T>.SetGetter(Func<T> value)
	{
		this.Getter = value;
		return this;
	}

	IModSettingsApi.IStepperModSetting<T> IModSettingsApi.IStepperModSetting<T>.SetSetter(Action<T> value)
	{
		this.Setter = value;
		return this;
	}

	IModSettingsApi.IStepperModSetting<T> IModSettingsApi.IStepperModSetting<T>.SetPreviousValue(Func<T, T?> value)
	{
		this.PreviousValue = value;
		return this;
	}

	IModSettingsApi.IStepperModSetting<T> IModSettingsApi.IStepperModSetting<T>.SetNextValue(Func<T, T?> value)
	{
		this.NextValue = value;
		return this;
	}

	IModSettingsApi.IStepperModSetting<T> IModSettingsApi.IStepperModSetting<T>.SetValueFormatter(Func<T, string>? value)
	{
		this.ValueFormatter = value;
		return this;
	}

	IModSettingsApi.IStepperModSetting<T> IModSettingsApi.IStepperModSetting<T>.SetValueWidth(Func<Rect, double>? value)
	{
		this.ValueWidth = value;
		return this;
	}

	IModSettingsApi.IStepperModSetting<T> IModSettingsApi.IStepperModSetting<T>.SetOnClick(Action<G, IModSettingsApi.IModSettingsRoute> value)
	{
		this.OnClick = value;
		return this;
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
			var value = this.Getter();
			var valueText = this.ValueFormatter is { } valueFormatter ? valueFormatter(value) : (value.ToString() ?? "<null>");
			var valueWidth = this.ValueWidth?.Invoke(box.rect) ?? 44;

			Draw.Text(this.Title(), box.rect.x + 10, box.rect.y + 5, DB.thicket, textColor);
			Draw.Text(valueText, (int)(box.rect.x2 - 10 - 18 - valueWidth / 2), box.rect.y + 5, DB.thicket, textColor, align: TAlign.Center);

			if (this.PreviousValue(value) is not null)
				SharedArt.ButtonSprite(g, new Rect(box.rect.w - 10 - 18 * 2 - valueWidth, -1, 18, 21), this.StepperLeftKey, StableSpr.buttons_selectShip, StableSpr.buttons_selectShip_on, boxColor: Colors.buttonBoxNormal, flipX: true, onMouseDown: this, noHover: Input.gamepadIsActiveInput);
			if (this.NextValue(value) is not null)
				SharedArt.ButtonSprite(g, new Rect(box.rect.w - 10 - 18, -1, 18, 21), this.StepperRightKey, StableSpr.buttons_selectShip, StableSpr.buttons_selectShip_on, boxColor: Colors.buttonBoxNormal, flipX: false, onMouseDown: this, noHover: Input.gamepadIsActiveInput);
		}

		return new(box.rect.w, 20);
	}

	public void OnMouseDown(G g, Box b)
	{
		var value = this.Getter();
		Audio.Play(Event.Click);

		if (b.key == this.StepperLeftKey)
		{
			if (this.PreviousValue(value) is not { } newValue)
				return;
			this.Setter(newValue);
		}
		else if (b.key == this.StepperRightKey)
		{
			if (this.NextValue(value) is not { } newValue)
				return;
			this.Setter(newValue);
		}
		else if (b.key == this.Key)
		{
			this.OnClick?.Invoke(g, this.CurrentRoute);
		}
	}
}
