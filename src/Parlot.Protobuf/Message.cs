namespace Parlot.Protobuf;

using System.Collections.Generic;

public class Message : Declaration
{
    public List<Property> Properties = new();

    public List<Enum<uint>> Enums = new();
}