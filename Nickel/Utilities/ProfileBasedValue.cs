using System;

namespace Nickel;

/// <summary>
/// A type that helps with seamless switching of values between profiles.
/// </summary>
/// <typeparam name="TProfile">The profiles type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
public sealed class ProfileBasedValue<TProfile, TData> : IProfileBasedValue<TProfile, TData>
{
	private readonly Func<TProfile> ActiveProfileGetter;
	private readonly Action<TProfile> ActiveProfileSetter;
	private readonly Func<TProfile, TData> DataGetter;
	private readonly Action<TProfile, TData> DataSetter;
	private readonly Func<TData, TData> DataCopier;
	
	internal ProfileBasedValue(
		Func<TProfile> activeProfileGetter,
		Action<TProfile> activeProfileSetter,
		Func<TProfile, TData> dataGetter,
		Action<TProfile, TData> dataSetter,
		Func<TData, TData> dataCopier
	)
	{
		this.ActiveProfileGetter = activeProfileGetter;
		this.ActiveProfileSetter = activeProfileSetter;
		this.DataGetter = dataGetter;
		this.DataSetter = dataSetter;
		this.DataCopier = dataCopier;
	}
	
	/// <inheritdoc/>
	public TProfile ActiveProfile
	{
		get => this.ActiveProfileGetter();
		set => this.ActiveProfileSetter(value);
	}

	/// <inheritdoc/>
	public TData Current
	{
		get => this.DataGetter(this.ActiveProfile);
		set => this.DataSetter(this.ActiveProfile, value);
	}

	/// <inheritdoc/>
	public void Import(TProfile profile)
	{
		if (Equals(profile, this.ActiveProfile))
			return;
		this.DataSetter(this.ActiveProfile, this.DataCopier(this.DataGetter(profile)));
	}
}

/// <summary>
/// Hosts extensions related to <see cref="ProfileBasedValue{TProfile,TData}"/> objects.
/// </summary>
public static class ProfileBasedValue
{
	/// <summary>
	/// Creates a new <see cref="ProfileBasedValue{TProfile,TData}"/>.
	/// </summary>
	/// <param name="activeProfileGetter">The getter for the currently active profile.</param>
	/// <param name="activeProfileSetter">The setter for the currently active profile.</param>
	/// <param name="dataGetter">The getter for the data for a given profile.</param>
	/// <param name="dataSetter">The setter for the data for a given profile.</param>
	/// <param name="dataCopier">A function that makes a copy of the given data.</param>
	/// <returns>A new <see cref="ProfileBasedValue{TProfile,TData}"/>.</returns>
	public static ProfileBasedValue<TProfile, TData> Create<TProfile, TData>(
		Func<TProfile> activeProfileGetter,
		Action<TProfile> activeProfileSetter,
		Func<TProfile, TData> dataGetter,
		Action<TProfile, TData> dataSetter,
		Func<TData, TData> dataCopier
	)
		=> new ProfileBasedValue<TProfile, TData>(activeProfileGetter, activeProfileSetter, dataGetter, dataSetter, dataCopier);

	/// <summary>
	/// Creates a new <see cref="ProfileBasedValue{TProfile,TData}"/>, which copies its data if needed via <see cref="Mutil.DeepCopy{T}"/>.
	/// </summary>
	/// <param name="activeProfileGetter">The getter for the currently active profile.</param>
	/// <param name="activeProfileSetter">The setter for the currently active profile.</param>
	/// <param name="dataGetter">The getter for the data for a given profile.</param>
	/// <param name="dataSetter">The setter for the data for a given profile.</param>
	/// <returns>A new <see cref="ProfileBasedValue{TProfile,TData}"/>.</returns>
	public static ProfileBasedValue<TProfile, TData> Create<TProfile, TData>(
		Func<TProfile> activeProfileGetter,
		Action<TProfile> activeProfileSetter,
		Func<TProfile, TData> dataGetter,
		Action<TProfile, TData> dataSetter
	) where TData : class
		=> Create(activeProfileGetter, activeProfileSetter, dataGetter, dataSetter, dataCopier: Mutil.DeepCopy);
}
