﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebOne
{
	/// <summary>
	/// Транзитная передача HTTP-контента с исправлением содержимого
	/// </summary>
	class Transit
	{
		public static string LastURL = "http://999.999.999.999/CON";//dirty workaround for phantom.sannata.org and similar sites

		//Based on https://habr.com/ru/post/120157/
		//Probably the class name needs to be changed

		/// <summary>
		/// Convert a Web 2.0 page to Web 1.0-like page.
		/// </summary>
		/// <param name="Client">TcpListener client</param>
		public Transit(TcpClient Client)
		{
			Console.Write("\n>");

			byte[] Buffer = new byte[10485760]; //todo: adjust for real request size
			string Request = "";
			string RequestHeaders = "";
			string RequestBody = "";
			int RequestHeadersEnd;

			int Count = 0;
			try
			{
				NetworkStream ClientStream = Client.GetStream();

				while(ClientStream.DataAvailable)
				{
					ClientStream.Read(Buffer, Count, 1);
					Count++;

				}
				Request += Encoding.ASCII.GetString(Buffer).Trim('\0'); //cut empty 10 megabytes

				if (Request.Length == 0) { SendError(Client, 400); return; }

				RequestHeadersEnd = Request.IndexOf("\r\n\r\n");
				RequestHeaders = Request.Substring(0, RequestHeadersEnd);
				RequestBody = Request.Substring(RequestHeadersEnd+4);

				/*Console.Write("-{0} of {1}-", Request.IndexOf("\r\n\r\n"), Request.Length);
				Console.Write("-POST body={0}-", Request.Substring(Request.IndexOf("\r\n\r\n")).Trim("\r\n\r\n".ToCharArray()));*/
			}
			catch(System.IO.IOException ioe) {
				Console.WriteLine("Can't read from client: " + ioe.ToString());
				SendError(Client,500);
				return;
			}

			Match ReqMatch = Regex.Match(Request, @"^\w+\s+([^\s]+)[^\s]*\s+HTTP/.*|");
			if (ReqMatch == Match.Empty)
			{
				//If the request seems to be invalid, raise HTTP 400 error.
				SendError(Client, 400);
				return;
			}

			string RequestMethod = "";
			string[] Headers = RequestHeaders.Split('\n');
			RequestMethod = Headers[0].Substring(0, Headers[0].IndexOf(" "));

			WebHeaderCollection RequestHeaderCollection = new WebHeaderCollection();
			foreach (string hdr in Headers)
			{
				if (hdr.Contains(": ")) //exclude method & URL
				{
					RequestHeaderCollection.Add(hdr);
				}
			}

			string RefererUri = RequestHeaderCollection["Referer"];
			string RequestUri = ReqMatch.Groups[1].Value;
			if (RequestUri.StartsWith("/")) RequestUri = RequestUri.Substring(1); //debug mode: http://localhost:80/http://example.com/indexr.shtml

			//dirty workarounds for HTTP>HTTPS redirection bugs
			if ((RequestUri == RefererUri || RequestUri == LastURL) && RequestUri != "")
			{
				Console.Write("Carousel");
				RequestUri = "https" + RequestUri.Substring(4);
			}

			Console.Write(" " + RequestUri + " ");
			LastURL = RequestUri;
			
			//SendError(Client, 200);
			
			HTTPC https = new HTTPC();
			string Html = ":(";
			string ContentType = "text/html";

			bool StWrong = false; //break operation if something is wrong.
			Console.Write("Try to " + RequestMethod.ToLower());

			try
			{
				if (RequestMethod != "GET") { SendError(Client, 405, "The proxy does not know the " + RequestMethod + " method."); Console.WriteLine(" Wrong method."); return; }

				//try to get...
				HttpResponse response = https.GET(RequestUri, new CookieContainer(), RequestHeaderCollection);
				Console.Write("...");
				Console.Write(response.StatusCode);
				Console.Write("...");
				var body = response.Content;
				ContentType = response.ContentType;
				Console.WriteLine("Body {0}K of {1}", body.Length / 1024, ContentType);
				if (response.ContentType.StartsWith("text"))
					Html = ProcessBody(response.Content);
				else
					Html = response.Content;
			} catch (WebException wex) {
				Html = "Cannot load this page: " + wex.Status.ToString() + "<br><i>" + wex.ToString().Replace("\n", "<br>") + "</i><br>URL: " + RequestUri + Program.GetInfoString();
				Console.WriteLine("Failed.");
			}
			catch (UriFormatException)
			{ 
				StWrong = true;
				SendError(Client, 400, "The URL <b>" + RequestUri + "</b> is not valid.");
			}

			try
			{
				//try to return...
				if (!StWrong)
				{
					string Str = "HTTP/1.0 200\nContent-type: " + ContentType + "\nContent-Length:" + Html.Length.ToString() + "\n\n" + Html;
					byte[] RespBuffer = Encoding.Default.GetBytes(Str);
					Client.GetStream().Write(RespBuffer, 0, RespBuffer.Length);
					Client.Close();
				}
			}catch (Exception ex) {
				Console.WriteLine("Cannot return reply to the client. " + ex.Message);
			}

			Console.WriteLine("The client is served.");
		}

		/// <summary>
		/// Process the reply's body and fix too modern stuff
		/// </summary>
		/// <param name="Body">The original body</param>
		/// <returns>The fixed body, compatible with old browsers</returns>
		private string ProcessBody(string Body) {
			Body = Body.Replace("https", "http");
			Body = Encoding.Default.GetString(Encoding.Convert(Encoding.UTF8, Encoding.Default, Encoding.UTF8.GetBytes(Body)));
			return Body;
		}

		/// <summary>
		/// Send a HTTP error to client
		/// </summary>
		/// <param name="Client">TcpListener client</param>
		/// <param name="Code">Error code number</param>
		/// <param name="Text">Error description for user</param>
		private void SendError(TcpClient Client, int Code, string Text = "")
		{
			Text += Program.GetInfoString();
			string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
			string Html = "<html><body><h1>" + CodeStr + "</h1>"+Text+"</body></html>";
			string Str = "HTTP/1.0 " + CodeStr + "\nContent-type: text/html\nContent-Length:" + Html.Length.ToString() + "\n\n" + Html;
			byte[] Buffer = Encoding.ASCII.GetBytes(Str);
			try
			{
				Client.GetStream().Write(Buffer, 0, Buffer.Length);
				Client.Close();
			}
			catch {
				Console.WriteLine("Cannot return HTTP error " + Code);
			}
		}
	}
}
