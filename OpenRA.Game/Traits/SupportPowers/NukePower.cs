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

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	class NukePowerInfo : SupportPowerInfo
	{
		public override object Create(Actor self) { return new NukePower(self, this); }
	}
	
	class NukePower : SupportPower, IResolveOrder
	{
		public NukePower(Actor self, NukePowerInfo info) : base(self, info) { }

		protected override void OnBeginCharging() { Sound.PlayToPlayer(Owner, "aprep1.aud"); }
		protected override void OnFinishCharging() { Sound.PlayToPlayer(Owner, "aready1.aud"); }
		protected override void OnActivate()
		{
			Game.controller.orderGenerator = new SelectTarget();
			Sound.Play("slcttgt1.aud");
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "NuclearMissile")
			{
				var silo = self.World.Queries.OwnedBy[self.Owner]
					.Where(a => a.traits.Contains<NukeSilo>())
					.FirstOrDefault();
				if (silo != null)
					silo.traits.Get<RenderBuilding>().PlayCustomAnim(silo, "active");
				
				Owner.World.AddFrameEndTask(w =>
				{
					// Play to everyone but the current player
					if (Owner != Owner.World.LocalPlayer)
						Sound.Play("alaunch1.aud");

					// TODO: FIRE ZE MISSILES
					//w.Add(new NukeLaunch(silo));
				});
				
				Game.controller.CancelInputMode();
				FinishActivate();
			}
		}

		class SelectTarget : IOrderGenerator
		{
			public SelectTarget() {	}

			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Right)
					Game.controller.CancelInputMode();

				return OrderInner(world, xy, mi);
			}

			IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
			{
				if (mi.Button == MouseButton.Left)
					yield return new Order("NuclearMissile", world.LocalPlayer.PlayerActor, xy);

				yield break;
			}

			public void Tick(World world)
			{
				var hasStructure = world.Queries.OwnedBy[world.LocalPlayer]
					.WithTrait<NukeSilo>()
					.Any();

				if (!hasStructure)
					Game.controller.CancelInputMode();
			}

			public void Render(World world) { }
			public string GetCursor(World world, int2 xy, MouseInput mi) { return "nuke"; }
		}
	}

	// tag trait for the building
	class NukeSiloInfo : StatelessTraitInfo<NukeSilo> { }
	class NukeSilo { }
}