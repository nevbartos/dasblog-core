using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using DasBlog.Core;
using DasBlog.Core.Configuration;
using DasBlog.Core.Services.Interfaces;

namespace DasBlog.Web.Services
{
	internal class ReportMailerService : IHostedService, IDisposable
	{
		private readonly IEmailService _emailService;
		private readonly ISiteConfig _config;
		private readonly ILogger _logger;
		
		private Timer _timer;

		public ReportMailerService(IEmailService emailService, IDasBlogSettings settings, ILogger<ReportMailerService> logger)
        {
			_emailService = emailService;
			_config = settings.SiteConfiguration;
			_logger = logger;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Service is starting.");

			//TODO: Set the timer to wait 30 seconds, then to repeat every hour
			//_timer = new Timer(DoWork, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3600));

			_timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Service is stopping.");

			_timer?.Change(Timeout.Infinite, 0);

			return Task.CompletedTask;
		}

		private void DoWork(object state)
		{
			var lastReportDateUTC = DateTime.Now.ToUniversalTime();

			if (!_config.EnableDailyReportEmail) return;

			try
			{
				//TODO: change condition back
				//if (lastReportDateUTC.Day != DateTime.Now.ToUniversalTime().Day)
				if (1 == 1)
				{
					// NB: should we be using EventCodes a la olde worlde when loggin?
					_logger.LogInformation("Sending Daily Email Report");
										
					var message = new EmailMessage
					{
						FromAddress = _config.Contact,
						ToAddresses = {
							!string.IsNullOrEmpty(_config.NotificationEMailAddress)
							? _config.NotificationEMailAddress
							: _config.Contact
						},
						Subject = $"Weblog Daily Activity Report for '{lastReportDateUTC.ToLongDateString()}'",
						Body = GenerateReportEmailBody(lastReportDateUTC)						
					};

					_emailService.SendMail(message);

					_logger.LogInformation("Sent Daily Email Report");

					// and update the cached date to today
					lastReportDateUTC = DateTime.Now.ToUniversalTime();
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "Problem Sending Daily Report");
				_timer.Change(TimeSpan.FromSeconds(3600), TimeSpan.FromSeconds(30));
			}
		}

		private string GenerateReportEmailBody(DateTime reportDate)
		{
			//NB: Should we move this 'template' to a markdown template read in from somewhere?
			var sb = new System.Text.StringBuilder();

			sb.Append(GetStyleSheet());
			sb.Append("<p><table><tr><td class=\"mainheader\" width=\"100%\">Weblog Daily Email Report (" + reportDate.ToLongDateString() + " UTC)</td></tr></table></p>");

			try
			{
				var referrerUrls = new Dictionary<string, int>();
				var userAgents = new Dictionary<string, int>();
				var searchUrls = new Dictionary<string, int>();
				var userDomains = new Dictionary<string, int>();

				//NB: How should we be handling the log reading?  
				//Do we need to implement the DasBlog.Core.Services.EventLineParser stuff again?

				//ILoggingDataService logService = LoggingDataServiceFactory.GetService(logPath);
				var siteRoot = _config.Root.ToUpper();

				//var logItems = new LogDataItemCollection();
				//logItems.AddRange(logService.GetReferralsForDay(reportDate));

				//foreach (LogDataItem log in logItems)
				//{
				//	bool exclude = false;
				//	if (log.UrlReferrer != null)
				//	{
				//		exclude = log.UrlReferrer.ToUpper().StartsWith(siteRoot);

				//		// Let Utils.ParseSearchString decide whether it's a search engine referrer.
				//		HyperLink link = SiteUtilities.ParseSearchString(log.UrlReferrer);

				//		if (link != null)
				//		{
				//			string linktext = "<a href=\"" + link.NavigateUrl + "\">" + link.Text + "</a>";
				//			exclude = true;
				//			if (!searchUrls.ContainsKey(linktext))
				//			{
				//				searchUrls[linktext] = 0;
				//			}
				//			searchUrls[linktext] = searchUrls[linktext] + 1;
				//		}
				//	}

				//	if (!exclude)
				//	{
				//		string linktext = log.UrlReferrer;
				//		if (linktext.Length > 0)
				//			linktext = "<a href=\"" + log.UrlReferrer + "\">" + log.UrlReferrer + "</a>";

				//		if (!referrerUrls.ContainsKey(linktext))
				//		{
				//			referrerUrls[linktext] = 0;
				//		}

				//		referrerUrls[linktext] = referrerUrls[linktext] + 1;

				//		log.UserAgent = WebUtility.HtmlEncode(log.UserAgent);
				//		if (!userAgents.ContainsKey(log.UserAgent))
				//		{
				//			userAgents[log.UserAgent] = 0;
				//		}

				//		userAgents[log.UserAgent] = userAgents[log.UserAgent] + 1;

				//		log.UserDomain = WebUtility.HtmlEncode(log.UserDomain);
				//		if (!userDomains.ContainsKey(log.UserDomain))
				//		{
				//			userDomains[log.UserDomain] = 0;
				//		}

				//		userDomains[log.UserDomain] = userDomains[log.UserDomain] + 1;
				//	}
				//}

				sb.Append("<p>");
				sb.Append("<table width=\"100%\">");
				sb.Append(MakeTableHeader("Summary", "Hits"));
				sb.Append(MakeTableRow("Internet Searches", GetTotal(searchUrls)));
				sb.Append(MakeTableRow("Referrers", GetTotal(referrerUrls)));
				sb.Append("</table>");
				sb.Append("</p>");

				sb.Append("<p>");
				sb.Append("<table width=\"100%\">");
				sb.Append(MakeTableHeader("Internet Searches", "Count"));
				sb.Append(MakeTableRowsFromArray(searchUrls));
				sb.Append("</table>");
				sb.Append("</p>");

				sb.Append("<p>");
				sb.Append("<table width=\"100%\">");
				sb.Append(MakeTableHeader("Referrers", "Count"));
				sb.Append(MakeTableRowsFromArray(referrerUrls));
				sb.Append("</table>");
				sb.Append("</p>");

				sb.Append("<p>");
				sb.Append("<table width=\"100%\">");
				sb.Append(MakeTableHeader("User Agents", "Count"));
				sb.Append(MakeTableRowsFromArray(userAgents));
				sb.Append("</table>");
				sb.Append("</p>");

				sb.Append("<p>");
				sb.Append("<table width=\"100%\">");
				sb.Append(MakeTableHeader("User Domains", "Count"));
				sb.Append(MakeTableRowsFromArray(userDomains));
				sb.Append("</table>");
				sb.Append("</p>");
				sb.Append("<br/><br/>");

			}
			catch (Exception e)
			{
				sb.Append("<p>Error : " + e.ToString() + "</p>");
			}

			return sb.ToString();
		}

