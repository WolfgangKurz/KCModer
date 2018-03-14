using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace KCModerPatch
{
	class Program
	{
		private class PatchPair
		{
			public string Before { get; set; }
			public string After { get; set; }

			public PatchPair(string Before, string After)
			{
				this.Before = Before;
				this.After = After;
			}
		}

		static PatchPair[] pathList = new PatchPair[]
			{
				new PatchPair("Nekoxy.dll", "Nekoxy_origin.dll"),
				new PatchPair(
					Path.Combine("lib", "Nekoxy.dll"),
					Path.Combine("lib", "Nekoxy_origin.dll")
				)
			};

		private static bool IsPatched()
			=> pathList.Any(x => File.Exists(x.After));

		private static void Initial()
		{
			Console.Clear();

			Console.WriteLine("KCModer status: {0}", IsPatched() ? "Patched" : "Not patched");
			Console.WriteLine();

			Console.WriteLine("1. Patch");
			Console.WriteLine("2. Restore");
			Console.WriteLine("3. Quit");
			Console.WriteLine();
		}

		private static void DoPatch()
		{
			if (IsPatched())
			{
				Console.WriteLine("Already Patched.");
				return;
			}

			foreach (var path in pathList)
			{
				try
				{
					if (File.Exists(path.Before))
					{
						File.Move(path.Before, path.After);

						File.WriteAllBytes(path.Before, KCModerPatch.Properties.Resources.Nekoxy);

						Console.WriteLine(path.Before + " patched");
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(path.Before + " patch failed");
					Console.WriteLine(e.ToString());
				}
			}

			Console.WriteLine("Patch done.");
		}
		private static void DoRestore()
		{
			if (!IsPatched())
			{
				Console.WriteLine("Not patched.");
				return;
			}

			foreach (var path in pathList)
			{
				try
				{
					if (File.Exists(path.After))
					{
						if (File.Exists(path.Before))
							File.Delete(path.Before);

						File.Move(path.After, path.Before);

						Console.WriteLine(path.Before + " restored");
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(path.Before + " restore failed");
					Console.WriteLine(e.ToString());
				}
			}

			Console.WriteLine("Restore done.");
		}

		static void Main(string[] args)
		{
			var loop = true;
			while (loop)
			{
				Initial();

				Console.Write("> ");
				var input = Console.In.ReadLine();

				Console.WriteLine();
				switch (input)
				{
					case "1":
						DoPatch();
						break;
					case "2":
						DoRestore();
						break;

					case "3":
						loop = false;
						Console.WriteLine("Bye.");
						break;
				}
				Thread.Sleep(3000);
			}
		}
	}
}
