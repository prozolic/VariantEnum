// See https://aka.ms/new-console-template for more information
using VariantEnum;

Console.WriteLine("Hello, World!");


public enum IpAddrVariant
{
    [VariantValueType(typeof(byte), typeof(byte), typeof(byte), typeof(byte))]
    V41,
    [VariantValueType(typeof(string))]
    V61
}
