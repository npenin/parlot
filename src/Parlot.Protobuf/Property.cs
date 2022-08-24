namespace Parlot.Protobuf;

using System;

public class Property : Declaration
{
    public TypeCode TypeCode { get; private set; }

    public bool IsFixedSize;
    public uint Index;
    public bool Repeated;
    public bool Optional;
    public bool Packed;
    private string type;
    public bool Required;
    public Declaration Declaration;

    public byte WireType { get; private set; }

    public string Type
    {
        get => type;
        set
        {
            type = value;
            TypeCode = value switch
            {
                "double" => TypeCode.Double,
                "float" => TypeCode.Float,
                "int32" => TypeCode.Int32,
                "int64" => TypeCode.Int64,
                "uint32" => TypeCode.UInt32,
                "uint64" => TypeCode.UInt64,
                "sint32" => TypeCode.Int32,
                "sint64" => TypeCode.Int64,
                "fixed32" => TypeCode.Int32,
                "fixed64" => TypeCode.Int64,
                "sfixed32" => TypeCode.Int32,
                "sfixed64" => TypeCode.Int64,
                "bool" => TypeCode.Boolean,
                "string" => TypeCode.String,
                "bytes" => TypeCode.Bytes,
                var x when x.StartsWith("map<") => TypeCode.Map,
                _ => TypeCode.Declaration
            };
            WireType = TypeCode switch
            {
                TypeCode.Double => 1,
                TypeCode.Float => 5,
                TypeCode.Int32 => 0,
                TypeCode.Int64 => 1,
                TypeCode.UInt32 => 0,
                TypeCode.UInt64 => 0,
                TypeCode.SInt32 => 0,
                TypeCode.SInt64 => 0,
                TypeCode.Fixed32 => 5,
                TypeCode.Fixed64 => 1,
                TypeCode.Sfixed32 => 5,
                TypeCode.Sfixed64 => 1,
                TypeCode.Boolean => 0,
                TypeCode.String => 2,
                TypeCode.Bytes => 2,
                TypeCode.Declaration when Declaration is Enum<ulong> => 0,
                TypeCode.Declaration => 2,
                TypeCode.Map => 2,
                _ => byte.MaxValue
            };
        }
    }
}