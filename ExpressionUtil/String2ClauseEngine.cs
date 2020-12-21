using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ExpressionUtil
{
    public class String2ClauseEngine
    {
        private static readonly HashSet<char> SpaceSet = new HashSet<char>(){' ', '\t', '\n'};
        private static readonly HashSet<string> OperatorKeyWordSet = new HashSet<string>()
        {
            "<","=","!=",">","<=",">=","in","In","iN","IN"
        };
        private static readonly HashSet<string> RelationKeyWord = new HashSet<string>()
        {
            "&&","||"
        };
        public enum ProcessState
        {
            Ready,
            ConditionLeft,
            ConditionOperator,
            ConditionRight,
            Complete
        }
        public ClauseGroup GetClauseGroup(string block)
        {
            var span = block.AsSpan();
            var result = new ClauseGroup();
            var currentGroup = result;
            var startIndex = 0;
            while (startIndex < span.Length)
            {
                var word = GetWord(span, false, ref startIndex, out var state, out var keyword);
                if (word == "(")
                {
                    var group = new ClauseGroup() { Parent = currentGroup };
                    currentGroup.ClauseQueue.Enqueue(group);
                    currentGroup = group;
                    continue;
                }
                else if(word == ")")
                {
                    currentGroup = currentGroup.Parent;
                }
                var group1 = new ClauseGroup() { Parent = currentGroup };
                currentGroup.ClauseQueue.Enqueue(group1);
                group1.IsCondition = true;
                var currentCondition = group1.Condition = new Condition();
                currentCondition.Left = word;
                if (state == ReadPhraseState.Operator)
                {
                    currentCondition.Operator = TryParseOperator(keyword);
                    currentCondition.IsSingle = false;
                    
                    var rightWord = GetWord(span, true, ref startIndex, out var state1, out var keyword1);
                    currentCondition.Right = rightWord;
                    if (!string.IsNullOrEmpty(keyword1))
                    {
                        currentGroup.RelationQueue.Enqueue(TryParseRelation(keyword1));
                    }
                }
                else
                {
                    currentCondition.IsSingle = true;
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        currentGroup.RelationQueue.Enqueue(TryParseRelation(keyword));
                    }
                }
            }
            return result;
        }

        private string GetWord(ReadOnlySpan<char> span, bool isIn, ref int startIndex, out ReadPhraseState state, out string keyword)
        {
            if (!isIn && (span[startIndex] == '(' || span[startIndex] == ')'))
            {
                state = span[startIndex] == '(' ? ReadPhraseState.Left : ReadPhraseState.Right;
                startIndex++;
                keyword = string.Empty;
                return span[startIndex - 1].ToString();
            }
            var currentIndex = startIndex;
            var wordStartIndex = startIndex;
            var inQuote = false;
            string result;
            while (currentIndex < span.Length)
            {
                if (span[currentIndex]=='\'')
                {
                    inQuote = !inQuote;
                }
                
                if (inQuote)
                {
                    currentIndex++;
                    continue;
                }

                if (span[currentIndex] == ')')
                {
                    state = ReadPhraseState.Right;
                    keyword = string.Empty;
                    result = span.Slice(startIndex, currentIndex - startIndex - 1).ToString();
                    startIndex = currentIndex;
                    return result;
                }
                if (SpaceSet.Contains(span[currentIndex]))
                {
                    if (currentIndex == wordStartIndex)
                    {
                        currentIndex++;
                        wordStartIndex++;
                    }
                    else
                    {
                        var s1 = span.Slice(wordStartIndex, currentIndex - wordStartIndex).ToString();
                        if (IsKeyWord(s1, out var type))
                        {
                            keyword = s1;
                            state = type == 0 ? ReadPhraseState.Operator : ReadPhraseState.Relation;
                            result = span.Slice(startIndex, wordStartIndex - startIndex - 1).ToString();
                            startIndex = currentIndex + 1;
                            return result;
                        }
                        wordStartIndex = currentIndex;
                    }
                }
                else
                {
                    currentIndex++;
                }
            }
            state = ReadPhraseState.End;
            keyword = string.Empty;
            result = span.Slice(startIndex, currentIndex - startIndex).ToString();
            startIndex = currentIndex + 1;
            return result;
        }

        /// <summary>
        /// 是否关键字
        /// </summary>
        /// <param name="s"></param>
        /// <param name="type">0：operator, 1:relation</param>
        /// <returns></returns>
        private bool IsKeyWord(string s, out int type)
        {
            if (OperatorKeyWordSet.Contains(s))
            {
                type = 0;
                return true;
            }

            if (RelationKeyWord.Contains(s))
            {
                type = 1;
                return true;
            }
            type = -1;
            return false;
        }
        private Relation TryParseRelation(string s)
        {
            Relation relation;
            if (s == "&&")
            {
                relation = Relation.And;
                return relation;
            }
            relation = Relation.Or;
            return relation;
        }
        private Operator TryParseOperator(string s)
        {
            Operator @operator;
            if (s == "<")
            {
                @operator = Operator.LessThan;
            }
            else if (s == "=")
            {
                @operator = Operator.Equal;
            }
            else if(s == "!=")
            {
                @operator = Operator.NotEqual;
            }
            else if (s == ">")
            {
                @operator = Operator.GreaterThan;
            }
            else if(s == "<=")
            {
                @operator = Operator.LessThanOrEqual;
            }
            else if (s == ">=")
            {
                @operator = Operator.GreaterThanOrEqual;
            }
            else
            {
                @operator = Operator.In;
            }
            return @operator;
        }
    }

    public enum ReadPhraseState
    {
        Start,
        Left,
        Operator,
        Right,
        Relation,
        End
    }

    public enum ClauseGroupState
    {
        Left,
        Operator,
        Right
    }

    public enum OperatorState
    {
        Left,
        Right
    }

    public class ClauseGroup
    {
        public ClauseGroup Parent { get; set; }

        public Queue<ClauseGroup> ClauseQueue { get; private set; } = new Queue<ClauseGroup>();

        public Queue<Relation> RelationQueue { get; private set; } =
        new Queue<Relation>();

        public bool IsCondition { get; set; }

        public Condition Condition { get; set; }
    }

    public class Condition
    {
        public bool IsSingle { get; set; }

        public string Left { get; set; }

        public string Right { get; set; }

        public Operator Operator { get; set; }
    }

    public enum Operator
    {
        None = -1,
        Equal = 0,
        NotEqual = 1,
        LessThan = 2,
        LessThanOrEqual = 3,
        GreaterThan = 4,
        GreaterThanOrEqual = 5,
        In = 6
    }

    public enum Relation
    {
        None = -1,
        And = 0,
        Or = 1
    }
}
