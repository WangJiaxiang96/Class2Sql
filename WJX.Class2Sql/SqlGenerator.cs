using System;
using System.Collections.Generic;
using System.Reflection;

namespace WJX.Class2Sql
{    
    /// <summary>
    /// 将指定类的属性生成各种Sql语句
    /// </summary>
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
        /// <summary>
        /// 指定创建表时的表名，默认为指定类的类名
        /// </summary>
        public string TableName
        {
            get => _tableName; set
            {
                if (value == null)
                {
                    _tableName = ClassType.Name;
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

        /// <summary>
        /// 设置指定类，不能为null
        /// </summary>
        public object TargetObject
        {
            get { return _targetObject; }
            set
            {
                if (_targetObject == null)
                {
                    throw new NullReferenceException("\"TargetObject\" is NULL, please set an instance of class");
                }
                //设置Type
                if (value != null)
                {
                    ClassType = value.GetType();
                }
                _targetObject = value;
            }
        }

        /// <summary>
        /// 生成Sql语句时忽略的属性的名，默认为null，即使用全部属性生成Sql语句
        /// </summary>
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
        /// <summary>
        /// 初始化SqlGenerator
        /// </summary>
        /// <param name="targetClass">指定一个对象，将根据此对象的属性生成Sql，不能为null</param>
        public SqlGenerator(object targetClass) : this(targetClass, null,null)
        {
        }
        /// <summary>
        /// 初始化SqlGenerator
        /// </summary>
        /// <param name="targetObject">指定一个对象，将根据此对象的属性生成Sql，不能为null</param>
        /// <param name="tableName">指定创建表时的表名，默认为指定类的类名</param>
        /// <param name="ignoredProperties">生成Sql语句时忽略的属性的名，默认为null，即使用全部属性生成Sql语句</param>
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
        /// <summary>
        /// 使用属性名生成 CreateTable创建表 的sql
        /// </summary>
        /// <param name="If_Not_Exists">如果存在该表则不创建</param>
        /// <returns></returns>
        public string CreateTable(bool If_Not_Exists=true)
        {
            // todo: 支持设置主键
            // todo: 生成的sql末端包含多余的","，未检查是否允许
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
        /// <summary>
        /// 使用属性名生成 Insert插入 的sql
        /// </summary>
        /// <param name="On_Duplicate_Key_Update">根据主键判断，该条数据如果不存在则插入，存在则更新</param>
        /// <param name="useParameters">是否在Sql中使用 @PropertyName 占位符</param>
        /// <returns></returns>
        public string InsertInto(bool On_Duplicate_Key_Update = true, bool useParameters = true)
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
                    sqlValue += "@" + name + ",";
                    sqlColumnValue += name + "=@" + name + ",";
                }
            }
            else
            {
                // todo: 测试可行性
                foreach (var name in TargetProperties)
                {
                    Type type = GetValueType(name);
                    tempValue = GetValue(name);

                    sqlColumn += name + ",";
                    sqlColumnValue += name + "=";

                    if (type.Name == "string" || type.Name == "char")
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
        /// <summary>
        /// 根据<c>MySqlCommand</c>实例生成给Sql语句中占位符赋值的C#代码，若需忽略某些属性，请预先设置<c>IgnoredProperties</c>
        /// </summary>
        /// <example>
        /// <code>
        ///  ...
        ///  
        ///  SqlGenerator sqlGenerator=new SqlGenerator(SomeClass);
        ///  MySqlCommand cmd=new ...;
        ///  string code=sqlGenerator.GenerateCodeForMySql("cmd");
        ///  //在此处添加断点，将变量code的值复制，停止调试，然后把代码粘贴在下方，最后删除上一条语句。
        ///  
        ///  ...
        ///  
        /// </code>
        /// </example>
        /// <param name="MySqlCommandInstance">MySqlCommand实例的名</param>
        /// <returns></returns>
        public string GenerateCodeForMySql(string MySqlCommandInstance)
        {
            string code = "";
            foreach (var name in TargetProperties)
            {
                code += $"{MySqlCommandInstance}.Parameters.AddWithValue(\"@{name}\", {name}); ";
            }
            return code;
        }

        private string GetValue(string propertyName)
        {
            return (string)GetProperty(propertyName).GetValue(TargetObject);
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
