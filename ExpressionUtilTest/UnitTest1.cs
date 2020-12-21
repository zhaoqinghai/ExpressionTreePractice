using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExpressionUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace ExpressionUtilTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {


            var str =
                "($.Age >= 1 && $.StartDate >= '  2020-01-01  asd  &&  ') && ((!$.IsMale || $.Name != 'zqh') && $.Name != 'zqh1') || $.Hobby in ('football','baseball')";
            Stopwatch sw = Stopwatch.StartNew();
            Parallel.For(0, 1, i =>
            {
               
                //var a = System.Text.Json.JsonSerializer.Serialize(new {a = 1, b = "zqh"});
                //var a = JsonConvert.SerializeObject(new {a = 1, b = "zqh"});
                var a = new String2ClauseEngine().GetClauseGroup(str);
            });
            var time = sw.ElapsedMilliseconds;
        }

        [TestMethod]
        public void Test2()
        {
            var a = new Dictionary<string, dynamic>()
            {
                {"a",1},{"b",DateTime.Now},{"c", Sex.Male},{"d",Guid.NewGuid()},{"e","abc"},{"f",null}
            };
            var b = SerializeObject(a);
        }

        private enum Sex
        {
            Male,
            Female
        }

        private string SerializeObject(Dictionary<string, dynamic> dict)
        {
            var result = "{";
            foreach (var item in dict)
            {
                result = string.Concat(result, "\"", item.Key, "\":");
                if (item.Value == null)
                {
                    result = string.Concat(result, "null,");
                    continue;
                }

                if (!(item.Value is ValueType))
                {
                    result = string.Concat(result, "\"", item.Value, "\",");
                    continue;
                }

                if (item.Value is Enum)
                {
                    result = string.Concat(result, (int) item.Value, ",");
                    continue;
                }

                if (item.Value is Guid)
                {
                    result = string.Concat(result, "\"", item.Value, "\",");
                    continue;
                }

                if (item.Value is DateTime || item.Value is DateTimeOffset)
                {
                    result = string.Concat(result, "\"", item.Value.ToString("yyyy-MM-dd HH:mm:ss"), "\",");
                    continue;
                }

                result = string.Concat(result, item.Value, ",");
            }

            result = result.Remove(result.Length - 1, 1);
            result += "}";
            return result;
        }
    }


}
