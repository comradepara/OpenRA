#region Copyright & License Information
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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Support;
using OpenRA.Traits;
using Timer = OpenRA.Support.Timer;

namespace OpenRA
{
	public static class Game
	{
		public static readonly int CellSize = 24;

		public static World world;
		internal static Viewport viewport;
		public static Controller controller;
		internal static Chrome chrome;
		public static UserSettings Settings;
		
		internal static OrderManager orderManager;

		public static bool skipMakeAnims = true;

		internal static Renderer renderer;
		static int2 clientSize;
		static string mapName;
		internal static Session LobbyInfo = new Session();
		static bool changePending;
		public static Pair<Assembly, string>[] ModAssemblies;

		static void LoadModPackages(Manifest manifest)
		{
			FileSystem.UnmountAll();
			Timer.Time("reset: {0}");

			foreach (var dir in manifest.Folders) FileSystem.Mount(dir);
			foreach (var pkg in manifest.Packages) FileSystem.Mount(pkg);
				
			Timer.Time("mount temporary packages: {0}");
		}
		
		static void LoadModAssemblies(Manifest m)
		{	
			// All the core namespaces
			var asms = typeof(Game).Assembly.GetNamespaces()
				.Select(c => Pair.New(typeof(Game).Assembly, c))
				.ToList();

			// Mod assemblies assumed to contain a single namespace
			foreach (var a in m.Assemblies)
			{
				var failures = new List<string>();
				var fullpath = Path.GetFullPath(a);

				var asm = Assembly.LoadFile(fullpath);
				asms.AddRange(asm.GetNamespaces().Select(ns => Pair.New(asm, ns)));
			}

			ModAssemblies = asms.ToArray();
		}

		public static T CreateObject<T>(string classname)
		{
			foreach (var mod in ModAssemblies)
			{
				var fullTypeName = mod.Second + "." + classname;
				var obj = mod.First.CreateInstance(fullTypeName);
				if (obj != null)
					return (T)obj;
			}

			throw new InvalidOperationException("Cannot locate type: {0}".F(classname));
		}
		
		public static void ChangeMap(string mapName)
		{
			Timer.Time( "----ChangeMap" );

			var manifest = new Manifest(LobbyInfo.GlobalSettings.Mods);
			Timer.Time( "manifest: {0}" );
			Game.LoadModAssemblies(manifest);
			Game.changePending = false;
			Game.mapName = mapName;
			SheetBuilder.Initialize(renderer);
			
			LoadModPackages(manifest);
			
			Rules.LoadRules(mapName, manifest);
			Timer.Time( "load rules: {0}" );

			world = null;	// trying to access the old world will NRE, rather than silently doing it wrong.

			ChromeProvider.Initialize(manifest.Chrome);

			world = new World();
						
			Timer.Time( "world: {0}" );
			
			SequenceProvider.Initialize(manifest.Sequences);
			viewport = new Viewport(clientSize, Game.world.Map.Offset, Game.world.Map.Offset + Game.world.Map.Size, renderer);
			Timer.Time( "ChromeProv, SeqProv, viewport: {0}" );

			chrome = new Chrome(renderer, manifest);
			Timer.Time( "chrome: {0}" );

			Timer.Time( "----end ChangeMap" );
			Debug("Map change {0} -> {1}".F(Game.mapName, mapName));
		}	

		internal static void Initialize(string mapName, Renderer renderer, int2 clientSize, int localPlayer, Controller controller)
		{
			Game.renderer = renderer;
			Game.clientSize = clientSize;
			
			Sound.Initialize();
			PerfHistory.items["render"].hasNormalTick = false;
			PerfHistory.items["batches"].hasNormalTick = false;
			PerfHistory.items["text"].hasNormalTick = false;
			PerfHistory.items["cursor"].hasNormalTick = false;
			Game.controller = controller;
			
			ChangeMap(mapName);

			if( Settings.Replay != "" )
				orderManager = new OrderManager( new ReplayConnection( Settings.Replay ) );
			else
				JoinLocal();
		}
		
		public static string CurrentHost = "";
		public static int CurrentPort = 0;
		internal static void JoinServer(string host, int port)
		{
			CurrentHost = host;
			CurrentPort = port;
			orderManager = new OrderManager(new NetworkConnection( host, port )); // TODO: supplying a replay file prevents osx from joining a game
		}
		
		internal static void JoinLocal()
		{
			orderManager = new OrderManager(new EchoConnection());
		}
				
		static int lastTime = Environment.TickCount;

		public static void ResetTimer()
		{
			lastTime = Environment.TickCount;
		}

		public static int RenderFrame = 0;

		internal static Chat chat = new Chat();

