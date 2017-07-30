using Newtonsoft.Json;
using System;

namespace BiDirectionalSync
{
	public class TableSchema
	{
		public TableSchema()
		{
			LastUpdated = DateTimeOffset.Now;
		}

		// The primary key of the database
		public Guid Id { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public string RowChanges { get; set; }

		// When this row was last updated
		public DateTimeOffset LastUpdated { get; set; }

		public DateTimeOffset ClientLastUpdated { get; set; }

		// When the row was deleted
		public DateTimeOffset? Deleted { get; set; }

		// Optionally you may want to record which user deleted or last updated the row.

		/// <summary>
		/// Helper function, to clone the object, just to ensure we aren't all keeping the same reference
		/// </summary>
		/// <returns></returns>
		public TableSchema Clone()
		{
			var serialized = JsonConvert.SerializeObject(this);

			return JsonConvert.DeserializeObject<TableSchema>(serialized);
		}
	}
}
