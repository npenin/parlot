namespace Parlot.Protobuf;

using System.Collections.Generic;

public class Enum<T> : Declaration
{
    public class EnumValue
    {
        public string Name;
        public T Value;
    }

    public List<EnumValue> Values = new();

}