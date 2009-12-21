﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Traits.Activities;

namespace OpenRa.Game.Traits
{
	class Plane : IOrder, IMovement
	{
		public Plane(Actor self)
		{
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;
			if (underCursor == null)
				return Order.Move(self, xy);
			if (underCursor.Info == Rules.UnitInfo["AFLD"] 
				&& underCursor.Owner == self.Owner)
				return Order.Enter(self, underCursor);
			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				self.CancelActivity();
				self.QueueActivity(new Circle(order.TargetLocation));
			}

			if (order.OrderString == "Enter")
			{
				self.CancelActivity();
				self.QueueActivity(new ReturnToBase(self, order.TargetActor.CenterLocation));
			}
		}

		public UnitMovementType GetMovementType()
		{
			return UnitMovementType.Fly;
		}

		public bool CanEnterCell(int2 location)
		{
			return true; // Planes can go anywhere (?)
		}
	}
}