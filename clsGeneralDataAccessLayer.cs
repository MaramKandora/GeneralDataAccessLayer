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

        string _TableName {  get; }    
        string _IDColumnName { get; }  
        string _ConnectionString {  get; }
        
        //Fields Keys will be stored in small Letters
       public Dictionary<string, object> Fields { get; set; }

     
       public clsGeneralDataAccessLayer(string ConnectionString, string TableName, string IDColumnName)
        {
            _TableName = TableName;
            _IDColumnName = IDColumnName.ToLower();   
            _ConnectionString = ConnectionString;   

            //ititial values of empty fields is -1
            UploadFields();
           
        }


        int GetAnActualIdFromTable()
        {
            int ID = -1;

            SqlConnection Connection = new SqlConnection(_ConnectionString);

            //Get names of fields
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

        bool UploadFields()
        {
            SqlConnection Connection = new SqlConnection(_ConnectionString);

            //Get names of fields
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
                    Fields = new Dictionary<string, object>(Reader.FieldCount);

                    for (int i = 0; i < Reader.FieldCount; i++)
                    {
                        Fields.Add(Reader.GetName(i).ToLower(), -1);
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

            
         public Dictionary<string, object> FindRecordByID(int RecordID)
        {
            

            Dictionary<string, object> RecordValues = new Dictionary<string, object>();

            SqlConnection Connection = new SqlConnection(_ConnectionString);

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
                        RecordValues[Reader.GetName(i)] = Reader[i];
                    }

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

            return RecordValues;

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

        public bool UpdateRecord(int RecordID)
        {
           //first you should fill Fields with values to update
            int AffectedRows = 0;

            SqlConnection Connection = new SqlConnection(_ConnectionString);

            Fields[_IDColumnName] = RecordID;

            string Query = $"UPDATE {_TableName} SET ";

            bool isThereFieldToUpdate = false;

            foreach(var pair in Fields)
            {
                //if value is set as -1 , means field is empty so skip updating it
                if ((pair.Key == _IDColumnName) || (pair.Value.ToString() == "-1")) 
                    continue;
                
                if (pair.Key != Fields.Last().Key)
                {
                    isThereFieldToUpdate = true;
                    Query += $"{pair.Key} = @{pair.Key} ,";
                }
               
            }

            //get rid of the last comma
            Query = Query.Substring(0, Query.Length - 1);
            

            Query += $" Where {_IDColumnName} = @{_IDColumnName};";

            if (isThereFieldToUpdate == false) 
            {
                return false;
            }

            SqlCommand Command = new SqlCommand(Query,Connection);

            
            foreach(var pair in Fields)
            {
                if(pair.Value.ToString()=="-1")
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

        public int AddNewRecord()
        {
            //first you should Fill Fields with Values of New Record

            Fields[_IDColumnName] = -1;


            SqlConnection connection = new SqlConnection(_ConnectionString);

            string Query = $"Insert into {_TableName} Values (";

            foreach (var pair in Fields)
            {
                if (pair.Key == _IDColumnName)
                    continue;

                if (pair.Value != null && pair.Value.ToString() == "-1")
                {
                    //There is an empty Field
                    return -1;
                }


                if (pair.Key != Fields.Last().Key)
                    Query += $"@{pair.Key} ,";
                else
                    Query += $"@{pair.Key}) ;";
            }

            Query += "Select Scope_Identity();";


            SqlCommand Command = new SqlCommand(Query, connection);

            foreach (var pair in Fields)
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
                    Fields[_IDColumnName] = InsertedID;
                }


            }
            catch
            {
               
            }
            finally
            {
                connection.Close();
            }

            return (int)Fields[_IDColumnName];
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
