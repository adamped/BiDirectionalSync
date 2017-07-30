using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace BiDirectionalSync
{
	public class ServerApi
	{
		// Example Table in a database
		private IList<TableSchema> _rows = new List<TableSchema>();
		private object _transactionLock = new object(); // Simulates a SQL Transaction. You must lock a row, when doing a select, then update

		public void Insert(TableSchema row)
		{
			// Just to make sure it has a value if the client doesn't pass one through
			if (row.LastUpdated == DateTimeOffset.MinValue)
				row.LastUpdated = DateTimeOffset.Now;

			IList<ColumnChange> changes = new List<ColumnChange>();
			changes.Add(new ColumnChange() { Column = nameof(row.Name) });
			changes.Add(new ColumnChange() { Column = nameof(row.Description) });

			row.RowChanges = JsonConvert.SerializeObject(changes);

			_rows.Add(row.Clone());
		}

		/// <summary>
		/// This is simulating a put request
		/// </summary>
		/// <param name="jsonUpdate"></param>
		public void DifferentialSync(List<string> jsonUpdate)
		{
			foreach (var item in jsonUpdate)
			{
				dynamic update = JsonConvert.DeserializeObject(item);

				var lastUpdated = (DateTimeOffset)update.LastUpdated;
				var id = (Guid)update.Id;
				var dbRow = _rows.Single(x => x.Id == id);

				if (((JObject)update)["Name"] != null)
					dbRow.Name = update.Name;

				if (((JObject)update)["Description"] != null)
					dbRow.Description = update.Description;

				dbRow.LastUpdated = DateTimeOffset.Now;
				dbRow.ClientLastUpdated = lastUpdated;
				
			}
		}
		public static bool IsPropertyExist(dynamic settings, string name)
		{
			if (settings is ExpandoObject)
				return ((IDictionary<string, object>)settings).ContainsKey(name);

			return settings.GetType().GetProperty(name) != null;
		}

		/// <summary>
		/// Simple Update, with choice of Server or Client Wins
		/// </summary>
		/// <param name="row"></param>
		private void Update(TableSchema row)
		{
			if (row.Deleted.HasValue)
			{
				var dbRow = _rows.Single(x => x.Id == row.Id);

				dbRow.Deleted = row.Deleted;
				dbRow.LastUpdated = row.LastUpdated;
			}
			else
			{
				var dbRow = _rows.Single(x => x.Id == row.Id);

				if (dbRow.ClientLastUpdated > row.LastUpdated)
				{
					// Conflict 
					// Here you can just do a server, or client wins scenario, on a whole row basis. 
					// E.g take the servers word or the clients word

					// e.g. Server - wins - Ignore changes and just update time.
					dbRow.LastUpdated = DateTimeOffset.Now;
					dbRow.ClientLastUpdated = row.LastUpdated;
				}
				else // Client is new than server
				{					
					dbRow.Name = row.Name;
					dbRow.Description = row.Description;
					dbRow.LastUpdated = DateTimeOffset.Now;
					dbRow.ClientLastUpdated = row.LastUpdated;
				}
			}
		}

		public IList<TableSchema> PullSync(DateTimeOffset? since = null)
		{
			if (since == null)
				return _rows;
			else
			{
				var list = _rows.Where(x => x.LastUpdated >= since.Value || (x.Deleted != null && x.Deleted > since.Value)).ToList();
				return list;
			}
		}

		public void PushSync(IList<TableSchema> rows)
		{

			foreach (var row in rows)
				if (!_rows.Any(x => x.Id == row.Id))
					Insert(row);
				else
					Update(row);

		}


		#region Helper

		/// <summary>
		/// Just a helper method to show everything in the database.
		/// Not actually part of the pattern.
		/// </summary>
		/// <returns></returns>
		public IList<TableSchema> GetAll()
		{
			return _rows.OrderBy(x => x.Name).ToList();
		}

		#endregion
	}
}
