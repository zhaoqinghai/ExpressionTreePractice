using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ExpressionUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionUtilTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var str =
                "$.Age >= 1 && $.StartDate >= '2020-01-01' && !$.IsMale || $.Name != 'zqh' && $.Name != 'zqh1' || $.Hobby in ('football', 'baseball')";
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000000; i++)
            {
                var list = new string() ;

                //var model = new QueryableExpressionHelper.BlockEngine(str).GetClauseGroup();
            }

            var time = sw.ElapsedMilliseconds;
        }
    }

   
}
