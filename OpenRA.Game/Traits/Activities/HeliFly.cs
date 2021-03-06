﻿#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;

namespace OpenRA.Traits.Activities
{
	class HeliFly : IActivity
	{
		const int CruiseAltitude = 20;
		readonly float2 Dest;
		public HeliFly(float2 dest)
		{
			Dest = dest;
		}

		public IActivity NextActivity { get; set; }
		bool isCanceled;
		
		public IActivity Tick(Actor self)
		{
			if (isCanceled)
				return NextActivity;

			var unit = self.traits.Get<Unit>();

			if (unit.Altitude != CruiseAltitude)
			{
				unit.Altitude += Math.Sign(CruiseAltitude - unit.Altitude);
				return this;
			}

			var dist = Dest - self.CenterLocation;
			if (float2.WithinEpsilon(float2.Zero, dist, 2))
			{
				self.CenterLocation = Dest;
				self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();
				return NextActivity;
			}

			var desiredFacing = Util.GetFacing(dist, unit.Facing);
			Util.TickFacing(ref unit.Facing, desiredFacing, 
				self.Info.Traits.Get<UnitInfo>().ROT);

			var rawSpeed = .2f * Util.GetEffectiveSpeed(self);
			self.CenterLocation += (rawSpeed / dist.Length) * dist;
			self.Location = ((1 / 24f) * self.CenterLocation).ToInt2();

			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
