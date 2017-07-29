using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BiDirectionalSync
{

	// This is an example project to show bi-driectional sync between multiple clients and server.
	// Instead of having to setup an API and database, this will just use in memory storage and method calls to simulate
	// You would normally implement this with a REST API and Database on the Server and the clients
	// would be a mobile or desktop application, somwhere different than the server.

	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine(" **** Scenario 1 (Intial Sync) ****");

			var server = new ServerApi();
			var client1 = new Client(server);
			var client2 = new Client(server);

			var row1Id = Guid.NewGuid();
			var row2Id = Guid.NewGuid();

			// Initial data loaded into the server
			server.Insert(new TableSchema() { Id = row1Id, Name = "Row1", Description = "mbeat" });
			server.Insert(new TableSchema() { Id = row2Id, Name = "Row2", Description = "samsung" });

			// Client adds a row
			var clientRow3 = new ClientTableSchema()
			{
				Id = Guid.NewGuid(),
				Name = "Row3",
				Description = "acer"
			};

			client1.InsertRow(clientRow3);

			// Scenario 1: New Row Added, New Server Rows
			client1.Sync();

			Console.WriteLine("\nCLIENT1:");
			foreach (var row in client1.GetAll())
				Console.WriteLine(RowDisplay(row));

			Console.WriteLine("\nSERVER:");
			foreach (var row in server.GetAll())
				Console.WriteLine(RowDisplay(row));


			// Scenario 2: New Row Added, Client2
			Console.WriteLine("\n **** Scenario 2 (Client Row Added) ****");
			var row4Id = Guid.NewGuid();
			client2.InsertRow(new ClientTableSchema() { Id = row4Id, Name = "Row4", Description = "lg" });

			Console.WriteLine("\nCLIENT2 (Before Sync):");
			foreach (var row in client2.GetAll())
				Console.WriteLine(RowDisplay(row));

			Console.WriteLine("\nNow syncing Client 2 and Client 1");
			client2.Sync();
			client1.Sync();

			Console.WriteLine("\nCLIENT1:");
			foreach (var row in client1.GetAll())
				Console.WriteLine(RowDisplay(row));

			Console.WriteLine("\nCLIENT2:");
			foreach (var row in client2.GetAll())
				Console.WriteLine(RowDisplay(row));

			Console.WriteLine("\nSERVER:");
			foreach (var row in server.GetAll())
				Console.WriteLine(RowDisplay(row));

			// This finishes up the Insert Rows, nice and easy

			// Scenario 3: Delete a row and sync
			Console.WriteLine("\n **** Scenario 3 (Delete Row) ****");
			client1.DeleteRow(row1Id);

			Console.WriteLine("\nCLIENT1 (Before Sync):");
			foreach (var row in client1.GetAll())
				Console.WriteLine(RowDisplay(row));

			Console.WriteLine("\nNow syncing Client 1 and Client 2");
			client1.Sync();
			client2.Sync();

			Console.WriteLine("\nCLIENT1:");
			foreach (var row in client1.GetAll())
				Console.WriteLine(RowDisplay(row));

			Console.WriteLine("\nCLIENT2:");
			foreach (var row in client2.GetAll())
				Console.WriteLine(RowDisplay(row));

			Console.WriteLine("\nSERVER:");
			foreach (var row in server.GetAll())
				Console.WriteLine(RowDisplay(row));


			// Scenario 4: Update a row
			Console.WriteLine("\n **** Scenario 4 (Update Row) ****");
			clientRow3.Name = "Row3 (Updated)";
			clientRow3.LastUpdated = DateTimeOffset.Now;
			client1.UpdateRow(clientRow3);

			Console.WriteLine("\nCLIENT1 (Before Sync):");
			foreach (var row in client1.GetAll())
				Console.WriteLine(RowDisplay(row));

			Console.WriteLine("\nNow syncing Client 2 and Client 1");
			client1.Sync();
			client2.Sync();

			Console.WriteLine("\nCLIENT1:");
			foreach (var row in client1.GetAll())
				Console.WriteLine(RowDisplay(row));

			Console.WriteLine("\nCLIENT2:");
			foreach (var row in client2.GetAll())
				Console.WriteLine(RowDisplay(row));

			Console.WriteLine("\nSERVER:");
			foreach (var row in server.GetAll())
				Console.WriteLine(RowDisplay(row));

			// Scenario 5: Conflicting Update
			Console.WriteLine("\n **** Scenario 5 (Partial non-conflicting update) ****");

			// Client 1 updates first, but Client 2 sync's first

			client1.UpdateRow(new ClientTableSchema() { Id = row4Id, LastUpdated = DateTimeOffset.Now, Name = "Updated Name (Row4)", Description = "lg" });

			client2.UpdateRow(new ClientTableSchema() { Id = row4Id, LastUpdated = DateTimeOffset.Now, Name = "Row4", Description = "Updated Description (lg)" });

			Console.WriteLine("\nNow syncing Client 2 and Client 1");
			client2.DifferentialSync(); // Does normal update

			client1.DifferentialSync(); // Will do partial, if it detects conflict

			Console.WriteLine("\nCLIENT1:");
			foreach (var row in client1.GetAll())
				Console.WriteLine(RowDisplay(row));

			Console.WriteLine("\nCLIENT2:");
			foreach (var row in client2.GetAll())
				Console.WriteLine(RowDisplay(row));

			Console.WriteLine("\nSERVER:");
			foreach (var row in server.GetAll())
				Console.WriteLine(RowDisplay(row));


			Console.ReadLine();
		}


		private static string RowDisplay(TableSchema row)
		{
			if (row.Deleted.HasValue)
				return $"{row.Name} {row.Description} (Deleted)";
			else
				return $"{row.Name} {row.Description}";
		}
	}

}