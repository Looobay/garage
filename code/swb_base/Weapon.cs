﻿using SWB.Shared;
using System.Linq;

namespace SWB.Base;

[Group( "SWB" )]
[Title( "Weapon" )]
public partial class Weapon : Component, IInventoryItem
{
	public IPlayerBase Owner { get; set; }
	public ViewModelHandler ViewModelHandler { get; set; }
	public SkinnedModelRenderer ViewModelRenderer { get; private set; }
	public SkinnedModelRenderer ViewModelHandsRenderer { get; private set; }
	public SkinnedModelRenderer WorldModelRenderer { get; private set; }

	protected override void OnAwake()
	{
		Tags.Add( TagsHelper.Weapon );
	}

	protected override void OnDestroy()
	{
		ViewModelRenderer?.GameObject?.Destroy();
	}

	protected override void OnEnabled()
	{
		if ( IsProxy ) return;
		if ( ViewModelRenderer?.GameObject is not null )
			ViewModelRenderer.GameObject.Enabled = true;

		CreateUI();
	}

	protected override void OnDisabled()
	{
		if ( IsProxy ) return;
		if ( ViewModelRenderer?.GameObject is not null )
			ViewModelRenderer.GameObject.Enabled = false;

		if ( ViewModelHandler is not null )
			ViewModelHandler.ShouldDraw = false;

		IsReloading = false;

		DestroyUI();
	}

	[Broadcast]
	public void OnCarryStart()
	{
		GameObject.Enabled = true;
	}

	[Broadcast]
	public void OnCarryStop()
	{
		GameObject.Enabled = false;
	}

	public bool CanCarryStop()
	{
		return TimeSinceDeployed > 0;
	}

	public void OnDeploy()
	{
		var delay = 0f;

		if ( Primary.Ammo == 0 && !string.IsNullOrEmpty( DrawEmptyAnim ) )
		{
			ViewModelRenderer?.Set( DrawEmptyAnim, true );
			delay = DrawEmptyTime;
		}
		else if ( !string.IsNullOrEmpty( DrawAnim ) )
		{
			ViewModelRenderer?.Set( DrawAnim, true );
			delay = DrawTime;
		}

		TimeSinceDeployed = -delay;

		// Start drawing
		ViewModelHandler.ShouldDraw = true;
	}

	protected override void OnStart()
	{
		Owner = Components.GetInAncestors<IPlayerBase>();

		CreateModels();
	}

	protected override void OnUpdate()
	{
		UpdateModels();

		Owner.AnimationHelper.HoldType = HoldType;

		if ( !IsProxy )
		{
			if ( TimeSinceDeployed < 0 ) return;

			IsAiming = !Owner.IsRunning && AimAnimData != AngPos.Zero && Input.Down( InputButtonHelper.SecondaryAttack );

			if ( IsAiming )
				Owner.InputSensitivity = AimSensitivity;

			ResetBurstFireCount( Primary, InputButtonHelper.PrimaryAttack );
			ResetBurstFireCount( Secondary, InputButtonHelper.SecondaryAttack );
			BarrelHeatCheck();

			var shouldTuck = ShouldTuck();

			if ( CanPrimaryShoot() && !shouldTuck )
			{
				TimeSincePrimaryShoot = 0;
				Shoot( Primary, true );
			}
			else if ( CanSecondaryShoot() && !shouldTuck )
			{
				TimeSinceSecondaryShoot = 0;
				Shoot( Secondary, false );
			}
			else if ( Input.Down( InputButtonHelper.Reload ) )
			{
				Reload();
			}

			if ( IsReloading && TimeSinceReload >= 0 )
			{
				OnReloadFinish();
			}
		}
	}

	void UpdateModels()
	{
		if ( !IsProxy && WorldModelRenderer is not null )
		{
			WorldModelRenderer.RenderType = Owner.IsFirstPerson ? ModelRenderer.ShadowRenderType.ShadowsOnly : ModelRenderer.ShadowRenderType.On;
		}
	}

	void CreateModels()
	{
		if ( !IsProxy && ViewModel is not null && ViewModelRenderer is null )
		{
			var viewModelGO = new GameObject( true, "Viewmodel" );
			viewModelGO.SetParent( Owner.GameObject, false );
			viewModelGO.Tags.Add( TagsHelper.ViewModel );
			viewModelGO.Flags |= GameObjectFlags.NotNetworked;

			ViewModelRenderer = viewModelGO.Components.Create<SkinnedModelRenderer>();
			ViewModelRenderer.Model = ViewModel;
			ViewModelRenderer.AnimationGraph = ViewModel.AnimGraph;
			ViewModelRenderer.Enabled = false;
			ViewModelRenderer.OnComponentEnabled += () =>
			{
				// Prevent flickering when enabling the component, this is controlled by the ViewModelHandler
				ViewModelRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
				OnDeploy();
			};

			ViewModelHandler = viewModelGO.Components.Create<ViewModelHandler>();
			ViewModelHandler.Weapon = this;
			ViewModelHandler.ViewModelRenderer = ViewModelRenderer;
			ViewModelHandler.Camera = Owner.ViewModelCamera;

			if ( ViewModelHands is not null )
			{
				ViewModelHandsRenderer = viewModelGO.Components.Create<SkinnedModelRenderer>();
				ViewModelHandsRenderer.Model = ViewModelHands;
				ViewModelHandsRenderer.BoneMergeTarget = ViewModelRenderer;
				ViewModelHandsRenderer.OnComponentEnabled += () =>
				{
					// Prevent flickering when enabling the component, this is controlled by the ViewModelHandler
					ViewModelHandsRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
				};
			}

			ViewModelHandler.ViewModelHandsRenderer = ViewModelHandsRenderer;
		}

		if ( WorldModel is not null && WorldModelRenderer is null )
		{
			WorldModelRenderer = Components.Create<SkinnedModelRenderer>();
			WorldModelRenderer.Model = WorldModel;
			WorldModelRenderer.CreateBoneObjects = true;

			var bodyRenderer = Owner.Body.Components.Get<SkinnedModelRenderer>();
			var holdBone = bodyRenderer.Model.Bones.AllBones.FirstOrDefault( bone => bone.Name == "hold_R" );
			var holdBoneGO = bodyRenderer.GetBoneObject( holdBone );

			this.GameObject.SetParent( holdBoneGO );
			WorldModelRenderer.Transform.Position = holdBoneGO.Transform.Position;
			WorldModelRenderer.Transform.Rotation = holdBoneGO.Transform.Rotation;
		}
	}

	[Broadcast]
	void PlaySound( int resourceID )
	{
		var sound = ResourceLibrary.Get<SoundEvent>( resourceID );
		var isScreenSound = CanSeeViewModel;
		sound.UI = isScreenSound;

		if ( isScreenSound )
		{
			Sound.Play( sound );
		}
		else
		{
			Sound.Play( sound, Transform.Position );
		}
	}
}
