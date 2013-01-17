namespace SendGridMail
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Net.Mail;
	using System.Net.Mime;
	using System.Text.RegularExpressions;

	public class SendGrid : ISendGrid
	{
		// private/constant vars:
		#region Constants

		private const string ReHtml = @"<\%\s*[^\s]+\s*\%>";

		private const string ReText = @"<\%\s*\%>";

		#endregion

		#region Static Fields

		private static readonly Dictionary<string, string> Filters = InitializeFilters();

		#endregion

		#region Fields

		private readonly MailMessage message;

		private List<string> attachments = new List<string>();

		private Dictionary<string, MemoryStream> streamedAttachments = new Dictionary<string, MemoryStream>();

		#endregion

		// TODO find appropriate types for these
		#region Constructors and Destructors

		internal SendGrid(MailAddress from, MailAddress[] to, MailAddress[] cc, MailAddress[] bcc, string subject, string html, string text, IHeader header = null)
			: this(header)
		{
			this.From = from;
			this.To = to;
			this.Cc = cc;
			this.Bcc = bcc;

			this.message.Subject = subject;

			this.Text = text;
			this.Html = html;
		}

		internal SendGrid(IHeader header)
		{
			this.message = new MailMessage();
			this.Header = header;
			this.Headers = new Dictionary<string, string>();
		}

		#endregion

		#region Public Properties

		public string[] Attachments
		{
			get
			{
				return this.attachments.ToArray();
			}

			set
			{
				this.attachments = value.ToList();
			}
		}

		public MailAddress[] Bcc
		{
			get
			{
				return this.message.Bcc.ToArray();
			}

			set
			{
				this.message.Bcc.Clear();
				foreach (MailAddress mailAddress in value)
				{
					this.message.Bcc.Add(mailAddress);
				}
			}
		}

		public MailAddress[] Cc
		{
			get
			{
				return this.message.CC.ToArray();
			}

			set
			{
				this.message.CC.Clear();
				foreach (MailAddress mailAddress in value)
				{
					this.message.CC.Add(mailAddress);
				}
			}
		}

		public MailAddress From
		{
			get
			{
				return this.message.From;
			}

			set
			{
				if (value != null)
				{
					this.message.From = value;
				}
			}
		}

		public IHeader Header { get; set; }

		public Dictionary<string, string> Headers { get; set; }

		public string Html { get; set; }

		public MailAddress[] ReplyTo
		{
			get
			{
				return this.message.ReplyToList.ToArray();
			}

			set
			{
				this.message.ReplyToList.Clear();
				foreach (MailAddress replyTo in value)
				{
					this.message.ReplyToList.Add(replyTo);
				}
			}
		}

		public Dictionary<string, MemoryStream> StreamedAttachments
		{
			get
			{
				return this.streamedAttachments;
			}

			set
			{
				this.streamedAttachments = value;
			}
		}

		public string Subject
		{
			get
			{
				return this.message.Subject;
			}

			set
			{
				if (value != null)
				{
					this.message.Subject = value;
				}
			}
		}

		public string Text { get; set; }

		public MailAddress[] To
		{
			get
			{
				return this.message.To.ToArray();
			}

			set
			{
				this.message.To.Clear();
				foreach (MailAddress mailAddress in value)
				{
					this.message.To.Add(mailAddress);
				}
			}
		}

		#endregion

		#region Public Methods and Operators

		/// <summary>
		///     Creates an instance of SendGrid's custom message object
		/// </summary>
		/// <returns></returns>
		public static SendGrid GetInstance()
		{
			Header header = new Header();
			return new SendGrid(header);
		}

		/// <summary>
		///     Creates an instance of SendGrid's custom message object with mail parameters
		/// </summary>
		/// <param name="from">The email address of the sender</param>
		/// <param name="to">An array of the recipients</param>
		/// <param name="cc">Supported over SMTP, with future plans for support in the Web transport</param>
		/// <param name="bcc">Blind recipients</param>
		/// <param name="subject">The subject of the message</param>
		/// <param name="html">the html content for the message</param>
		/// <param name="text">the plain text part of the message</param>
		/// <returns></returns>
		public static SendGrid GetInstance(MailAddress from, MailAddress[] to, MailAddress[] cc, MailAddress[] bcc, string subject, string html, string text)
		{
			Header header = new Header();
			return new SendGrid(from, to, cc, bcc, subject, html, text, header);
		}

		public void AddAttachment(Stream stream, string name)
		{
			MemoryStream ms = new MemoryStream();
			stream.CopyTo(ms);
			ms.Seek(0, SeekOrigin.Begin);
			this.StreamedAttachments[name] = ms;
		}

		public void AddAttachment(string filePath)
		{
			this.attachments.Add(filePath);
		}

		public void AddBcc(string address)
		{
			MailAddress mailAddress = new MailAddress(address);
			this.message.Bcc.Add(mailAddress);
		}

		public void AddBcc(IEnumerable<string> addresses)
		{
			if (addresses != null)
			{
				foreach (string address in addresses)
				{
					if (address != null)
					{
						this.AddBcc(address);
					}
				}
			}
		}

		public void AddBcc(IDictionary<string, IDictionary<string, string>> addresssInfo)
		{
			foreach (string address in addresssInfo.Keys)
			{
				IDictionary<string, string> table = addresssInfo[address];

				// DisplayName is the only option that this implementation of MailAddress implements.
				MailAddress mailAddress = new MailAddress(address, table.ContainsKey("DisplayName") ? table["DisplayName"] : null);
				this.message.Bcc.Add(mailAddress);
			}
		}

		public void AddCc(string address)
		{
			MailAddress mailAddress = new MailAddress(address);
			this.message.CC.Add(mailAddress);
		}

		public void AddCc(IEnumerable<string> addresses)
		{
			if (addresses != null)
			{
				foreach (string address in addresses)
				{
					if (address != null)
					{
						this.AddCc(address);
					}
				}
			}
		}

		public void AddCc(IDictionary<string, IDictionary<string, string>> addresssInfo)
		{
			foreach (string address in addresssInfo.Keys)
			{
				IDictionary<string, string> table = addresssInfo[address];

				// DisplayName is the only option that this implementation of MailAddress implements.
				MailAddress mailAddress = new MailAddress(address, table.ContainsKey("DisplayName") ? table["DisplayName"] : null);
				this.message.CC.Add(mailAddress);
			}
		}

		public void AddHeaders(IDictionary<string, string> headers)
		{
			headers.Keys.ToList().ForEach(key => this.Headers[key] = headers[key]);
		}

		public void AddSubVal(string replacementTag, List<string> substitutionValues)
		{
			// let the system complain if they do something bad, since the function returns null
			this.Header.AddSubVal(replacementTag, substitutionValues);
		}

		public void AddTo(string address)
		{
			MailAddress mailAddress = new MailAddress(address);
			this.message.To.Add(mailAddress);
		}

		public void AddTo(IEnumerable<string> addresses)
		{
			if (addresses != null)
			{
				foreach (string address in addresses)
				{
					if (address != null)
					{
						this.AddTo(address);
					}
				}
			}
		}

		public void AddTo(IDictionary<string, IDictionary<string, string>> addresssInfo)
		{
			foreach (string address in addresssInfo.Keys)
			{
				IDictionary<string, string> table = addresssInfo[address];

				// DisplayName is the only option that this implementation of MailAddress implements.
				MailAddress mailAddress = new MailAddress(address, table.ContainsKey("DisplayName") ? table["DisplayName"] : null);
				this.message.To.Add(mailAddress);
			}
		}

		public void AddUniqueIdentifiers(IDictionary<string, string> identifiers)
		{
			this.Header.AddUniqueIdentifier(identifiers);
		}

		public MailMessage CreateMimeMessage()
		{
			string smtpapi = this.Header.AsJson();

			if (!string.IsNullOrEmpty(smtpapi))
			{
				this.message.Headers.Add("X-Smtpapi", smtpapi);
			}

			this.Headers.Keys.ToList().ForEach(k => this.message.Headers.Add(k, this.Headers[k]));

			this.message.Attachments.Clear();
			this.message.AlternateViews.Clear();

			if (this.Attachments != null)
			{
				foreach (string attachment in this.Attachments)
				{
					this.message.Attachments.Add(new Attachment(attachment, MediaTypeNames.Application.Octet));
				}
			}

			if (this.StreamedAttachments != null)
			{
				foreach (KeyValuePair<string, MemoryStream> attachment in this.StreamedAttachments)
				{
					attachment.Value.Position = 0;
					this.message.Attachments.Add(new Attachment(attachment.Value, attachment.Key));
				}
			}

			if (this.Text != null)
			{
				AlternateView plainView = AlternateView.CreateAlternateViewFromString(this.Text, null, "text/plain");
				this.message.AlternateViews.Add(plainView);
			}

			if (this.Html != null)
			{
				AlternateView htmlView = AlternateView.CreateAlternateViewFromString(this.Html, null, "text/html");
				this.message.AlternateViews.Add(htmlView);
			}

			// message.SubjectEncoding = Encoding.GetEncoding(charset);
			// message.BodyEncoding = Encoding.GetEncoding(charset);
			return this.message;
		}

		public void DisableBcc()
		{
			this.Header.Disable(Filters["Bcc"]);
		}

		public void DisableBypassListManagement()
		{
			this.Header.Disable(Filters["BypassListManagement"]);
		}

		public void DisableClickTracking()
		{
			this.Header.Disable(Filters["ClickTracking"]);
		}

		public void DisableFooter()
		{
			this.Header.Disable(Filters["Footer"]);
		}

		public void DisableGoogleAnalytics()
		{
			this.Header.Disable(Filters["GoogleAnalytics"]);
		}

		public void DisableGravatar()
		{
			this.Header.Disable(Filters["Gravatar"]);
		}

		public void DisableOpenTracking()
		{
			this.Header.Disable(Filters["OpenTracking"]);
		}

		public void DisableSpamCheck()
		{
			this.Header.Disable(Filters["SpamCheck"]);
		}

		public void DisableTemplate()
		{
			this.Header.Disable(Filters["Template"]);
		}

		public void DisableUnsubscribe()
		{
			this.Header.Disable(Filters["Unsubscribe"]);
		}

		public void EnableBcc(string email)
		{
			string filter = Filters["Bcc"];

			this.Header.Enable(filter);
			this.Header.AddFilterSetting(filter, new List<string> { "email" }, email);
		}

		public void EnableBypassListManagement()
		{
			this.Header.Enable(Filters["BypassListManagement"]);
		}

		public void EnableClickTracking(bool includePlainText = false)
		{
			string filter = Filters["ClickTracking"];

			this.Header.Enable(filter);
			if (includePlainText)
			{
				this.Header.AddFilterSetting(filter, new List<string> { "enable_text" }, "1");
			}
		}

		public void EnableFooter(string text = null, string html = null)
		{
			string filter = Filters["Footer"];

			this.Header.Enable(filter);
			this.Header.AddFilterSetting(filter, new List<string> { "text/plain" }, text);
			this.Header.AddFilterSetting(filter, new List<string> { "text/html" }, html);
		}

		public void EnableGoogleAnalytics(string source, string medium, string term, string content = null, string campaign = null)
		{
			string filter = Filters["GoogleAnalytics"];

			this.Header.Enable(filter);
			this.Header.AddFilterSetting(filter, new List<string> { "utm_source" }, source);
			this.Header.AddFilterSetting(filter, new List<string> { "utm_medium" }, medium);
			this.Header.AddFilterSetting(filter, new List<string> { "utm_term" }, term);
			this.Header.AddFilterSetting(filter, new List<string> { "utm_content" }, content);
			this.Header.AddFilterSetting(filter, new List<string> { "utm_campaign" }, campaign);
		}

		public void EnableGravatar()
		{
			this.Header.Enable(Filters["Gravatar"]);
		}

		public void EnableOpenTracking()
		{
			this.Header.Enable(Filters["OpenTracking"]);
		}

		public void EnableSpamCheck(int score = 5, string url = null)
		{
			string filter = Filters["SpamCheck"];

			this.Header.Enable(filter);
			this.Header.AddFilterSetting(filter, new List<string> { "maxscore" }, score.ToString(CultureInfo.InvariantCulture));
			this.Header.AddFilterSetting(filter, new List<string> { "url" }, url);
		}

		public void EnableTemplate(string html)
		{
			string filter = Filters["Template"];

			if (!Regex.IsMatch(html, ReHtml))
			{
				throw new Exception("Missing substitution replacementTag in html");
			}

			this.Header.Enable(filter);
			this.Header.AddFilterSetting(filter, new List<string> { "text/html" }, html);
		}

		public void EnableUnsubscribe(string text, string html)
		{
			string filter = Filters["Unsubscribe"];

			if (!Regex.IsMatch(text, ReText))
			{
				throw new Exception("Missing substitution replacementTag in text");
			}

			if (!Regex.IsMatch(html, ReHtml))
			{
				throw new Exception("Missing substitution replacementTag in html");
			}

			this.Header.Enable(filter);
			this.Header.AddFilterSetting(filter, new List<string> { "text/plain" }, text);
			this.Header.AddFilterSetting(filter, new List<string> { "text/html" }, html);
		}

		public void EnableUnsubscribe(string replace)
		{
			string filter = Filters["Unsubscribe"];

			this.Header.Enable(filter);
			this.Header.AddFilterSetting(filter, new List<string> { "replace" }, replace);
		}

		public IEnumerable<string> GetRecipients()
		{
			List<MailAddress> tos = this.message.To.ToList();
			List<MailAddress> ccs = this.message.CC.ToList();
			List<MailAddress> bccs = this.message.Bcc.ToList();

			IEnumerable<string> rcpts = tos.Union(ccs.Union(bccs)).Select(address => address.Address);
			return rcpts;
		}

		public void SetCategory(string category)
		{
			this.Header.SetCategory(category);
		}

		#endregion

		#region Methods

		/// <summary>
		///     Helper function lets us look at the mime before it is sent
		/// </summary>
		/// <param name="directory">directory in which we store this mime message</param>
		internal void SaveMessage(string directory)
		{
			SmtpClient client = new SmtpClient("localhost") { DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory, PickupDirectoryLocation = @"C:\temp" };
			MailMessage msg = this.CreateMimeMessage();
			client.Send(msg);
		}

		private static Dictionary<string, string> InitializeFilters()
		{
			return new Dictionary<string, string> { { "Gravatar", "gravatar" }, { "OpenTracking", "opentrack" }, { "ClickTracking", "clicktrack" }, { "SpamCheck", "spamcheck" }, { "Unsubscribe", "subscriptiontrack" }, { "Footer", "footer" }, { "GoogleAnalytics", "ganalytics" }, { "Template", "template" }, { "Bcc", "bcc" }, { "BypassListManagement", "bypass_list_management" } };
		}

		#endregion
	}
}