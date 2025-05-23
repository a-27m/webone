﻿using System.Collections.Specialized;
using System.Web;

namespace WebOne
{
	/// <summary>
	/// HTML player for YouTube and similar sites
	/// </summary>
	class WebVideoPlayer
	{
		public InfoPage Page = new();

		public WebVideoPlayer(NameValueCollection Parameters)
		{
			Page.Title = "Retro online video player";
			Page.Header = "";
			Page.ShowFooter = false;
			Page.HtmlHeaders = "<style type='text/css'>html, body { border-style: none; } </style>";

			string VideoUrl = Program.ProcessUriMasks("/!webvideo/?");
			foreach (string Par in Parameters.AllKeys)
			{ if (Par != "type") VideoUrl += Par + "=" + HttpUtility.UrlEncode(Parameters[Par]) + "&"; }

			// Initial page & iframe status
			string SampleUrl = Parameters["url"] ?? "https://www.youtube.com/watch?v=XXXXXXX";
			string PreferPage = "intro";
			if (Parameters["gui"] == "1")
			{
				Parameters["type"] = null;
				if (Parameters["prefer"] != null) PreferPage = Parameters["prefer"] + "&url=" + SampleUrl;
			}

			if (!ConfigFile.WebVideoOptions.ContainsKey("Enable") || !Program.ToBoolean(ConfigFile.WebVideoOptions["Enable"] ?? "yes"))
			{
				Page.Content = "It's disabled.";
				Page.HttpHeaders.Add("Refresh", "0;url=/norovp.htm");
				return;
			}

			switch (Parameters["type"])
			{
				case "":
				case null:
					Page.Content = "<div style=\"background-color: black; color: white; padding: 3em;\"><p align=\"center\">";
					Page.Content += "<a href=\"/rovp.htm\"><img src=\"/rovp.gif\" alt=\"Click here to open Retro Online Video Player.\" border=\"0\"></a>";
					Page.Content += "<br><h1 align=\"center\">Retro Online Video Player</h1>";
					Page.Content += "</p></div>";
					Page.HttpHeaders.Add("Refresh", "5;url=/rovp.htm");
					break;
				case "intro":
					Page.Content = "<p align='center'><big>Use the toolbar above to watch a video.</big></p>";
					Page.Content += "<noscript><p align='center'>If you have troubles with playback, read help page (&quot;?&quot; at the top).</p></noscript>";
					Page.AddCss = false;
					Page.Title = "Video player - INTRO";
					break;
				case "embed":
					// [Embed] Universal - plugin
					string EmbHtml = "<embed id='MediaPlayer' " +
					"showcontrols='true' showpositioncontrols='true' showstatusbar='tue' showgotobar='true'" +
					"src='" + VideoUrl + "' autostart='true' style='width: 100%; height: 100%;' />";
					Page.Content = EmbHtml;
					Page.AddCss = false;
					Page.Title = "Video player - Universal";
					break;
				case "embedwm":
					// [WMP] Windows Media Player - plugin
					string WMP64html = "<embed id='MediaPlayer' type='application/x-mplayer2'" +
					"pluginspage='http://microsoft.com/windows/mediaplayer/en/download/'" +
					"showcontrols='true' showpositioncontrols='true' showstatusbar='tue' showgotobar='true'" +
					"src='" + VideoUrl + "' autostart='true' style='width: 100%; height: 100%;' />";
					Page.Content = WMP64html;
					Page.AddCss = false;
					Page.Title = "Video player - WMP";
					break;
				case "embedvlc":
					// [VLC] VLC Mediaplayer - plugin
					string VlcHtml = "<embed id='MediaPlayer' type='application/x-vlc-plugin'" +
					"codebase='http://download.videolan.org/pub/videolan/vlc/last/win32/axvlc.cab'" +
					"pluginspage='http://www.videolan.org'" +
					"showcontrols='true' showpositioncontrols='true' showstatusbar='tue' showgotobar='true' autoplay='yes'" +
					"src='" + VideoUrl + "' autostart='true' style='width: 100%; height: 100%;' />";
					Page.Content = VlcHtml;
					Page.AddCss = false;
					Page.Title = "Video player - VLC";
					break;
				case "objectns":
					// [NetShow] NetShow Player Control, Windows Media Player 5/6 Control - ActiveX
					// Docs: https://web.archive.org/web/20010124080600/http://msdn.microsoft.com/library/psdk/wm_media/wmplay/mmp_sdk/
					// Docs: https://web.archive.org/web/19990117015933/http://www.microsoft.com/devonly/tech/amov1doc/amsdk008.htm
					/*
					 * Download: http://www.microsoft.com/netshow/download/player.htm
					 * CODEBASE='http://www.microsoft.com/netshow/download/en/nsasfinf.cab#Version=2,0,0,912'  - NS 2.0
					 * CODEBASE='http://www.microsoft.com/netshow/download/en/nsmp2inf.cab#Version=5,1,51,415' - NS 3.0 aka WMP 5.2 Beta
					 * CODEBASE='http://activex.microsoft.com/activex/controls/mplayer/en/nsmp2inf.cab#Version=6,4,7,1112' - WMP 6.4 RTM
					 */
					string NSActiveXhtml = "<center><object ID='MediaPlayer' style='width: 100%; height: 100%;' " +
					"CLASSID='CLSID:2179C5D3-EBFF-11CF-B6FD-00AA00B4E220' " +
					"codebase='http://www.microsoft.com/netshow/download/en/nsasfinf.cab#Version=2,0,0,912'" +
					"standby='Loading Microsoft NetShow Player components...'>" +
					"<param name='FileName' value='" + VideoUrl + "'>" +
					"<param name='ControlType' value='2'>" +
					"<param name='EnableContextMenu' value='true'>" +
					"<param name='AnimationAtStart' value='true'>" +
					"<param name='TransparentAtStart' value='false'>" +
					"<param name='AutoStart' value='true'>" +
					"This mode is working only with Microsoft Internet Explorer 3 (or newer) and NetShow Player 2.0." +
					"</object></center>";
					Page.Content = NSActiveXhtml;
					Page.AddCss = false;
					Page.Title = "Video player - NetShow ActiveX";
					break;
				case "objectwm":
					// [WinMedia] Windows Media Player 7.0 Control - ActiveX
					// Download: http://microsoft.com/windows/mediaplayer/en/download/
					// Docs: https://learn.microsoft.com/en-us/previous-versions/windows/desktop/wmp/detailed-object-model-comparison
					string WMPActiveXhtml = "<center><object ID='MediaPlayer' style='width: 100%; height: 100%;' " +
					"CLASSID='CLSID:6BF52A52-394A-11d3-B153-00C04F79FAA6' " +
					"codebase='http://activex.microsoft.com/activex/controls/mplayer/en/nsmp2inf.cab#Version=6,4,7,1112'" +
					"standby='Loading Microsoft Windows Media Player components...'>" +
					"<param name='URL' value='" + VideoUrl + "'>" +
					"<param name='ShowControls' value='true'>" +
					"<param name='ShowDisplay' value='true'>" +
					"<param name='ShowStatusBar' value='true'>" +
					"<param name='ShowPositionControls' value='true'>" +
					"<param name='ShowGoToBar' value='true'>" +
					"<param name='Controls' value='true'>" +
					"<param name='AutoSize' value='true'>" +
					"<param name='AutoStart' value='true'>" +
					"Try another player type, as Windows Media Player Control 7.0 (or newer) is unavailable." +
					"</object></center>";
					Page.Content = WMPActiveXhtml;
					Page.AddCss = false;
					Page.Title = "Video player - WMP ActiveX";
					break;
				case "html5":
					// HTML5 VIDEO tag
					Page.Content = "<center><video id='MediaPlayer' src='" + VideoUrl + "' controls='yes' autoplay='yes' style='width: 100%; height: 100%;'>"
					+ "Try another player type, as HTML5 is not supported.</video></center>";
					Page.AddCss = false;
					//idea: made multi-source code with hard-coded containers (ogg, webm, etc)
					Page.Title = "Video player - HTML5";
					break;
				case "dynimg":
					// Dynamic Image - IE only 
					// (http://www.jmcgowan.com/aviweb.html)
					// also: https://web.archive.org/web/19990117015933/http://www.microsoft.com/devonly/tech/amov1doc/amsdk008.htm
					// optimized for MPEG format & codec
					string PlaceholderUrl = "/nodynsrc.gif";
					Page.Content = "<center><IMG DYNSRC='" + VideoUrl + "' CONTROLS START='FILEOPEN' SRC='" + PlaceholderUrl + "'></center>";
					Page.AddCss = false;
					Page.Title = "Video player - Dynamic Image";
					break;
				case "link":
					// Link only
					Page.Content = "<center><big><a href='" + VideoUrl + "'>Download the video</a></big></center>";
					if (Parameters["f"] == null) Page.Content += "<center><p>Or select format, codecs and convert the video online.</p></center>";
					Page.AddCss = false;
					Page.Title = "Video player - link only";
					break;
				case "file":
					// Redirect to file only
					Page.Content = "<center>Please wait up to 30 sec.<br>If nothing appear, <a href='" + VideoUrl + "'>click here</a> to download manually.</center>";
					Page.HttpHeaders.Add("Refresh", "0; url=" + VideoUrl);
					Page.AddCss = false;
					Page.Title = "Video player - FILE REDIRECT";
					break;
				default:
					Page.Content = "Unknown player type.";
					Page.AddCss = false;
					Page.Title = "Video player - ERROR";
					break;
			}
		}
	}
}
