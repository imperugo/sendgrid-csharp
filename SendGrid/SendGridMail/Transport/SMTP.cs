namespace SendGridMail.Transport
{
	using System;
	using System.Net;
	using System.Net.Mail;

	/// <summary>
	///     Transport class for delivering messages via SMTP
	/// </summary>
	public class SMTP : ITransport
	{
		#region Constants

		/// <summary>
		///     Port for Simple Mail Transfer Protocol
		/// </summary>
		public const int Port = 25;

		/// <summary>
		///     SendGrid's host name
		/// </summary>
		public const string SmtpServer = "smtp.sendgrid.net";

		/// <summary>
		///     Port for Secure SMTP
		/// </summary>
		public const int SslPort = 465;

		/// <summary>
		///     Port for TLS (currently not supported)
		/// </summary>
		public const int TlsPort = 571;

		#endregion

		#region Fields

		/// <summary>
		///     Client used to deliver SMTP message
		/// </summary>
		private readonly ISmtpClient client;

		#endregion

		#region Constructors and Destructors

		/// <summary>
		///     Transport created to deliver messages to SendGrid using SMTP
		/// </summary>
		/// <param name="client">SMTP client we are wrapping</param>
		/// <param name="credentials">Sendgrid user credentials</param>
		/// <param name="host">MTA recieving this message.  By default, sent through SendGrid.</param>
		/// <param name="port">SMTP port 25 is the default.  Port 465 can be used for Secure SMTP.</param>
		private SMTP(ISmtpClient client, NetworkCredential credentials, string host = SmtpServer, int port = Port)
		{
			this.client = client;
			switch (port)
			{
				case Port:
					break;
				case SslPort:
					this.client.EnableSsl = true;
					break;
				case TlsPort:
					throw new NotSupportedException("TLS not supported");
			}
		}

		#endregion

		#region Interfaces

		/// <summary>
		///     Interface to allow testing
		/// </summary>
		internal interface ISmtpClient
		{
			#region Public Properties

			bool EnableSsl { get; set; }

			#endregion

			#region Public Methods and Operators

			void Send(MailMessage mime);

			#endregion
		}

		#endregion

		#region Public Methods and Operators

		/// <summary>
		///     Transport created to deliver messages to SendGrid using SMTP
		/// </summary>
		/// <param name="credentials">Sendgrid user credentials</param>
		/// <param name="host">MTA recieving this message.  By default, sent through SendGrid.</param>
		/// <param name="port">SMTP port 25 is the default.  Port 465 can be used for Secure SMTP.</param>
		public static SMTP GetInstance(NetworkCredential credentials, string host = SmtpServer, int port = Port)
		{
			SmtpWrapper client = new SmtpWrapper(host, port, credentials, SmtpDeliveryMethod.Network);
			return new SMTP(client, credentials, host, port);
		}

		/// <summary>
		///     Deliver an email using SMTP protocol
		/// </summary>
		/// <param name="message"></param>
		public void Deliver(ISendGrid message)
		{
			MailMessage mime = message.CreateMimeMessage();
			this.client.Send(mime);
		}

		#endregion

		#region Methods

		/// <summary>
		///     For Unit Testing Only!
		/// </summary>
		/// <param name="client"></param>
		/// <param name="credentials"></param>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		internal static SMTP GetInstance(ISmtpClient client, NetworkCredential credentials, string host = SmtpServer, int port = Port)
		{
			return new SMTP(client, credentials, host, port);
		}

		#endregion

		/// <summary>
		///     Implementation of SmtpClient wrapper, separated to allow dependency injection
		/// </summary>
		internal class SmtpWrapper : ISmtpClient
		{
			#region Fields

			private readonly SmtpClient client;

			#endregion

			#region Constructors and Destructors

			public SmtpWrapper(string host, int port, NetworkCredential credentials, SmtpDeliveryMethod deliveryMethod)
			{
				this.client = new SmtpClient(host, port) { Credentials = credentials, DeliveryMethod = deliveryMethod };
			}

			#endregion

			#region Public Properties

			public bool EnableSsl
			{
				get
				{
					return this.client.EnableSsl;
				}

				set
				{
					this.client.EnableSsl = value;
				}
			}

			#endregion

			#region Public Methods and Operators

			public void Send(MailMessage mime)
			{
				this.client.Send(mime);
			}

			#endregion
		}
	}
}