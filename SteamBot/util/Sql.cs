using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;

namespace SteamBot.util {
	public class Sql {
		private MySqlConnection myConnection;
		private static string lck = "";

		public Sql() {
			myConnection = new MySqlConnection("");
			checkConnection();
		}

		private MySqlCommand createCommand(string sql, Dictionary<string, object> pars) {
			MySqlCommand cmd = new MySqlCommand(sql, myConnection);
			if (pars != null) {
				foreach (string key in pars.Keys) {
					cmd.Parameters.AddWithValue(key, pars[key]);
				}
			}
			return cmd;
		}

		public int update(string sql, Dictionary<string, object> pars = null) {
			lock (lck) {
				checkConnection();
				MySqlCommand cmd = createCommand(sql, pars);
				return cmd.ExecuteNonQuery();
			}
		}

		public int insert(string sql, Dictionary<string, object> pars = null) {
			lock (lck) {
				checkConnection();
				MySqlCommand cmd = createCommand(sql, pars);
				int aff = cmd.ExecuteNonQuery();
				return (int) cmd.LastInsertedId;
			}
		}

		private void checkConnection() {
			while (myConnection.State != System.Data.ConnectionState.Open) {
				Thread.Sleep(1000);
				try {
					myConnection.Open();
				} catch (Exception) {
				}
			}
		}

		public List<object[]> query(string sql, Dictionary<string, object> pars = null) {
			lock (lck) {
				checkConnection();
				MySqlDataReader lastReader = createCommand(sql, pars).ExecuteReader();
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
