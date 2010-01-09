﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.SupportPowers
{
	interface ISupportPowerImpl
	{
		void Activate(SupportPower p);
		void OnFireNotification(Actor target, int2 xy);
		void IsChargingNotification(SupportPower p);
		void IsReadyNotification(SupportPower p);
	}
}