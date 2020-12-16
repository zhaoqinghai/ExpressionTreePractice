using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace ExpressionUtil
{
    public static class QueryableExpressionHelper
    {
        public static IQueryable<T> GetQuery<T>(this IQueryable<T> source, QueryClause query)
        {
            var groupList = query.Conditions.GroupBy(_ => _.Group).OrderBy(_ => _.Key);
            Expression<Func<T, bool>> complete = null;
            foreach (var group in groupList)
            {
                var param = Expression.Parameter(typeof(T), "entity");
                foreach (var condition in group)
                {
                    var left = Expression.Property(param, condition.Field);
                    var right = Expression.Constant(condition.Value);
                    var binary = UseCondition(condition.Operator, left, right);
                    var partExpression = Expression.Lambda<Func<T, bool>>(binary, param);

                    if (complete == null)
                    {
                        complete = partExpression;
                    }
                    else
                    {
                        var parameter = Expression.Parameter(typeof(T), "entity");
                        SetParamExpressionVisitor visitor = new SetParamExpressionVisitor(parameter);
                        var newExp1 = visitor.Modify(complete.Body);
                        var newExp2 = visitor.Modify(partExpression.Body);
                        complete = Expression.Lambda<Func<T, bool>>(Expression.AndAlso(newExp1, newExp2), parameter);
                    }
                }
            }
            var completeParam = Expression.Parameter(typeof(IQueryable<T>), "entity");

            
            //SetParamExpressionVisitor completeVisitor = new SetParamExpressionVisitor(completeParam);
            //var exp = completeVisitor.Modify(complete?.Body);
            var queryExp = Expression.Call(typeof(Queryable), "Where", new Type[] {typeof(T)}, source.Expression, complete);
            var exp = Expression.Lambda<Func<IQueryable<T>, IQueryable<T>>>(queryExp, Expression.Parameter(typeof(IQueryable<T>)))
                .Compile();
            var result = exp(source);
            return result;
        }
        //$.Age >= 1 && $.StartDate >= '2020-01-01' && !$.IsMale || ($.Name != 'zqh' && $.Name != 'zqh1') || $.Hobby in ('football', 'baseball')
       

        public class Operators
        {
            public static HashSet<string> Collection = new HashSet<string>()
            {
                None, Equal, NotEqual, LessThan, LessEqualThan, GreaterThan, GreaterEqualThan, In
            };
            public const string None = "";
            public const string Equal = "=";
            public const string NotEqual = "!=";
            public const string LessThan = "<";
            public const string LessEqualThan = "<=";
            public const string GreaterThan = ">";
            public const string GreaterEqualThan = ">=";
            public const string In = "in";
        }

        public class Relations
        {
            public const string None = "";
            public const string And = "&&";
            public const string Or = "||";
            public const string Not = "!";
        }

        

        public class ClauseGroup
        {
            //public ClauseGroup Parent { get; }

            //public ClauseGroup(ClauseGroup group)
            //{
            //    Parent = group;
            //}

            //public string Left { get; set; }

            //public string Right { get; set; }

            public List<string> Collection { get; set; } = new List<string>();

            public List<Relation> Relation { get; set; } = new List<Relation>();
        }

        public class BlockEngine
        {
            private readonly string _block;
            /// <summary>
            /// false r true l
            /// </summary>
            private readonly Dictionary<int, bool> _dict = new Dictionary<int, bool>();
            public BlockEngine(string block)
            {
                block = Regex.Replace(block, @"\s", "");
                var sb = new StringBuilder();
                var i = 0;
                while (i < block.Length)
                {
                    //if ('(' == block[i])
                    //{
                    //    _dict.Add(sb.Length, true);
                    //    continue;
                    //}
                    //if (')' == block[i])
                    //{
                    //    _dict.Add(sb.Length, false);
                    //    continue;
                    //}
                    sb.Append(block[i]);
                    i++;
                }
                _block = sb.ToString();
            }

            

            public ClauseGroup GetClauseGroup()
            {
                var i = 0;
                var clauseGroup = new ClauseGroup();
                while (i < _block.Length)
                {
                    var j = i;
                    while (j < _block.Length)
                    {
                        if (_block[j] == '&')
                        {
                            clauseGroup.Collection.Add(_block[i..(j)]);
                            clauseGroup.Relation.Add(Relation.And);
                            goto A;
                        }
                        if (_block[j] == '|')
                        {
                            clauseGroup.Collection.Add(_block[i..(j)]);
                            clauseGroup.Relation.Add(Relation.Or);
                            goto A;
                        }
                        j++;
                    }
                    clauseGroup.Collection.Add(_block[i.._block.Length]);
                    A: ;
                    i = j + 2;
                }

                return clauseGroup;
            }
        }


        public class Condition : Expression
        {
            public Expression Left { get; set; }

            public Expression Right { get; set; }

            public string Operator { get; set; }
        }


        private static Expression UseCondition(Operator @operator, Expression left, Expression right)
        {
            switch (@operator)
            {
                case Operator.Equal:
                    return Expression.Equal(left, right);
                case Operator.GreaterThan:
                    return Expression.GreaterThan(left, right);
                case Operator.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(left, right);
                case Operator.In:
                    return Expression.Constant(1);
                case Operator.LessThan:
                    return Expression.LessThan(left, right);
                case Operator.LessThanOrEqual:
                    return Expression.LessThanOrEqual(left, right);
                case Operator.Include:
                    return Expression.Constant(1);
                case Operator.NotInclude:
                    return Expression.Constant(1);
                case Operator.NotEqual:
                    return Expression.NotEqual(left, right);
                case Operator.NotIn:
                    return Expression.Constant(1);
            }
            return Expression.Constant(1);
        }
    }

    public abstract class BaseClause<T>
    {
        public List<T> Conditions { get; set; }
    }

    public class QueryClause : BaseClause<QueryCondition>
    {
    }

    public class SortClause : BaseClause<SortCondition>
    {
    }

    public class QueryCondition
    {
        public string Field { get; set; }
        public object Value { get; set; }
        public Operator Operator { get; set; }
        public Relation Relation { get; set; }
        public int Group { get; set; }
        public Operator GroupOperator { get; set; }
    }

    public enum Operator
    {
        Equal = 0,
        NotEqual = 1,
        LessThan = 2,
        LessThanOrEqual = 3,
        GreaterThan = 4,
        GreaterThanOrEqual = 5,
        Include = 6,
        NotInclude = 7,
        In = 8,
        NotIn = 9
    }

    public enum Relation
    {
        And = 0,
        Or = 1,
        Not = 2
    }

    public class SortCondition
    {
        public string Field { get; set; }

        public Sort Sort { get; set; }
    }

    public enum Sort
    {
        Asc,
        Desc
    }

    public class SetParamExpressionVisitor : ExpressionVisitor
    {
        private ParameterExpression Parameter { get; set; }
        public SetParamExpressionVisitor() { }
        public SetParamExpressionVisitor(ParameterExpression parameter)
        {
            this.Parameter = parameter;
        }
        public Expression Modify(Expression exp)
        {
            return this.Visit(exp);
        }
        protected override Expression VisitParameter(ParameterExpression parameter)
        {
            return this.Parameter;
        }
    }
}