		public static int LocalTick = 0;
		const int NetTickScale = 3;		// 120ms net tick for 40ms local tick

		public static void Tick()
		{
			if (changePending && PackageDownloader.IsIdle())
			{
				ChangeMap(LobbyInfo.GlobalSettings.Map);
				return;
			}

			int t = Environment.TickCount;
			int dt = t - lastTime;
			if (dt >= Settings.Timestep)
			{
				using (new PerfSample("tick_time"))
				{
					lastTime += Settings.Timestep;
					chrome.Tick( world );

					orderManager.TickImmediate( world );

					var isNetTick = LocalTick % NetTickScale == 0;

					if (!isNetTick || orderManager.IsReadyForNextFrame)
					{
						++LocalTick;

						if (isNetTick) orderManager.Tick(world);

						controller.orderGenerator.Tick(world);
						controller.selection.Tick(world);

						world.Tick();

						PerfHistory.Tick();
					}
					else
						if (orderManager.FrameNumber == 0)
							lastTime = Environment.TickCount;
				}
			}

			using (new PerfSample("render"))
			{
				++RenderFrame;
				viewport.DrawRegions( world );
			}

			PerfHistory.items["render"].Tick();
			PerfHistory.items["batches"].Tick();
			PerfHistory.items["text"].Tick();
			PerfHistory.items["cursor"].Tick();
		}

		public static void SyncLobbyInfo(string data)
		{
			var oldLobbyInfo = LobbyInfo;

			var session = new Session();
			session.GlobalSettings.Mods = Settings.InitialMods;

			var ys = MiniYaml.FromString(data);
			foreach (var y in ys)
			{
				if (y.Key == "GlobalSettings")
				{
					FieldLoader.Load(session.GlobalSettings, y.Value);
					continue;
				}

				int index;
				if (!int.TryParse(y.Key, out index))
					continue;	// not a player.

				var client = new Session.Client();
				FieldLoader.Load(client, y.Value);
				session.Clients.Add(client);
			}

			LobbyInfo = session;

			if (!IsStarted)
				world.SharedRandom = new OpenRA.Thirdparty.Random(LobbyInfo.GlobalSettings.RandomSeed);

			if (Game.orderManager.Connection.ConnectionState == ConnectionState.Connected)
				world.SetLocalPlayer(Game.orderManager.Connection.LocalClientId);

			if (Game.orderManager.FramesAhead != LobbyInfo.GlobalSettings.OrderLatency
				&& !Game.orderManager.GameStarted)
			{
				Game.orderManager.FramesAhead = LobbyInfo.GlobalSettings.OrderLatency;
				Debug("Order lag is now {0} frames.".F(LobbyInfo.GlobalSettings.OrderLatency));
			}

			if (PackageDownloader.SetPackageList(LobbyInfo.GlobalSettings.Packages)
				|| mapName != LobbyInfo.GlobalSettings.Map)
				changePending = true;

			if (string.Join(",", oldLobbyInfo.GlobalSettings.Mods)
				!= string.Join(",", LobbyInfo.GlobalSettings.Mods))
			{
				Debug("Mods list changed, reloading: {0}".F(string.Join(",", LobbyInfo.GlobalSettings.Mods)));
				changePending = true;
			}
		}

		public static void IssueOrder(Order o) { orderManager.IssueOrder(o); }	/* avoid exposing the OM to mod code */

		public static bool IsStarted { get { return orderManager.GameStarted; } }

		public static void StartGame()
		{
			if( orderManager.GameStarted ) return;
			chat.Reset();

			foreach (var c in LobbyInfo.Clients)
				world.AddPlayer(new Player(world, c));

			foreach (var p in world.players.Values)
				foreach (var q in world.players.Values)
					p.Stances[q] = ChooseInitialStance(p, q);
			
			world.Queries = new World.AllQueries(world);

			foreach (var gs in world.WorldActor.traits.WithInterface<IGameStarted>())
				gs.GameStarted(world);

			Game.viewport.GoToStartLocation( world.LocalPlayer );
			orderManager.StartGame();
		}

		static Stance ChooseInitialStance(Player p, Player q)
		{
			if (p == q) return Stance.Ally;
			if (p == world.NeutralPlayer || q == world.NeutralPlayer) return Stance.Neutral;

			var pc = GetClientForPlayer(p);
			var qc = GetClientForPlayer(q);

			return pc.Team != 0 && pc.Team == qc.Team 
				? Stance.Ally : Stance.Enemy;
		}

		static Session.Client GetClientForPlayer(Player p)
		{
			return LobbyInfo.Clients.Single(c => c.Index == p.Index);
		}

