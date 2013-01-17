namespace SendGridMail.Transport
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Text;
	using System.Xml;

	using RestSharp;

	public class Web : ITransport
	{
		// TODO: Make this configurable
		#region Constants

		public const string BaseURl = "sendgrid.com/api/";

		public const string Endpoint = "mail.send";

		public const string JsonFormat = "json";

		public const string XmlFormat = "xml";

		#endregion

		#region Fields

		private readonly bool Https;

		private readonly NetworkCredential credentials;

		#endregion

		#region Constructors and Destructors

		/// <summary>
		///     Creates a new Web interface for sending mail.  Preference is using the Factory method.
		/// </summary>
		/// <param name="credentials">SendGrid user parameters</param>
		/// <param name="https">Use https?</param>
		internal Web(NetworkCredential credentials, bool https = true)
		{
			this.Https = https;
			this.credentials = credentials;
		}

		#endregion

		#region Properties

		public IWebProxy Proxy { get; set; }

		#endregion

		#region Public Methods and Operators

		/// <summary>
		///     Factory method for Web transport of sendgrid messages
		/// </summary>
		/// <param name="credentials">SendGrid credentials for sending mail messages</param>
		/// <param name="https">Use https?</param>
		/// <returns>New instance of the transport mechanism</returns>
		public static Web GetInstance(NetworkCredential credentials, bool https = true)
		{
			return new Web(credentials, https);
		}

		/// <summary>
		///     Delivers a message over SendGrid's Web interface
		/// </summary>
		/// <param name="message"></param>
		public void Deliver(ISendGrid message)
		{
			RestClient client = this.Https ? new RestClient("https://" + BaseURl) : new RestClient("http://" + BaseURl);

			if (this.Proxy != null)
			{
				client.Proxy = this.Proxy;
			}

			RestRequest request = new RestRequest(Endpoint + ".xml", Method.POST);

			this.AttachFormParams(message, request);
			this.AttachFiles(message, request);

			IRestResponse response = client.Execute(request);
			this.CheckForErrors(response);
		}

		#endregion

		#region Methods

		internal List<KeyValuePair<string, FileInfo>> FetchFileBodies(ISendGrid message)
		{
			if (message.Attachments == null)
			{
				return new List<KeyValuePair<string, FileInfo>>();
			}

			return message.Attachments.Select(name => new KeyValuePair<string, FileInfo>(name, new FileInfo(name))).ToList();
		}

		internal List<KeyValuePair<string, string>> FetchFormParams(ISendGrid message)
		{
			List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("api_user", this.credentials.UserName), new KeyValuePair<string, string>("api_key", this.credentials.Password), new KeyValuePair<string, string>("headers", message.Headers.Count == 0 ? null : Utils.SerializeDictionary(message.Headers)), new KeyValuePair<string, string>("replyto", message.ReplyTo.Length == 0 ? null : message.ReplyTo.ToList().First().Address), new KeyValuePair<string, string>("from", message.From.Address), new KeyValuePair<string, string>("fromname", message.From.DisplayName), new KeyValuePair<string, string>("subject", message.Subject), new KeyValuePair<string, string>("text", message.Text), new KeyValuePair<string, string>("html", message.Html), new KeyValuePair<string, string>("x-smtpapi", message.Header.AsJson()) };
			if (message.To != null)
			{
				result = result.Concat(message.To.ToList().Select(a => new KeyValuePair<string, string>("to[]", a.Address))).Concat(message.To.ToList().Select(a => new KeyValuePair<string, string>("toname[]", a.DisplayName))).ToList();
			}

			if (message.Bcc != null)
			{
				result = result.Concat(message.Bcc.ToList().Select(a => new KeyValuePair<string, string>("bcc[]", a.Address))).ToList();
			}

			if (message.Cc != null)
			{
				result = result.Concat(message.Cc.ToList().Select(a => new KeyValuePair<string, string>("cc[]", a.Address))).ToList();
			}

			return result.Where(r => !string.IsNullOrEmpty(r.Value)).ToList();
		}

		internal List<KeyValuePair<string, MemoryStream>> FetchStreamingFileBodies(ISendGrid message)
		{
			return message.StreamedAttachments.Select(kvp => kvp).ToList();
		}

		private void AttachFiles(ISendGrid message, RestRequest request)
		{
			// TODO: think the files are being sent in the POST data... but we need to add them as params as well
			List<KeyValuePair<string, FileInfo>> files = this.FetchFileBodies(message);
			files.ForEach(kvp => request.AddFile("files[" + Path.GetFileName(kvp.Key) + "]", kvp.Value.FullName));

			List<KeyValuePair<string, MemoryStream>> streamingFiles = this.FetchStreamingFileBodies(message);
			foreach (KeyValuePair<string, MemoryStream> file in streamingFiles)
			{
				string name = file.Key;
				MemoryStream stream = file.Value;
				Action<Stream> writer = delegate(Stream s) { stream.CopyTo(s); };

				request.AddFile("files[" + name + "]", writer, name);
			}
		}

		private void AttachFormParams(ISendGrid message, RestRequest request)
		{
			List<KeyValuePair<string, string>> formParams = this.FetchFormParams(message);
			formParams.ForEach(kvp => request.AddParameter(kvp.Key, kvp.Value));
		}

		private void CheckForErrors(IRestResponse response)
		{
			// transport error
			if (response.ResponseStatus == ResponseStatus.Error)
			{
				throw new Exception(response.ErrorMessage);
			}

			// TODO: check for HTTP errors... don't throw exceptions just pass info along?
			string content = response.Content;
			MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

			using (XmlReader reader = XmlReader.Create(stream))
			{
				while (reader.Read())
				{
					if (reader.IsStartElement())
					{
						switch (reader.Name)
						{
							case "result":
								break;
							case "message": // success
								bool errors = reader.ReadToNextSibling("errors");
								if (errors)
								{
									throw new ProtocolViolationException(content);
								}
								else
								{
									return;
								}

							case "error": // failure
								throw new ProtocolViolationException(content);
							default:
								throw new ArgumentException("Unknown element: " + reader.Name);
						}
					}
				}
			}
		}

		#endregion
	}
}