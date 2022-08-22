namespace Parlot.Protobuf
{
    using System;
    using System.Collections.Generic;

    public class ParsedValue
    {
        public Property Definition;

        public object Value;

        public ParsedMessage MessageValue;
        public object[] Values;

        public ParsedMessage[] MessageValues;

        public IEnumerable<object> GetValues()
        {
            if (Value != null)
                return new[] { Value };
            if (MessageValue != null)
                return new[] { MessageValue };
            if (Values != null)
                return Values;
            if (MessageValues != null)
                return MessageValues;
            throw new NotSupportedException("There is no value");
        }

        public object GetValue()
        {
            if (Value != null)
                return Value;
            if (MessageValue != null)
                return MessageValue;
            if (Values != null)
                return Values;
            if (MessageValues != null)
                return MessageValues;
            throw new NotSupportedException("There is no value");
        }
    }
}