		private string GetStyleSheet()
		{
			var sb = new System.Text.StringBuilder();
			sb.Append("<style type=\"text/css\">");
			sb.Append("table {border: 0; border-spacing: 0;	border-collapse: collapse; padding: 0;	width: 100%; font-family: Tahoma,Arial,Helvetica; font-size: 8pt; }");
			sb.Append("td.heading { text-align: left;	font-weight: bold; color: white; background-color: #919eae;	border: 0;	padding: 10px 5px 10px 5px;}");
			sb.Append("td.data {font-weight: normal;	border: 0; border-color: #fff #fff #abc #fff; border-style: solid; border-width: 1px;	padding: 0 5px 0 5px; }");
			sb.Append("td.mainheader {text-align: center; font-weight: bold; color: white; background-color: #919eae; border: 0; padding: 10px 5px 10px 5px;	font-size: 16pt;}");
			sb.Append("</style>");
			return sb.ToString();
		}

		private string GetTotal(Dictionary<string, int> hash)
		{
			return hash.Values.Sum().ToString();
		}

		private string MakeTableHeader(string col1, string col2)
		{
			var sb = new System.Text.StringBuilder();
			sb.Append("<tr>");
			sb.Append("<td class=\"heading\" width=\"90%\"><b>" + WebUtility.HtmlEncode(col1) + "</b></td>");
			sb.Append("<td class=\"heading\" width=\"10%\"><b>" + WebUtility.HtmlEncode(col2) + "</b></td>");
			sb.Append("</tr>");
			return sb.ToString();
		}

		private string MakeTableRow(string title, string count)
		{
			var sb = new System.Text.StringBuilder();
			sb.Append("<tr>");
			sb.Append("<td class=\"data\">"); // needed to split this as the doted IP is not liked 
			sb.Append(title);                 // when adding strings together ??
			sb.Append("</td>");               // depending on email client / SMTP server ??
			sb.Append("<td class=\"data\">" + count + "</td>");
			sb.Append("</tr>");
			return sb.ToString();
		}

		private string MakeTableRowsFromArray(IDictionary<string, int> hash)
		{
			var sb = new System.Text.StringBuilder();

			//List<ActivityItem> arrayList = GenerateSortedItemList(hash);
			//foreach (ActivityItem ai in arrayList)
			//{
			//	sb.Append(MakeTableRow(ai.Key, ai.Val.ToString()));
			//}

			return sb.ToString();
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}		
	}
}
