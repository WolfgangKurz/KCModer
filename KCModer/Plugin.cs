using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.IO;
using System.Threading;
using System.Reflection;

using Nekoxy;
using System.Reactive.Linq;

using Grabacr07.KanColleViewer.Composition;
using Grabacr07.KanColleWrapper.Models.Raw;
using Grabacr07.KanColleWrapper;

namespace KCModer
{
	[Export(typeof(IPlugin))]
	[ExportMetadata("Guid", "A2389776-A3F7-41EF-9AEB-F1D5239F912C")]
	[ExportMetadata("Title", "KCModer")]
	[ExportMetadata("Description", "Help moding KanColle assets easily")]
	[ExportMetadata("Version", "0.1.0.0")]
	[ExportMetadata("Author", "WolfgangKurz")] // wolfgangkurzdev@gmail.com
	[ExportMetadata("AuthorURL", "http://swaytwig.com/")]
	public class Plugin : IPlugin
	{
		private class AliasInfo
		{
			// {Directory}/{Alias} => {Directory}/{Source}
			public string Directory { get; set; }
			public string Alias { get; set; }
			public string Source { get; set; }
		}

		private string BaseDir { get; }
			= Path.Combine(
				Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
				"KCModer"
			);

		private AliasInfo[] LoadAlias()
		{
			var output = new List<AliasInfo>();

			var aliasFile = Path.Combine(BaseDir, "profile.txt");
			if (!File.Exists(aliasFile))
				return new AliasInfo[0];

			var lines = File.ReadAllLines(aliasFile).Where(x => x.Length > 0);
			var Directory = "";
			foreach (var line in lines)
			{
				if (line.Contains("->"))
				{
					output.Add(new AliasInfo
					{
						Directory = Directory,
						Alias = line.Substring(0, line.IndexOf("->")).Trim(),
						Source = line.Substring(line.IndexOf("->") + 2).Trim()
					});
				}
				else
					Directory = line;
			}
			return output.ToArray();
		}

		public void Initialize()
		{
			if (!typeof(Session).GetProperties().Any(x => x.Name == nameof(Session.KCModerPatched)))
				throw new Exception("Cannot initialize KCModer - Not prepared before use this plugin. Please run KCModerPatch.exe first.");

			var aliasList = LoadAlias();

			/*
			TransparentProxyLogic.BeforeResponse += (session, data) =>
			{
				var origin = session.Request.PathAndQuery;
				if (origin.Contains("?")) origin = origin.Substring(0, origin.IndexOf("?"));

				origin = Path.Combine(BaseDir, origin.Replace('/', Path.DirectorySeparatorChar));
				if (File.Exists(origin))
				{
					File.AppendAllText("kcmoder.txt", "Patched " + origin + Environment.NewLine);
					return File.ReadAllBytes(origin);
				}

				return data; // Encoding.UTF8.GetBytes(sv_data);
			};
			*/
		}
	}
}
