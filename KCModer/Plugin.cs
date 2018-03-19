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
			// {Directory}/{Alias} -> {Directory}/{Source}
			public string Directory { get; set; }
			public string Alias { get; set; }
			public string Source { get; set; }

			public string FullSource
				=> Path.Combine(this.Directory, this.Source);

			public string FullAlias
				=> Path.Combine(
					this.Directory.Length > 0 && this.Directory[0] == Path.DirectorySeparatorChar
						? this.Directory.Substring(1)
						: this.Directory,
					this.Alias
				);

			public override string ToString()
				=> $"{{Directory={this.Directory}, Alias={this.Alias}, Source={this.Source}}}";
		}

		private string BaseDir
			=> Path.Combine(
				Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
				"KCModer"
			);

		private AliasInfo[] LoadAlias()
		{
			var output = new List<AliasInfo>();

			var aliasFile = Path.Combine(BaseDir, "alias.txt");
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
					Directory = line.Replace('/', Path.DirectorySeparatorChar);
			}
			return output.ToArray();
		}

		public void Initialize()
		{
			if (!typeof(Session).GetProperties().Any(x => x.Name == nameof(Session.KCModerPatched)))
				throw new Exception("Cannot initialize KCModer - Not prepared before use this plugin. Please run KCModerPatch.exe first.");

			var aliasList = LoadAlias();

			File.WriteAllText(
				Path.Combine(BaseDir, "kcmoder.txt"),
				"BaseDir: " + BaseDir + Environment.NewLine
				+ "Alias loaded (" + aliasList.Count() + ")" + Environment.NewLine
			);

			TransparentProxyLogic.BeforeResponse += (session, data) =>
			{
				var origin = session.Request.PathAndQuery;
				if (!origin.StartsWith("/kcs/")) return data; // pass

				origin = origin.Substring(4); // skip "/kcs"
				if (origin.Contains("?"))
					origin = origin.Substring(0, origin.IndexOf("?"));

				File.AppendAllText(
					Path.Combine(BaseDir, "kcmoder.txt"),
					Environment.NewLine + "Requested " + origin + Environment.NewLine
				);

				origin = Path.Combine(BaseDir, origin.Replace('/', Path.DirectorySeparatorChar));
				File.AppendAllText(
					Path.Combine(BaseDir, "kcmoder.txt"),
					"Finding " + origin + Environment.NewLine
				);
				if (File.Exists(origin))
				{
					File.AppendAllText(
						Path.Combine(BaseDir, "kcmoder.txt"),
						"Patched " + origin + Environment.NewLine
					);
					return File.ReadAllBytes(origin);
				}

				File.AppendAllText(
					Path.Combine(BaseDir, "kcmoder.txt"),
					"Finding " + origin + " alias" + Environment.NewLine
				);
				var m = aliasList.FirstOrDefault(x => (x.FullSource + ".swf" == origin));
				if (m != null)
				{
					if (!File.Exists(Path.Combine(BaseDir, m.FullAlias + ".swf")))
					{
						File.AppendAllText(
							Path.Combine(BaseDir, "kcmoder.txt"),
							"Alias found but not exists " + Path.Combine(BaseDir, m.FullAlias + ".swf") + Environment.NewLine
						);
					}
					else
					{
						File.AppendAllText(
							Path.Combine(BaseDir, "kcmoder.txt"),
							"Patched(alias) " + origin + Environment.NewLine
						);
						return File.ReadAllBytes(
							Path.Combine(BaseDir, m.FullAlias + ".swf")
						);
					}
				}

				File.AppendAllText(
					Path.Combine(BaseDir, "kcmoder.txt"),
					"Not found " + origin + Environment.NewLine
				);
				return data;
			};
		}
	}
}
