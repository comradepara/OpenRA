﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Graphics;

namespace OpenRa.Traits
{
	class WithShadowInfo : StatelessTraitInfo<WithShadow> {}

	class WithShadow : IRenderModifier
	{
		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			var unit = self.traits.Get<Unit>();

			var shadowSprites = r.Select(a => a.WithPalette("shadow"));
			var flyingSprites = (unit.Altitude <= 0) ? r 
				: r.Select(a => a.WithPos(a.Pos - new float2(0, unit.Altitude)).WithZOffset(3));

			return shadowSprites.Concat(flyingSprites);
		}
	}
}