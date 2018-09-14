using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

namespace DasBlog.Core.Services.Interfaces
{
	public class EmailService : IEmailService
	{
		private readonly ILogger _logger;

		private bool _isEnabled;
		private readonly string _smtpServer;
		private readonly int _smtpPort;
		private readonly string _smtpUsername;
		private readonly string _smtpPassword;
		private readonly bool _isSmtpAuthEnabled;
		private readonly bool _smtpUseSSL;
				
		public EmailService(IDasBlogSettings settings, ILogger<EmailService> logger)
		{
			var siteConfig = settings.SiteConfiguration;
			_logger = logger;

			// NB: is this right?  Limits the email service to only handling the daily report
			_isEnabled = siteConfig.EnableDailyReportEmail;
			
			// NB: What happens if the site.config file gets changed between runs of the task?
			// NB: Should we cater for that.
			_smtpServer = siteConfig.SmtpServer;
			_smtpPort = siteConfig.SmtpPort;
			_smtpUsername = siteConfig.SmtpUserName;
			_smtpPassword = siteConfig.SmtpPassword;
			_isSmtpAuthEnabled = siteConfig.EnableSmtpAuthentication;
			_smtpUseSSL = siteConfig.UseSSLForSMTP;

			ValidateConfiguration();			
		}
		
		public void SendMail(EmailMessage emailMessage)
		{
			if (_isEnabled) return;

			var message = new MimeMessage()
			{				
				Subject = emailMessage.Subject,
				Body = new TextPart(TextFormat.Html)
				{					
					Text = emailMessage.Body
				}
			};

			message.To.AddRange(emailMessage.ToAddresses.Select(x => new MailboxAddress(x)));
			message.From.Add(new MailboxAddress(emailMessage.FromAddress));

			using (var emailClient = new SmtpClient())
			{
				emailClient.Connect(_smtpServer, _smtpPort, _smtpUseSSL);

				emailClient.AuthenticationMechanisms.Remove("XOAUTH2");

				if (_isSmtpAuthEnabled)
					emailClient.Authenticate(_smtpUsername, _smtpPassword);

				emailClient.Send(message);
				emailClient.Disconnect(true);
			}
		}

		private void ValidateConfiguration()
		{
			if (!_isEnabled) return;

			try
			{
				if (string.IsNullOrWhiteSpace(_smtpServer)) throw new ArgumentNullException(nameof(_smtpServer));
				if (_smtpPort == default(int)) throw new ArgumentNullException(nameof(_smtpPort));

				if (_isSmtpAuthEnabled)
				{
					if (string.IsNullOrWhiteSpace(_smtpUsername)) throw new ArgumentNullException(nameof(_smtpUsername));
					if (string.IsNullOrWhiteSpace(_smtpPassword)) throw new ArgumentNullException(nameof(_smtpPassword));
				}
			}
			catch (Exception ex)
			{				
				_logger.LogError(ex.Message, "Invalid Email Service Configuration Provided.");
				_isEnabled = false;
				//NB: Should we fail silently, or throw?
				throw;
			}
		}
	}

	//NB: Where should this live?
	public class EmailMessage
	{
		public List<string> ToAddresses { get; set; }
		public string FromAddress { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }

		public EmailMessage()
		{
			ToAddresses = new List<string>();
		}
	}
}
