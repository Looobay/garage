using Sandbox;

public sealed class Radio : Component
{
	/// <summary>
	/// You need to put here a sound event not a sound file !!
	/// </summary>
	[Property] public SoundEvent radioSound { get; set; }

	/// <summary>
	/// The volume.
	/// </summary>
	[Property] public float Volume { get; set; } = 1;
	
	/// <summary>
	/// The pitch.
	/// </summary>
	[Property] public float Pitch { get; set; } = 1;
	protected override void OnUpdate()
	{
	}

	protected override void OnEnabled()
	{
		var sound = Sound.Play( radioSound, Transform.World.Position );
		sound.Pitch = Pitch;
		sound.Volume = Volume;
	}
}
