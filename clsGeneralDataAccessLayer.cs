using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Messaging;

namespace GeneralDataAccessLayer
{
    public class clsGeneralDataAccessLayer
    {

       public string _TableName {  get; }    
       public string _IDColumnName { get; }  
       public string _ConnectionString {  get; }
        
        //Columns Names 'Keys' will be stored in small Letters
        //ititial values of empty Record = -1
       public Dictionary<string, object> ColumnsNames_Record { get; set; }

     
       public clsGeneralDataAccessLayer(string ConnectionString, string TableName, string IDColumnName)
        {
            _TableName = TableName;
            _IDColumnName = IDColumnName.ToLower();   
            _ConnectionString = ConnectionString;   

           
            GetColumnsNames();
           
        }


        int GetAnActualIdFromTable()
        {
            int ID = -1;

            SqlConnection Connection = new SqlConnection(_ConnectionString);

            string Query = "Select * from " + _TableName +";";

            SqlCommand Command = new SqlCommand(Query, Connection);

            try
            {
                Connection.Open();
                object result = Command.ExecuteScalar();

                if (result == null || !int.TryParse(result.ToString(), out ID)) 
                {
                    ID = -1;
                }

            }
            catch (Exception ex)
            {

            }
            finally
            {
                Connection.Close();
            }

            return ID;  
        }

        bool GetColumnsNames()
        {
            SqlConnection Connection = new SqlConnection(_ConnectionString);

            
            int ID = GetAnActualIdFromTable();

            if (ID == -1) 
            {
                return false;
            }
            
            string Query = $"Select * from {_TableName} Where {_IDColumnName} = {ID};";

            SqlCommand Command = new SqlCommand(Query, Connection);

            try
            {
                Connection.Open();
                SqlDataReader Reader = Command.ExecuteReader();

                if (Reader.Read())
                {
                    ColumnsNames_Record = new Dictionary<string, object>(Reader.FieldCount);

                    for (int i = 0; i < Reader.FieldCount; i++)
                    {
                        ColumnsNames_Record.Add(Reader.GetName(i).ToLower(), -1);
                    }
                }
                Reader.Close(); 

            }
            catch (Exception ex) 
            {

            }
            finally
            {
                Connection.Close();
            }

            return true;    
        }

            
         public bool FindRecordByID(int RecordID)
        {

           //Found Record will be stored in 'ColumnsNames_Record' field

            SqlConnection Connection = new SqlConnection(_ConnectionString);

            bool isRecordFound = false;

            string Query = "Select * from " + _TableName + " where " + _IDColumnName + " = @Id";

            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue("@Id", RecordID);

            try
            {

                Connection.Open();
                SqlDataReader Reader = Command.ExecuteReader();

                if (Reader.Read())
                {
                    for (int i = 0; i < Reader.FieldCount; i++)
                    {
                        ColumnsNames_Record[Reader.GetName(i).ToLower()] = Reader[i];
                    }

                    isRecordFound = true;   

                }
                Reader.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Connection.Close();
            }

            return isRecordFound;

        }


       public bool IsRecordExist(int RecordID)
        {
            object Result = null;
            SqlConnection Connection = new SqlConnection(_ConnectionString);

            string Query = "Select x=1 from " + _TableName + " Where " + _IDColumnName + " = @Id";

            SqlCommand Command = new SqlCommand(Query,Connection);
            Command.Parameters.AddWithValue("@Id", RecordID);

            try
            {
                Connection.Open();
                 Result = Command.ExecuteScalar();

            }
            catch
            {

            }
            finally
            {
                Connection.Close(); 
            }

            return Result != null ; 
        }

