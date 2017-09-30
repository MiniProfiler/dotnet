using System;
using System.ComponentModel.DataAnnotations;

namespace StackExchange.Profiling.Storage
{
	public class MiniProfilerClientTimings
	{ 
		[Key]
		public Int64 RowId { get; set; }
		public Guid Id { get; set; }
		public Guid MiniProfilerId { get; set; }
		public string Name { get; set; }
		public decimal Start { get; set; }
		public decimal Duration { get; set; }
	}
}
