using System;
using System.Data.SqlClient;

namespace WebScraper
{
    public static class Sql
    {
        static string _connectionString = "Server=localhost; Integrated Security = True";
        static string _dbName = "TimberBusiness";
        static string _tableName = "Deals";

        public static bool DatabaseIsCreated()
        {
            bool result = false;

            string command = $"Select * From dbo.Sysdatabases Where name='{_dbName}'";
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand sqlCommand = new SqlCommand(command, connection))
                {
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        result = reader.HasRows;
                    }
                }
            }

            return result;
        }

        public static void CreateDatabase()
        {
            string createDbCmd = $"Create Database {_dbName}";
            string createTableCmd = $"Create Table {_dbName}.dbo.{_tableName} (" +
                $"DealNumber NVARCHAR(28) PRIMARY KEY," +
                $"SellerName NVARCHAR(300) NOT NULL," +
                $"SellerInn NVARCHAR(12) NOT NULL," +
                $"BuyerName NVARCHAR(300) NOT NULL," +
                $"BuyerInn NVARCHAR(12) DEFAULT ''," +
                $"DealDate DATE NOT NULL," +
                $"WoodVolumeSeller DECIMAL(18,1) DEFAULT 0.0," +
                $"WoodVolumeBuyer DECIMAL(18,1) DEFAULT 0.0" +
                $");";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand sqlCreateDb = new SqlCommand(createDbCmd, connection))
                {
                    sqlCreateDb.ExecuteNonQuery();
                }
                using (SqlCommand sqlCreateTable = new SqlCommand(createTableCmd, connection))
                {
                    sqlCreateTable.ExecuteNonQuery();
                }
            }
        }

        public static DealModel GetDealById(string id)
        {
            DealModel result = null;

            string getByIdCmd = $"SELECT * FROM {_dbName}.dbo.{_tableName} " +
                $"WHERE DealNumber=@Id";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand sqlGetById = new SqlCommand(getByIdCmd, connection))
                {
                    sqlGetById.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader reader = sqlGetById.ExecuteReader())
                    {
                        reader.Read();
                        result = new DealModel
                        {
                            DealNumber = (string)reader["DealNumber"],
                            SellerName = (string)reader["SellerName"],
                            SellerInn = (string)reader["SellerInn"],
                            BuyerName = (string)reader["BuyerName"],
                            BuyerInn = (string)reader["BuyerInn"],
                            DealDate = (DateTime)reader["DealDate"],
                            WoodVolumeSeller = (decimal)reader["WoodVolumeSeller"],
                            WoodVolumeBuyer = (decimal)reader["WoodVolumeBuyer"]
                        };
                    }
                }
            }

            return result;
        }

        public static bool DealExists(string id)
        {
            bool result = false;

            string getByIdCmd = $"SELECT * FROM {_dbName}.dbo.{_tableName} " +
                $"WHERE DealNumber=@Id";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand sqlGetById = new SqlCommand(getByIdCmd, connection))
                {
                    sqlGetById.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader reader = sqlGetById.ExecuteReader())
                    {
                        result = reader.HasRows;
                    }
                }
            }

            return result;
        }

        public static void CreateDeal(DealModel deal)
        {
            string insertCmd = $"INSERT INTO {_dbName}.dbo.{_tableName} " +
                $"VALUES(@DealNumber, @SellerName, @SellerInn, @BuyerName, @BuyerInn, @DealDate, @WoodVolumeSeller, @WoodVolumeBuyer)";


            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand sqlInsert = new SqlCommand(insertCmd, connection))
                {
                    sqlInsert.Parameters.AddWithValue("@DealNumber", deal.DealNumber);
                    sqlInsert.Parameters.AddWithValue("@SellerName", deal.SellerName);
                    sqlInsert.Parameters.AddWithValue("@SellerInn", deal.SellerInn);
                    sqlInsert.Parameters.AddWithValue("@BuyerName", deal.BuyerName);
                    sqlInsert.Parameters.AddWithValue("@BuyerInn", deal.BuyerInn);
                    sqlInsert.Parameters.AddWithValue("@DealDate", deal.DealDate);
                    sqlInsert.Parameters.AddWithValue("@WoodVolumeSeller", deal.WoodVolumeSeller);
                    sqlInsert.Parameters.AddWithValue("@WoodVolumeBuyer", deal.WoodVolumeBuyer);
                    sqlInsert.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateDeal(DealModel deal)
        {
            string updateCmd = $"UPDATE {_dbName}.dbo.{_tableName} " +
                $"SET SellerName=@SellerName, SellerInn=@SellerInn, BuyerName=@BuyerName, BuyerInn=@BuyerInn, DealDate=@DealDate, WoodVolumeSeller=@WoodVolumeSeller, WoodVolumeBuyer=@WoodVolumeBuyer " +
                $"WHERE DealNumber=@DealNumber";


            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand sqlUpdate = new SqlCommand(updateCmd, connection))
                {
                    sqlUpdate.Parameters.AddWithValue("@DealNumber", deal.DealNumber);
                    sqlUpdate.Parameters.AddWithValue("@SellerName", deal.SellerName);
                    sqlUpdate.Parameters.AddWithValue("@SellerInn", deal.SellerInn);
                    sqlUpdate.Parameters.AddWithValue("@BuyerName", deal.BuyerName);
                    sqlUpdate.Parameters.AddWithValue("@BuyerInn", deal.BuyerInn);
                    sqlUpdate.Parameters.AddWithValue("@DealDate", deal.DealDate);
                    sqlUpdate.Parameters.AddWithValue("@WoodVolumeSeller", deal.WoodVolumeSeller);
                    sqlUpdate.Parameters.AddWithValue("@WoodVolumeBuyer", deal.WoodVolumeBuyer);
                    sqlUpdate.ExecuteNonQuery();
                }
            }
        }

        public static void DropDatabase()
        {
            string dropCmd = $"DROP DATABASE {_dbName}";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand sqlDropDb = new SqlCommand(dropCmd, connection))
                {
                    sqlDropDb.ExecuteNonQuery();
                }
            }
        }
    }
}
