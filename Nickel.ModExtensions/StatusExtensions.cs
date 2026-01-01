namespace Nickel.ModExtensions;

public static class StatusExtensions
{
	extension(Status status)
	{
		/// <summary>
		/// The entry for this <see cref="Status"/>, if it's registered.
		/// </summary>
		public IStatusEntry? Entry
			=> ModExtensions.Helper.Content.Statuses.LookupByStatus(status);
	}
}
