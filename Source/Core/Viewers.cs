﻿using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace Puppeteer
{
	public static class Viewers
	{
		public static void Join(Connection connection, ViewerID vID)
		{
			if (vID.IsValid)
			{
				Tools.LogWarning($"{vID.name} joined");
				State.instance.SetConnected(vID, true);
				State.instance.Save();
				SendAllState(connection, vID);
			}
		}

		public static void Leave(ViewerID vID)
		{
			if (vID.IsValid)
			{
				Tools.LogWarning($"{vID.name} left");
				State.instance.SetConnected(vID, false);
				State.instance.Save();
			}
		}

		/*public static IEnumerable<ViewerID> Available()
		{
			return State.instance.AvailableViewers();
		}*/

		static void SendGameInfo(Connection connection, ViewerID vID)
		{
			connection.Send(new GameInfo() { viewer = vID, info = new GameInfo.Info() { terrain = GridUpdater.ColorList() } });
		}

		public static void SendEarnToAll(Connection connection, int amount)
		{
			var puppeteers = State.instance.ConnectedPuppeteers();
			puppeteers.Do(puppeteer =>
			{
				puppeteer.coinsEarned += amount;
				SendEarned(connection, puppeteer);
			});
		}

		static void SendEarned(Connection connection, State.Puppeteer puppeteer)
		{
			connection.Send(new Earned() { viewer = puppeteer.vID, info = new Earned.Info() { amount = puppeteer.coinsEarned } });
		}

		public static void SendPortrait(Connection connection, State.Puppeteer puppeteer)
		{
			var vID = puppeteer?.vID;
			var pawn = puppeteer?.puppet?.pawn;
			if (vID != null && pawn != null)
				OperationQueue.Add(OperationType.Portrait, () =>
				{
					var portrait = Renderer.GetPawnPortrait(pawn, new Vector2(35f, 55f));
					connection.Send(new Portrait() { viewer = vID, info = new Portrait.Info() { image = portrait } });
				});
		}

		static void SendStates<T>(Connection connection, string key, Func<Pawn, T> valueFunction, State.Puppeteer forPuppeteer = null)
		{
			void SendState(State.Puppeteer puppeteer)
			{
				var vID = puppeteer?.vID;
				var pawn = puppeteer?.puppet?.pawn;

				if (pawn != null)
					connection.Send(new OutgoingState<T>() { viewer = vID, key = key, val = valueFunction(pawn) });
			}

			if (forPuppeteer != null)
			{
				SendState(forPuppeteer);
				return;
			}
			var puppeteers = State.instance.ConnectedPuppeteers();
			puppeteers.Do(p => SendState(p));
		}

		public static void SendAreas(Connection connection, State.Puppeteer forPuppeteer = null)
		{
			string[] GetResult(Pawn pawn)
			{
				return pawn.Map.areaManager.AllAreas
					.Where(a => a.AssignableAsAllowed())
					.Select(a => a.Label).ToArray();
			}
			SendStates(connection, "zones", GetResult, forPuppeteer);
		}

		public static void SendPriorities(Connection connection)
		{
			PrioritiyInfo GetResult(Pawn pawn)
			{
				int[] GetValues(Pawn p)
				{
					return Integrations.GetWorkTypeDefs().Select(def =>
					{
						var priority = p.workSettings.GetPriority(def);
						var passion = (int)p.skills.MaxPassionOfRelevantSkillsFor(def);
						var disabled = def.relevantSkills.Any(skill => p.skills.GetSkill(skill).TotallyDisabled);
						return disabled ? -1 : passion * 100 + priority;
					})
					.ToArray();
				}

				var columns = Integrations.GetWorkTypeDefs().Select(def => def.labelShort).ToArray();
				var rows = Tools.AllColonists(false)
					.Select(colonist => new PrioritiyInfo.Priorities() { pawn = colonist.LabelShortCap, yours = colonist == pawn, val = GetValues(colonist) })
					.ToArray();
				return new PrioritiyInfo()
				{
					columns = columns,
					manual = Current.Game.playSettings.useWorkPriorities,
					norm = Integrations.defaultPriority,
					max = Integrations.maxPriority,
					rows = rows
				};
			}
			SendStates(connection, "priorities", GetResult, null);
		}

		public static void SendSchedules(Connection connection)
		{
			ScheduleInfo GetResult(Pawn pawn)
			{
				string GetValues(Pawn p)
				{
					var schedules = Enumerable.Range(0, 24).Select(hour => p.timetable.GetAssignment(hour)).ToArray();
					return schedules.Join(s => Tools.Assignments[s], "");
				}
				var rows = Tools.AllColonists(false)
					.Select(colonist => new ScheduleInfo.Schedules() { pawn = colonist.LabelShortCap, yours = colonist == pawn, val = GetValues(colonist) })
					.ToArray();
				return new ScheduleInfo() { rows = rows };
			}
			SendStates(connection, "schedules", GetResult, null);
		}

		public static void SendAllState(Connection connection, ViewerID vID)
		{
			var puppeteer = State.instance.PuppeteerForViewer(vID);

			SendGameInfo(connection, vID);
			SendEarned(connection, puppeteer);
			SendPortrait(connection, puppeteer);
			SendAreas(connection, puppeteer);
			SendPriorities(connection);
			SendSchedules(connection);
		}
	}
}