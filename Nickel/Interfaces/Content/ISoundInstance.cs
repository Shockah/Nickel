namespace Nickel;

public interface ISoundInstance
{
	ISoundEntry Entry { get; }
	bool IsPaused { get; set; }
	float Volume { get; set; }
	float Pitch { get; set; }
}
