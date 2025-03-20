using VariantEnum;

namespace ConsoleApp;

// uncreated.
public enum Variant : byte
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

public enum TestVariant
{

}

// uncreated.
[IgnoreVariant]
public enum TestIgnoreVariant
{

}

// uncreated.
public class A
{
    [IgnoreVariant]
    public enum ABVariant
    {

    }
}