		static int2 lastPos;
		public static void DispatchMouseInput(MouseInputEvent ev, MouseEventArgs e, Modifiers modifierKeys)
		{
			int sync = Game.world.SyncHash();

			if (ev == MouseInputEvent.Down)
				lastPos = new int2(e.Location);

			if (ev == MouseInputEvent.Move && (e.Button == MouseButtons.Middle || e.Button == (MouseButtons.Left | MouseButtons.Right)))
			{
				var p = new int2(e.Location);
				viewport.Scroll(lastPos - p);
				lastPos = p;
			}

			Game.viewport.DispatchMouseInput( world, 
				new MouseInput
				{
					Button = (MouseButton)(int)e.Button,
					Event = ev,
					Location = new int2(e.Location),
					Modifiers = modifierKeys,
				});

			if( sync != Game.world.SyncHash() )
				throw new InvalidOperationException( "Desync in DispatchMouseInput" );
		}

		public static bool IsHost
		{
			get { return orderManager.Connection.LocalClientId == 0; }
		}

		public static Session.Client LocalClient
		{
			get { return LobbyInfo.Clients.FirstOrDefault(c => c.Index == orderManager.Connection.LocalClientId); }
		}

		static Dictionary<char, char> RemapKeys = new Dictionary<char, char>
		{
			{ '!', '1' },
			{ '@', '2' },
			{ '#', '3' },
			{ '$', '4' },
			{ '%', '5' },
			{ '^', '6' },
			{ '&', '7' },
			{ '*', '8' },
			{ '(', '9' },
			{ ')', '0' },
		};

		public static void HandleKeyPress( KeyPressEventArgs e, Modifiers modifiers )
		{
			int sync = Game.world.SyncHash();
			
			if( e.KeyChar == '\r' )
				Game.chat.Toggle();
			else if (Game.chat.isChatting)
				Game.chat.TypeChar(e.KeyChar);
			else
			{
				var c = RemapKeys.ContainsKey(e.KeyChar) ? RemapKeys[e.KeyChar] : e.KeyChar;

				if (c >= '0' && c <= '9')
					Game.controller.selection.DoControlGroup(world,
						c - '0', modifiers);
			}

			if( sync != Game.world.SyncHash() )
				throw new InvalidOperationException( "Desync in OnKeyPress" );
		}

		public static void HandleModifierKeys(Modifiers mods)
		{
			controller.SetModifiers(mods);
		}

		static Size GetResolution(Settings settings)
		{
			var desktopResolution = Screen.PrimaryScreen.Bounds.Size;
			if (Game.Settings.Width > 0 && Game.Settings.Height > 0)
			{
				desktopResolution.Width = Game.Settings.Width;
				desktopResolution.Height = Game.Settings.Height;
			}
			return new Size(
				desktopResolution.Width,
				desktopResolution.Height);
		}

		public static void PreInit(Settings settings)
		{
			AppDomain.CurrentDomain.AssemblyResolve += FileSystem.ResolveAssembly;
			while (!Directory.Exists("mods"))
			{
				var current = Directory.GetCurrentDirectory();
				if (Directory.GetDirectoryRoot(current) == current)
					throw new InvalidOperationException("Unable to load MIX files.");
				Directory.SetCurrentDirectory("..");
			}
			
			LoadUserSettings(settings);
			Game.LobbyInfo.GlobalSettings.Mods = Game.Settings.InitialMods;
			
			// Load the default mod to access required files
			Game.LoadModPackages(new Manifest(Game.LobbyInfo.GlobalSettings.Mods));
			
			Renderer.SheetSize = Game.Settings.SheetSize;
			
			bool windowed = !Game.Settings.Fullscreen;
			var resolution = GetResolution(settings);
			renderer = new Renderer(resolution, windowed);
			resolution = renderer.Resolution;

			var controller = new Controller();

			Game.Initialize(Game.Settings.Map, renderer, new int2(resolution), Game.Settings.Player, controller);

			Game.ResetTimer();
		}

		static void LoadUserSettings(Settings settings)
		{
			Game.Settings = new UserSettings();
			var settingsFile = settings.GetValue("settings", "settings.ini");
			FileSystem.Mount("./");
			if (FileSystem.Exists(settingsFile))
				FieldLoader.Load(Game.Settings,
					new IniFile(FileSystem.Open(settingsFile)).GetSection("Settings"));
			FileSystem.UnmountAll();
		}

		static bool quit;
		internal static void Run()
		{
			while (!quit)
			{
				Game.Tick();
				Application.DoEvents();
			}
		}

		public static void Exit() { quit = true; }

		public static void Debug(string s) { chat.AddLine(Color.White, "Debug", s); }
	}
}
