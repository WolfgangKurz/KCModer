﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nekoxy
{
    /// <summary>
    /// HTTPセッションデータ。
    /// </summary>
    public class Session
    {
		public bool KCModerPatched { get; } = true;

        /// <summary>
        /// HTTPリクエストデータ。
        /// </summary>
        public HttpRequest Request { get; internal set; }

        /// <summary>
        /// HTTPレスポンスデータ。
        /// </summary>
        public HttpResponse Response { get; internal set; }

        public override string ToString()
            => $"{this.Request}{Environment.NewLine}{Environment.NewLine}" +
               $"{this.Response}";
    }
}
