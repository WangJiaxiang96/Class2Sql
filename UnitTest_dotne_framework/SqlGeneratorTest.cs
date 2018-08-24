using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WJX.Class2Sql;
using UnitTest_dotne_framework.Model;

namespace UnitTest_dotne_framework
{
    [TestClass]
    public class SqlGeneratorTest
    {
        public TestModel model { get; set; } = new TestModel();
        public SqlGenerator sqlGenerator { get; set; }

        [TestMethod]
        public void TestMethod1()
        {
            sqlGenerator = new SqlGenerator(model);
            string sqlTable=sqlGenerator.CreateTable(true);
            Console.WriteLine(sqlTable);
            Assert.IsNotNull(sqlTable);

            string sqlInsert = sqlGenerator.InsertInto();
            Console.WriteLine(sqlInsert);
            Assert.IsNotNull(sqlInsert);

        }
    }
}
