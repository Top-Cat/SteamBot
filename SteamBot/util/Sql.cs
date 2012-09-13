using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;

namespace SteamBot.util {
    public class Sql {
		private MySqlConnection myConnection;
		private string lck = "";

		public Sql() {
			myConnection = new MySqlConnection("");
			myConnection.Open();
		}

		private MySqlCommand createCommand(string sql) {
			return new MySqlCommand(sql, myConnection);
		}

		public int update(string sql) {
			return createCommand(sql).ExecuteNonQuery();
		}

		public List<object[]> query(string sql) {
			lock (lck) {
				MySqlDataReader lastReader = createCommand(sql).ExecuteReader();
				List<object[]> response = new List<object[]>();
				while (lastReader.Read()) {
					object[] row = new object[lastReader.FieldCount];
					for (int i = 0; i < lastReader.FieldCount; i++) {
						row[i] = lastReader.GetValue(i);
					}
					response.Add(row);
				}
				lastReader.Close();
				return response;
			}
		}
    }
}
