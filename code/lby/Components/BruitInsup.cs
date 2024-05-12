using Sandbox;

public sealed class BruitInsup : Component
{
	protected override void OnUpdate()
	{
	}

	protected override void OnEnabled()
	{
		var sound = Sound.Play( "fluorescent_light", Transform.World.Position );
		sound.Pitch = 1f;
		sound.Volume = 1.3f;
	}
}
