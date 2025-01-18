using daisyowl.text;
using FSPRO;
using System;
using System.Collections.Generic;

namespace Nickel.ModSettings;

public sealed class StepperModSetting<T> : BaseModSetting, OnMouseDown, OnInputPhase, IModSettingsApi.IStepperModSetting<T> where T : struct
{
	private const double PreferredHeight = 20;
	
	public required Func<string> Title { get; set; }
	public required Func<T> Getter { get; set; }
	public required Action<T> Setter { get; set; }
	public required Func<T, T?> PreviousValue { get; set; }
	public required Func<T, T?> NextValue { get; set; }
	public int MultipleStepsCount { get; set; } = 1;
	public Func<T, string>? ValueFormatter { get; set; }
	public Func<Rect, double>? ValueWidth { get; set; }
	public Action<G, IModSettingsApi.IModSettingsRoute>? OnClick { get; set; }
	public Func<IEnumerable<Tooltip>>? Tooltips { get; set; }

	private UIKey StepperLeftKey;
	private UIKey StepperRightKey;

	public StepperModSetting()
	{
		this.OnMenuOpen += (_, _) =>
		{
			if (this.StepperLeftKey == 0)
				this.StepperLeftKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
			if (this.StepperRightKey == 0)
				this.StepperRightKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
		};
	}

	~StepperModSetting()
	{
		if (this.StepperLeftKey != 0)
			ModEntry.Instance.Helper.Utilities.FreeEnumCase(this.StepperLeftKey.k);
		if (this.StepperRightKey != 0)
			ModEntry.Instance.Helper.Utilities.FreeEnumCase(this.StepperRightKey.k);
	}

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

	IModSettingsApi.IStepperModSetting<T> IModSettingsApi.IStepperModSetting<T>.SetMultipleStepsCount(int value)
	{
		this.MultipleStepsCount = value;
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

	IModSettingsApi.IStepperModSetting<T> IModSettingsApi.IStepperModSetting<T>.SetOnClick(Action<G, IModSettingsApi.IModSettingsRoute>? value)
	{
		this.OnClick = value;
		return this;
	}

	IModSettingsApi.IStepperModSetting<T> IModSettingsApi.IStepperModSetting<T>.SetTooltips(Func<IEnumerable<Tooltip>>? value)
	{
		this.Tooltips = value;
		return this;
	}

	public override Vec? Render(G g, Box box, bool dontDraw)
	{
		if (box.key is not null)
		{
			box.autoFocus = true;
			box.onMouseDown = this;
			box.onInputPhase = this;
			box.leftHint = box.key;
			box.rightHint = box.key;
		}
		
		if (!dontDraw)
		{
			var isHover = (box.key is not null && box.IsHover()) || g.hoverKey == this.StepperLeftKey || g.hoverKey == this.StepperRightKey;
			if (isHover)
			{
				Draw.Rect(box.rect.x, box.rect.y, box.rect.w, box.rect.h, Colors.menuHighlightBox.gain(0.5), BlendMode.Screen);
				if (this.Tooltips is { } tooltips)
					g.tooltips.Add(new Vec(box.rect.x2 - Tooltip.WIDTH, box.rect.y2), tooltips());
			}

			var textColor = isHover ? Colors.textChoiceHoverActive : Colors.textMain;
			var value = this.Getter();
			var valueText = this.ValueFormatter is { } valueFormatter ? valueFormatter(value) : (value.ToString() ?? "<null>");
			var valueWidth = this.ValueWidth?.Invoke(box.rect) ?? 44;

			Draw.Text(this.Title(), box.rect.x + 10, box.rect.y + 5 + (int)((box.rect.h - PreferredHeight) / 2), DB.thicket, textColor);
			Draw.Text(valueText, (int)(box.rect.x2 - 10 - 18 - valueWidth / 2), box.rect.y + 5 + (int)((box.rect.h - PreferredHeight) / 2), DB.thicket, textColor, align: TAlign.Center);

			if (this.PreviousValue(value) is not null)
				SharedArt.ButtonSprite(g, new Rect(box.rect.w - 10 - 18 * 2 - valueWidth, -1 + (int)((box.rect.h - PreferredHeight) / 2), 18, 21), this.StepperLeftKey, StableSpr.buttons_selectShip, StableSpr.buttons_selectShip_on, boxColor: Colors.buttonBoxNormal, flipX: true, onMouseDown: Input.gamepadIsActiveInput ? null : this, noHover: Input.gamepadIsActiveInput);
			if (this.NextValue(value) is not null)
				SharedArt.ButtonSprite(g, new Rect(box.rect.w - 10 - 18, -1 + (int)((box.rect.h - PreferredHeight) / 2), 18, 21), this.StepperRightKey, StableSpr.buttons_selectShip, StableSpr.buttons_selectShip_on, boxColor: Colors.buttonBoxNormal, flipX: false, onMouseDown: Input.gamepadIsActiveInput ? null : this, noHover: Input.gamepadIsActiveInput);
		}

		return new(box.rect.w, dontDraw ? PreferredHeight : box.rect.h);
	}

	public void OnMouseDown(G g, Box b)
	{
		Audio.Play(Event.Click);

		if (b.key == this.StepperLeftKey)
		{
			var steps = Input.shift ? this.MultipleStepsCount : 1;
			for (var i = 0; i < steps; i++)
			{
				var value = this.Getter();
				if (this.PreviousValue(value) is not { } newValue)
					break;
				this.Setter(newValue);
			}
		}
		else if (b.key == this.StepperRightKey)
		{
			var steps = Input.shift ? this.MultipleStepsCount : 1;
			for (var i = 0; i < steps; i++)
			{
				var value = this.Getter();
				if (this.NextValue(value) is not { } newValue)
					return;
				this.Setter(newValue);
			}
		}
		else if (b.key == this.Key)
		{
			this.OnClick?.Invoke(g, this.CurrentRoute);
		}
	}

	public void OnInputPhase(G g, Box b)
	{
		if (g.hoverKey != this.Key)
			return;
		
		var value = this.Getter();
		
		if (Input.GetGpDown(Btn.DpLeft))
		{
			if (this.PreviousValue(value) is not { } newValue)
				return;
			Audio.Play(Event.Click);
			this.Setter(newValue);
		}
		if (Input.GetGpDown(Btn.DpRight))
		{
			if (this.NextValue(value) is not { } newValue)
				return;
			Audio.Play(Event.Click);
			this.Setter(newValue);
		}
	}
}