        ///<remarks>
        /// First you should Fill 'ColumnsNames_Record' field with New values To Update.
        /// <para> '-1' value of a column will skip updating this column </para>
        /// </remarks>
        /// <returns>true if update was successful, otherwise false.</returns>
        public bool UpdateRecord(int RecordID)
        {
            int AffectedRows = 0;

            SqlConnection Connection = new SqlConnection(_ConnectionString);

            ColumnsNames_Record[_IDColumnName] = RecordID;

            string Query = $"UPDATE {_TableName} SET ";

            bool isThereFieldToUpdate = false;

            foreach(var pair in ColumnsNames_Record)
            {
                //if a value is set as -1 , means it`s empty so skip updating Column of this value
                if ((pair.Key == _IDColumnName) || (pair.Value != null && pair.Value.ToString() == "-1")) 
                    continue;

                Query += $"{pair.Key} = @{pair.Key} ,";
                isThereFieldToUpdate = true;
                
                
               
            }

            if (isThereFieldToUpdate == false) 
            {
                return false;
            }

            //get rid of the last comma
            Query = Query.Substring(0, Query.Length - 1);
            

            Query += $" Where {_IDColumnName} = @{_IDColumnName};";

           

            SqlCommand Command = new SqlCommand(Query,Connection);

            
            foreach(var pair in ColumnsNames_Record)
            {
                if(pair.Value != null && pair.Value.ToString() == "-1")
                    continue;

                if (pair.Value != null)
                {
                    Command.Parameters.AddWithValue($"@{pair.Key}", pair.Value);
                }
                else
                {
                    Command.Parameters.AddWithValue($"@{pair.Key}", System.DBNull.Value);
                }
            }


            try 
            {

                Connection.Open();

                AffectedRows = Command.ExecuteNonQuery();

                

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Connection.Close();
            }

            return (AffectedRows > 0);
        }

        ///<remarks>
        /// First you should Fill 'ColumnsNames_Record' field with values To Add New.
        /// <para> Skip Adding a value will throw an exception. </para>
        /// </remarks>
        /// <returns>true if Adding was successful, otherwise false.</returns>
        public int AddNewRecord()
        {

            ColumnsNames_Record[_IDColumnName] = -1;


            SqlConnection connection = new SqlConnection(_ConnectionString);

            string Query = $"Insert into {_TableName} Values (";

            foreach (var pair in ColumnsNames_Record)
            {
                if (pair.Key == _IDColumnName)
                    continue;

                if (pair.Value != null && pair.Value.ToString() == "-1")
                {
                    //There is an empty value of record
                    return -1;
                }


                if (pair.Key != ColumnsNames_Record.Last().Key)
                    Query += $"@{pair.Key} ,";
                else
                    Query += $"@{pair.Key}) ;";
            }

            Query += "Select Scope_Identity();";


            SqlCommand Command = new SqlCommand(Query, connection);

            foreach (var pair in ColumnsNames_Record)
            {
                if (pair.Value != null)
                {
                    Command.Parameters.AddWithValue($"@{pair.Key}", pair.Value);
                }
                else
                {
                    Command.Parameters.AddWithValue($"@{pair.Key}", System.DBNull.Value);
                }
            }

            try
            {
                connection.Open();
                object result = Command.ExecuteScalar();

                if (result != null && int.TryParse(result.ToString(), out int InsertedID))
                {
                    ColumnsNames_Record[_IDColumnName] = InsertedID;
                }


            }
            catch
            {
               
            }
            finally
            {
                connection.Close();
            }

            return (int)ColumnsNames_Record[_IDColumnName];
        }


        public bool DeleteRecord(int RecordID)
        {
            int AffectedRows = 0;

            SqlConnection Connection = new SqlConnection(_ConnectionString);

            string Query = $@"Delete From {_TableName}
                               WHERE {_IDColumnName} = @{_IDColumnName};";

            SqlCommand Command = new SqlCommand(Query, Connection);
            Command.Parameters.AddWithValue($"@{_IDColumnName}", RecordID);

            try
            {
                Connection.Open();
                AffectedRows = Command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                Connection.Close();
            }

            return (AffectedRows > 0);
            

        }

        public DataTable GetAllRecords()
        {
            DataTable dt = new DataTable();

            SqlConnection Connection = new SqlConnection(_ConnectionString);

            string Query = $"Select * from {_TableName}";
            SqlCommand Command = new SqlCommand(Query, Connection);

            try
            {
                Connection.Open();
                SqlDataReader Reader = Command.ExecuteReader();

                if(Reader.HasRows)
                {
                    dt.Load(Reader);
                }

                Reader.Close();
            }
            catch(Exception ex)
            {

            }
            finally
            {
                Connection.Close(); 
            }

            return dt;

        }

       
    }
}
