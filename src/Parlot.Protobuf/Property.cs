namespace Parlot.Protobuf;

using System;

public class Property
{
    public string Name;
    public TypeCode TypeCode;

    public bool IsFixedSize;
    public uint Index;
    public bool Repeated;
    public bool Optional;
    public bool Packed;
    public string Type;
    public bool Required;
    public Declaration Declaration;
}