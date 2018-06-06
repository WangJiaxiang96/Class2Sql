using System;
using System.Collections.Generic;
using System.Reflection;

namespace WJX.Class2Sql
{
    public class SqlGenerator
    {
        #region Fields        
        private List<string> _allProperties;
        private Type _classType;
        private object _targetObject;
        private List<string> _ignoredProperties;
        private List<string> _targetProperties;
        private string _tableName;
        #endregion

        #region Properties      
        public string TableName
        {
            get => _tableName; set
            {
                if (value == null)
                {
                    _tableName = _classType.Name;
                }
                else
                {
                    _tableName = value;
                }
            }
        }

        private Type ClassType
        {
            get { return _classType; }
            set
            {
                //获取Type所有属性
                if (value != null)
                {
                    AllProperties = new List<string>();
                    foreach (var property in value.GetProperties())
                    {
                        AllProperties.Add(property.Name);
                    }
                    GenerateTargetProperties();
                }
                _classType = value;
            }
        }

        public object TargetObject
        {
            get { return _targetObject; }
            set
            {
                //设置Type
                if (value != null)
                {
                    ClassType = value.GetType();
                }
                _targetObject = value;
            }
        }

        public List<string> IgnoredProperties
        {
            get => _ignoredProperties;
            set
            {
                GenerateTargetProperties();
                _ignoredProperties = value;
            }
        }

        private List<string> TargetProperties { get => _targetProperties; set => _targetProperties = value; }

        private List<string> AllProperties { get => _allProperties; set => _allProperties = value; }
        #endregion

        #region Contructors
        public SqlGenerator(object targetClass) : this(targetClass, null)
        {
        }

        public SqlGenerator(object targetClass, string tableName) : this(targetClass, tableName, null)
        {
        }

        public SqlGenerator(object targetObject, string tableName, List<string> ignoredProperties)
        {
            AllProperties = null;
            ClassType = null;
            TargetProperties = null;

            IgnoredProperties = ignoredProperties;
            TargetObject = targetObject;
            //为了防止TableName为null的情况，必须将它在TargetObject后初始化
            TableName = tableName;
        }
        #endregion

        public string CreateTable(bool If_Not_Exists, bool SkipIgnoredProperties)
        {
            // todo: 支持设置主键
            string sql = $"CREATE TABLE {(If_Not_Exists ? "IF NOT EXISTS" : "")} {TableName} (";
            string temp = null;
            foreach (var name in TargetProperties)
            {
                Type type = GetValueType(name);
                switch (type.Name)
                {
                    case "string":
                        temp = $"{name} varchar(100),";
                        break;
                    case "int":
                        temp = $"{name} int,";
                        break;
                    case "double":
                        temp = $"{name} double,";
                        break;
                    case "float":
                        temp = $"{name} float,";
                        break;
                    case "char":
                        temp = $"{name} char,";
                        break;
                    default:
                        temp = $"{name} varchar(100),";
                        break;
                }
                sql += temp;
            }
            sql += ");";
            return sql;
        }

        public string InsertInto(bool On_Duplicate_Key_Update=true, bool useParameters=true)
        {
            string sql = $"INSERT INTO {TableName} ";
            string tempValue = "";
            string sqlColumn = "";
            string sqlValue = "";
            string sqlColumnValue = "";

            if (useParameters)
            {
                foreach (var name in TargetProperties)
                {
                    sqlColumn += name + ",";
                    sqlValue += "@" + name+",";
                    sqlColumnValue += name + "=@"+name+",";
                }
            }
            else
            {
                foreach (var name in TargetProperties)
                {
                    Type type = GetValueType(name);
                    tempValue = GetValue(name);

                    sqlColumn += name + ",";
                    sqlColumnValue += name + "=";

                    if (type.Name == "string"||type.Name=="char")
                    {
                        sqlValue += $"'{tempValue}',";
                        sqlColumnValue += $"'{tempValue}',";
                    }
                    else
                    {
                        sqlValue += $"{tempValue},";
                        sqlColumnValue += $"{tempValue},";
                    }
                }
            }

            sqlColumn = sqlColumn.TrimEnd(',');
            sqlValue = sqlValue.TrimEnd(',');            

            sql += $"({sqlColumn}) VALUES({sqlValue}) ";
            if (On_Duplicate_Key_Update)
            {
                sqlColumnValue = sqlColumnValue.TrimEnd(',');
                sql += $"ON DUPLICATE KEY UPDATE {sqlColumnValue}";
            }
            sql += ";";
            return sql;
        }

        private string GetValue(string propertyName)
        {
            return (string) GetProperty(propertyName).GetValue(TargetObject);
        }

        private Type GetValueType(string propertyName)
        {
            return GetProperty(propertyName).GetType();
        }

        private PropertyInfo GetProperty(string propertyName)
        {
            return ClassType.GetProperty(propertyName);
        }

        private void GenerateTargetProperties()
        {
            if (IgnoredProperties == null)
            {
                TargetProperties = AllProperties;
            }
            else if (AllProperties != null)
            {
                TargetProperties = AllProperties.FindAll(
                    (x) =>
                    {
                        return !IgnoredProperties.Contains(x);
                    });
            }
            else
            {
                TargetProperties = null;
            }
        }
    }
}
