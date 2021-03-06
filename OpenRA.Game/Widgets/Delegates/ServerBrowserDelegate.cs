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
using System.Drawing;
using System.Linq;
using OpenRA.Server;

namespace OpenRA.Widgets.Delegates
{
	public class ServerBrowserDelegate : WidgetDelegate
	{
		static GameServer[] GameList;
		static List<Widget> GameButtons = new List<Widget>();

		public override bool OnMouseUp(Widget w, MouseInput mi)
		{
			// Main Menu root
			if (w.Id == "MAINMENU_BUTTON_JOIN")
			{
				var bg = Game.chrome.rootWidget.ShowMenu("JOINSERVER_BG");

				int height = 50;
				int width = 300;
				int i = 0;
				GameList = MasterServerQuery.GetGameList(Game.Settings.MasterServer).ToArray();

				bg.Children.RemoveAll(a => GameButtons.Contains(a));
				GameButtons.Clear();

				foreach (var game in GameList)
				{
					ButtonWidget b = new ButtonWidget();
					b.Bounds = new Rectangle(bg.Bounds.X + 20, bg.Bounds.Y + height, width, 25);
					b.GetType().GetField("Id").SetValue(b, "JOIN_GAME_{0}".F(i));
					b.GetType().GetField("Text").SetValue(b, "{0} ({1})".F(game.Name, game.Address));
					b.GetType().GetField("Delegate").SetValue(b, "ServerBrowserDelegate");

					bg.AddChild(b);
					GameButtons.Add(b);

					height += 35;
				}

				return true;
			}

			if (w.Id == "JOINSERVER_BUTTON_DIRECTCONNECT")
			{
				Game.chrome.rootWidget.GetWidget("JOINSERVER_BG").Visible = false;
				Game.JoinServer(Game.Settings.NetworkHost, Game.Settings.NetworkPort);
				return true;
			}

			if (w.Id.Substring(0, 10) == "JOIN_GAME_")
			{
				Game.chrome.rootWidget.GetWidget("JOINSERVER_BG").Visible = false;
				int index = int.Parse(w.Id.Substring(10));
				var game = GameList[index];
				Game.JoinServer(game.Address.Split(':')[0], int.Parse(game.Address.Split(':')[1]));
				return true;
			}

			if (w.Id == "JOINSERVER_BUTTON_CANCEL")
			{
				Game.chrome.rootWidget.ShowMenu("MAINMENU_BG");
				return true;
			}

			return false;
		}
	}	
}
