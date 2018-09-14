namespace DasBlog.Core.Services.Interfaces
{
	public interface IEmailService
	{
		void SendMail(EmailMessage message);
	}
}
