using Newtonsoft.Json;

namespace BiDirectionalSync
{
	public class ClientTableSchema: TableSchema
    {
		public ClientTableSchema() {}
		public ClientTableSchema(TableSchema row)
		{
			Id = row.Id;
			Name = row.Name;
			Description = row.Description;
			Deleted = row.Deleted;
			LastUpdated = row.LastUpdated;
		}

		/// <summary>
		/// This contains a serialization of the original table
		/// This is used to determine the actual column changes and to only push the actual changes to the 
		/// </summary>
		public string Original { get; set; }

		public new ClientTableSchema Clone()
		{
			var serialized = JsonConvert.SerializeObject(this);

			return JsonConvert.DeserializeObject<ClientTableSchema>(serialized);
		}
	}
}
