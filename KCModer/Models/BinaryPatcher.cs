using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

using Nekoxy;

namespace KCModer.Models
{
	internal class BinaryPatcher
	{
		public virtual byte[] original_hash { get; protected set; }
		public virtual int original_length { get; protected set; }

		protected virtual string match_query => "";
		protected virtual string patched_file => "";

		public BinaryPatcher()
		{
			this.original_hash = new byte[16];
			this.original_length = 0;
		}

		public virtual bool Patch(Session session, ref byte[] data)
		{
			try
			{
				if (!session.Request.PathAndQuery.StartsWith(this.match_query)) return false;
				if (data.Length != original_length) return false;

				byte[] hash;
				using (MD5 md5 = MD5.Create()) hash = md5.ComputeHash(data);
				if (!hash.SequenceEqual(original_hash)) return false;

				data = File.ReadAllBytes(this.patched_file);
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
