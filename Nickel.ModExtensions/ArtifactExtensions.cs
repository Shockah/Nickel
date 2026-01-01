namespace Nickel.ModExtensions;

public static class ArtifactExtensions
{
	extension(Artifact artifact)
	{
		/// <summary>
		/// The entry for this <see cref="Artifact"/>, if it's registered.
		/// </summary>
		public IArtifactEntry? Entry
			=> ModExtensions.Helper.Content.Artifacts.LookupByArtifactType(artifact.GetType());
	}
}
