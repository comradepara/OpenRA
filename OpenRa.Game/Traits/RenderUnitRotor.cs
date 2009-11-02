﻿using System;
using System.Collections.Generic;
using System.Linq;
using IjwFramework.Types;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderUnitRotor : RenderUnit
	{
		public Animation rotorAnim;

		public RenderUnitRotor( Actor self )
			: base(self)
		{
			rotorAnim = new Animation(self.unitInfo.Name);
			rotorAnim.PlayRepeating("rotor");
		}

		public override IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			var mobile = self.traits.Get<Mobile>();

			yield return Util.Centered(anim.Image, self.CenterLocation);
			yield return Util.Centered(rotorAnim.Image, self.CenterLocation 
				+ Util.GetTurretPosition(self, self.unitInfo.PrimaryOffset, 0));
			if (self.unitInfo.SecondaryOffset != null)
				yield return Util.Centered(rotorAnim.Image, self.CenterLocation
					+ Util.GetTurretPosition(self, self.unitInfo.SecondaryOffset, 0));
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			rotorAnim.Tick();
		}
	}
}