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

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class SonarPulsePowerInfo : SupportPowerInfo
	{
		public override object Create(Actor self) { return new SonarPulsePower(self, this); }
	}

	public class SonarPulsePower : SupportPower, IResolveOrder
	{
		public SonarPulsePower(Actor self, SonarPulsePowerInfo info) : base(self, info) { }

		protected override void OnBeginCharging() { }
		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, "pulse1.aud"); }

		protected override void OnActivate()
		{
			Game.IssueOrder(new Order("SonarPulse", Owner.PlayerActor));
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SonarPulse")
			{
				// TODO: Reveal submarines

				// Should this play for all players?
				Sound.Play("sonpulse.aud");
				FinishActivate();
			}
		}
	}
}
