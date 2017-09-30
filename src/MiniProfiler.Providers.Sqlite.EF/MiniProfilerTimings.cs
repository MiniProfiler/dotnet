using System;
using System.ComponentModel.DataAnnotations;

namespace StackExchange.Profiling.Storage
{
    public class MiniProfilerTimings
    {
		[Key]
		public long RowId { get; set; }
		public Guid Id { get; set; }
		public Guid MiniProfilerId { get; set; }
		public Guid ParentTimingId { get; set; }
		public string Name { get; set; }
		public decimal? DurationMilliseconds { get; set; }
		public decimal StartMilliseconds { get; set; }
		public bool IsRoot { get; set; }
		public short Depth { get; set; }
		public string CustomTimingsJson { get; set; }
	}
}
