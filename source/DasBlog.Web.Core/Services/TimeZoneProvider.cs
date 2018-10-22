using DasBlog.Core.Services.Interfaces;
using Microsoft.Extensions.Options;
using NodaTime;

namespace DasBlog.Core.Services
{
	public class TimeZoneProvider : ITimeZoneProvider
	{
		private bool adjustDisplayTimeZone;
		private decimal displayTimeZoneIndex;		// this seems to be a misnomer - I think this is just an offset in hours
		public TimeZoneProvider(IOptions<TimeZoneProviderOptions> opt)
		{
			adjustDisplayTimeZone = opt.Value.AdjustDisplayTimeZone;
			displayTimeZoneIndex = opt.Value.DisplayTimeZoneIndex;
		}
		public DateTimeZone GetConfiguredTimeZone()
		{
			// currently Sept 2018 displayTimeZoneIndex is always an int.
			if (adjustDisplayTimeZone)
			{
				return DateTimeZone.ForOffset(Offset.FromHoursAndMinutes((int)displayTimeZoneIndex
				  , (int)(displayTimeZoneIndex % 1m * 60)));
			}
			else
			{
				return DateTimeZone.Utc;
			}
		}
	}

	public class TimeZoneProviderOptions
	{
		public bool AdjustDisplayTimeZone { get; set; }
		public decimal DisplayTimeZoneIndex { get; set; }
					// hopefully we will end up allowing half hour increments, etc.
	}
}
