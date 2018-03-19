using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TrotiNet;

namespace Nekoxy
{
	/// <summary>
	/// 通信データを透過し読み取るためのProxyLogic。
	/// Transfer-Encoding: chunked なHTTPリクエストの RequestBody の読み取りは未対応。
	/// </summary>
	public class TransparentProxyLogic : ProxyLogic
	{
		public static event Action<Session> AfterSessionComplete;
		public static event Action<HttpRequest> AfterReadRequestHeaders;
		public static event Action<HttpResponse> AfterReadResponseHeaders;

		public static event Func<HttpRequest, bool> BeforeRequest;
		public static event Func<Session, byte[], byte[]> BeforeResponse;

		/// <summary>
		/// Upstream proxy config
		/// </summary>
		public static ProxyConfig UpstreamProxyConfig { get; set; } = new ProxyConfig(ProxyConfigType.SystemProxy);

		public new static TransparentProxyLogic CreateProxy(HttpSocket clientSocket)
			=> new TransparentProxyLogic(clientSocket);

		public TransparentProxyLogic(HttpSocket clientSocket) : base(clientSocket) { }


		private void SetUpstreamProxy()
		{
			this.RelayHttpProxyHost = null;
			this.RelayHttpProxyPort = 80;

			var config = UpstreamProxyConfig;

			if (config.Type == ProxyConfigType.DirectAccess) return;

			if (config.Type == ProxyConfigType.SpecificProxy)
			{
				this.RelayHttpProxyHost = string.IsNullOrWhiteSpace(config.SpecificProxyHost) ? null : config.SpecificProxyHost;
				this.RelayHttpProxyPort = config.SpecificProxyPort;
				return;
			}

			var requestUri = this.GetEffectiveRequestUri();
			if (requestUri != null)
			{
				var systemProxyConfig = WebRequest.GetSystemWebProxy();
				if (systemProxyConfig.IsBypassed(requestUri)) return;
				var systemProxy = systemProxyConfig.GetProxy(requestUri);

				this.RelayHttpProxyHost = !systemProxy.IsOwnProxy() ? systemProxy.Host : null;
				this.RelayHttpProxyPort = systemProxy.Port;
			}
			else
			{
				var systemProxyHost = WinInetUtil.GetSystemHttpProxyHost();
				var systemProxyPort = WinInetUtil.GetSystemHttpProxyPort();
				if (systemProxyPort == HttpProxy.ListeningPort && systemProxyHost.IsLoopbackHost())
					return;
				this.RelayHttpProxyHost = systemProxyHost;
				this.RelayHttpProxyPort = systemProxyPort;
			}
		}

		private Uri GetEffectiveRequestUri()
		{
			if (this.RequestLine.URI.Contains("://"))
				return new Uri(this.RequestLine.URI);

			int destinationPort;
			var originalUri = this.RequestLine.URI;

			var destinationHost = this.ParseDestinationHostAndPort(this.RequestLine, this.RequestHeaders, out destinationPort);
			this.RequestLine.URI = originalUri;
			var isDefaultPort = destinationPort == (this.RequestLine.Method == "CONNECT" ? 443 : 80);

			var scheme = this.RequestLine.Method == "CONNECT" ? "https" : "http";
			var authority = isDefaultPort ? destinationHost : $"{destinationHost}:{destinationPort}";
			var pathAndQuery = this.RequestLine.URI.Contains("/") ? this.RequestLine.URI : string.Empty;

			Uri uri;
			return Uri.TryCreate($"{scheme}://{authority}/{pathAndQuery}", UriKind.Absolute, out uri) ? uri : null;
		}

		/// <summary>
		/// Override SendResponse to read request data
		/// </summary>
		protected override void SendRequest()
		{
			var req = new HttpRequest(this.RequestLine, this.RequestHeaders, null);

			if (BeforeRequest != null)
			{
				if (!BeforeRequest.Invoke(req))
				{
					this.AbortRequest();
					return;
				}
			}

			AfterReadRequestHeaders?.Invoke(req);

			this.currentSession = new Session();
			this.SocketPS.WriteBinary(Encoding.ASCII.GetBytes(
				$"{this.RequestLine.RequestLine}\r\n{this.RequestHeaders.HeadersInOrder}\r\n"));

			byte[] request = null;
			if (this.State.bRequestHasMessage)
			{
				if (this.State.bRequestMessageChunked)
					this.SocketBP.TunnelChunkedDataTo(this.SocketPS);

				else
				{
					request = new byte[this.State.RequestMessageLength];
					this.SocketBP.TunnelDataTo(request, this.State.RequestMessageLength);
					this.SocketPS.TunnelDataTo(this.TunnelPS, request);
				}
			}
			this.currentSession.Request = new HttpRequest(this.RequestLine, this.RequestHeaders, request);
			this.State.NextStep = this.ReadResponse;
		}

		/// <summary>
		/// Override OnReceiveResponse to read response data
		/// </summary>
		protected override void OnReceiveResponse()
		{
			#region Nekoxy Code
			AfterReadResponseHeaders?.Invoke(new HttpResponse(this.ResponseStatusLine, this.ResponseHeaders, null));

			if (this.ResponseStatusLine.StatusCode != 200) return;

			var response = this.ResponseHeaders.IsUnknownLength()
				? this.GetContentWhenUnknownLength()
				: this.GetContent();
			this.State.NextStep = null;

			using (var ms = new MemoryStream())
			{
				var stream = this.GetResponseMessageStream(response);
				stream.CopyTo(ms);
				var content = ms.ToArray();
				this.currentSession.Response = new HttpResponse(this.ResponseStatusLine, this.ResponseHeaders, content);
			}
			#endregion

			if (BeforeResponse != null)
			{
				response = BeforeResponse?.Invoke(this.currentSession, this.currentSession.Response.Body);
				this.ResponseHeaders.ContentEncoding = null; // remove gzip and chunked...
			}

			#region Nekoxy Code
			this.ResponseHeaders.TransferEncoding = null;
			this.ResponseHeaders.ContentLength = (uint)response.Length;

			this.SendResponseStatusAndHeaders(); // Send HTTP status & head to client
			this.SocketBP.TunnelDataTo(this.TunnelBP, response); // Send response body to client

			if (!this.State.bPersistConnectionPS)
			{
				this.SocketPS?.CloseSocket();
				this.SocketPS = null;
			}

			AfterSessionComplete?.Invoke(this.currentSession);
			#endregion
		}

		private byte[] GetContentWhenUnknownLength()
		{
			var buffer = new byte[512];
			this.SocketPS.TunnelDataTo(ref buffer); // buffer will be re-allocated in TunnelDataTo function
			return buffer;
		}

		private Session currentSession;
	}
}
