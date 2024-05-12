using Sandbox;

public sealed class Radio : Component
{
	protected override void OnUpdate()
	{
	}

	protected override void OnEnabled()
	{
		var sound = Sound.Play( "portal_radio_uncompressed", Transform.World.Position );
		sound.Pitch = 1.0f;
		sound.Volume = 2.5f;
	}
}
