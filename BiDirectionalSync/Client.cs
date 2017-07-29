using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BiDirectionalSync
{
	public class Client
	{
		// Example Table in a database
		private IList<ClientTableSchema> _rows = new List<ClientTableSchema>();
		private readonly ServerApi _server;
		private DateTimeOffset _lastSync = DateTimeOffset.MinValue;

		public Client(ServerApi server)
		{
			_server = server;
		}

		/// <summary>
		/// Will only push actual changes. 
		/// Allows differential columns to change
		/// </summary>
		public void DifferentialSync()
		{
			var changed = _rows.Where(x => x.LastUpdated >= _lastSync || (x.Deleted != null && x.Deleted >= _lastSync)).ToList();

			var list = new List<string>();

			foreach (var item in changed)
			{
				var original = JsonConvert.DeserializeObject<TableSchema>(item.Original);

				// A better way to implement this is needed. It only detects the first column changed.
				// This would need to group them all together and send them.
				// But again, this is a sample, I can't do all the work for you :)
				// At the moment this will only work with Name or Description

				if (original.Name != item.Name)
					list.Add(JsonConvert.SerializeObject(new { Id = item.Id, LastUpdated=item.LastUpdated, Name = item.Name }));
				else if (original.Description != item.Description)
					list.Add(JsonConvert.SerializeObject(new { Id = item.Id, LastUpdated = item.LastUpdated, Description = item.Description }));

				item.Original = null; // Clear original as now sync'd
			}

			_server.DifferentialSync(list);

			foreach (var row in _server.PullSync(_lastSync))
				if (!_rows.Any(x => x.Id == row.Id)) // Does not exist, hence insert
					InsertRow(new ClientTableSchema(row));
				else if (row.Deleted.HasValue)
					DeleteRow(row.Id);
				else
					UpdateRow(new ClientTableSchema(row));

			_lastSync = DateTimeOffset.Now;
		}

		/// <summary>
		/// Does a Sync at the Row Level.
		/// LastUpdated is used to determined who wins at the server.
		/// </summary>
		public void Sync()
		{
			// All rows that have changed since last sync
			var changed = _rows.Where(x => x.LastUpdated >= _lastSync || (x.Deleted != null && x.Deleted >= _lastSync)).ToList();
			_server.PushSync(changed.Cast<TableSchema>().ToList());

			foreach (var row in _server.PullSync(_lastSync))
				if (!_rows.Any(x => x.Id == row.Id)) // Does not exist, hence insert
					InsertRow(new ClientTableSchema(row));
				else if (row.Deleted.HasValue)
					DeleteRow(row.Id);
				else
					UpdateRow(new ClientTableSchema(row));

			_lastSync = DateTimeOffset.Now;
		}



		public void InsertRow(ClientTableSchema row)
		{
			_rows.Add(row.Clone());
		}

		public void DeleteRow(Guid id)
		{
			_rows.Single(x => x.Id == id).Deleted = DateTimeOffset.Now;
		}

		public void UpdateRow(ClientTableSchema row)
		{
			var dbRow = _rows.Single(x => x.Id == row.Id);

			if (string.IsNullOrEmpty(dbRow.Original))
				dbRow.Original = JsonConvert.SerializeObject(row);

			dbRow.Name = row.Name;
			dbRow.Description = row.Description;
			dbRow.LastUpdated = DateTimeOffset.Now;
		}


		#region Helper

		/// <summary>
		/// Just a helper method to show everything in the database.
		/// Not actually part of the pattern.
		/// </summary>
		/// <returns></returns>
		public IList<ClientTableSchema> GetAll()
		{
			return _rows.OrderBy(x => x.Name).ToList();
		}

		#endregion


	}
}
