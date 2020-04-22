﻿using UnityEngine;
using Verse;

namespace Puppeteer
{
	public class Settings
	{
		public int graphicalMapSize = 3;
	}

	public static class SettingsDrawer
	{
		public static string currentHelpItem = null;
		public static Vector2 scrollPosition = Vector2.zero;
		public static void DoWindowContents(ref Settings settings, Rect inRect)
		{
			inRect.yMin += 15f;
			inRect.yMax -= 15f;

			var firstColumnWidth = (inRect.width - Listing.ColumnSpacing) * 3.5f / 5f;
			var secondColumnWidth = inRect.width - Listing.ColumnSpacing - firstColumnWidth;

			var outerRect = new Rect(inRect.x, inRect.y, firstColumnWidth, inRect.height);
			var innerRect = new Rect(0f, 0f, firstColumnWidth - 24f, inRect.height);
			Widgets.BeginScrollView(outerRect, ref scrollPosition, innerRect, true);

			currentHelpItem = null;

			var list = new Listing_Standard();
			list.Begin(innerRect);

			{
				// About
				var intro = "Puppeteer";
				var textHeight = Text.CalcHeight(intro, list.ColumnWidth - 3f - Dialogs.inset) + 2 * 3f;
				Widgets.Label(list.GetRect(textHeight).Rounded(), intro);
				list.Gap(10f);

				var tmp = (settings.graphicalMapSize - 1) / 2;
				list.Dialog_IntSlider("GraphicalMapSize", n => $"{1 + 2 * n}x{1 + 2 * n}", ref tmp, 1, 5);
				settings.graphicalMapSize = 1 + 2 * tmp;
			}

			list.End();
			Widgets.EndScrollView();

			if (currentHelpItem != null)
			{
				outerRect.x += firstColumnWidth + Listing.ColumnSpacing;
				outerRect.width = secondColumnWidth;

				list = new Listing_Standard();
				list.Begin(outerRect);

				var title = currentHelpItem.SafeTranslate().Replace(": {0}", "");
				list.Dialog_Label(title, false);

				list.Gap(8f);

				var text = (currentHelpItem + "_Help").SafeTranslate();
				var anchor = Text.Anchor;
				Text.Anchor = TextAnchor.MiddleLeft;
				var textHeight = Text.CalcHeight(text, list.ColumnWidth - 3f - Dialogs.inset) + 2 * 3f;
				var rect = list.GetRect(textHeight).Rounded();
				GUI.color = Color.white;
				Widgets.Label(rect, text);
				Text.Anchor = anchor;

				list.End();
			}
		}
	}
}