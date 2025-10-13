using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeneralDataAccessLayer;

namespace GeneralBusinessLayer
{
    public class clsGeneralBusinessLayer
    {
        clsGeneralDataAccessLayer DataAccessObject;

        //Columns Names 'Keys' will be stored in small Letters
        //ititial values of empty Record = -1
        public Dictionary<string, object> ColumnsNames_Record { get; set; }
        enum enMode { AddNew,Update}
        enMode _Mode;

        public clsGeneralBusinessLayer(string ConnectionString, string TableName, string IDColumnName)
        {
            this.DataAccessObject = new clsGeneralDataAccessLayer(ConnectionString, TableName, IDColumnName);

            ColumnsNames_Record = DataAccessObject.ColumnsNames_Record;

            _Mode = enMode.AddNew;

        }

        

        public bool Find(int RecordID)
        {
           

            if (DataAccessObject.FindRecordByID(RecordID))
            {
                ColumnsNames_Record = DataAccessObject.ColumnsNames_Record;   
                _Mode = enMode.Update;
                return true;
            }
            else
            {
                return false;
            }
          

        }

        private bool _AddNewRecord()
        {

            return DataAccessObject.AddNewRecord() != -1;
        }

        private bool _UpdateRecord()
        {
            int ID = (int)this.DataAccessObject.ColumnsNames_Record[DataAccessObject._IDColumnName.ToLower()];

            return DataAccessObject.UpdateRecord(ID);

        }


        public bool Save()
        {
            switch (_Mode)
            {
                case enMode.AddNew:

                    if (_AddNewRecord())
                    {
                        _Mode = enMode.Update;
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case enMode.Update:
                    return _UpdateRecord();


                default:
                    return false;

            }
        }

        public bool DeleteRecord(int ID)
        {
            return DataAccessObject.DeleteRecord(ID);
        }

        public bool IsRecordExist(int ID)
        {
            return DataAccessObject.IsRecordExist(ID);
        }

        public DataTable GetAllRecords()
        {
            return DataAccessObject.GetAllRecords();
        }

        
    }
}
