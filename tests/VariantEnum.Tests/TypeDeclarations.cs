
namespace VariantEnum.Tests;

public enum NoneVariant
{
}

public enum IpAddrVariant : byte
{
    [VariantValueType(typeof(byte), typeof(byte), typeof(byte), typeof(byte))]
    V4,
    [VariantValueType(typeof(string))]
    V6,
    None
}