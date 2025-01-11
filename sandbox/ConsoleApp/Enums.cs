using VariantEnum;

namespace ConsoleApp;

public enum IpAddrVariant : byte
{
    [VariantValueType(typeof(byte), typeof(byte), typeof(byte), typeof(byte))]
    V4,
    [VariantValueType(typeof(string))]
    V6,
    None
}

[IgnoreVariant]
public enum TestVariant
{

}

public class A
{
    [IgnoreVariant]
    public enum ABVariant
    {

    }
}
