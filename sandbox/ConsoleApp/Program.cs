// See https://aka.ms/new-console-template for more information
using ConsoleApp;
using System.Buffers;
using VariantEnum;

Console.WriteLine("Hello, World!");

var v = new IpAddr.V4(127, 0, 0, 1);
var v2 = new IpAddr.V6("::1");

var v_string = $"IpAddr.V4 = {v}";

var str = (IpAddr)v switch
{
    IpAddr.V4 v4 => $"{v4.args0}.{v4.args1}.{v4.args2}.{v4.args3}",
    IpAddr.V6 v6 => v6.args0,
    _ => throw new Exception(),
};
Console.WriteLine(str);
Console.WriteLine(IpAddr.Count);

var value = IpAddr.ConvertEnum(v);
var value2 = IpAddr.ConvertEnum(v2);
Console.WriteLine(IpAddr.GetName(v));

var buffer = new ArrayBufferWriter<char>(1024);
if (v.TryFormat(buffer.GetSpan(1024), out var charsWritten))
{
    buffer.Advance(charsWritten);
    var str2 = string.Create(charsWritten, (buffer, charsWritten), static (buffer, state) =>
    {
        for (var i = 0; i < state.charsWritten; i++)
        {
            buffer[i] = state.buffer.WrittenSpan[i];
        }
    });
    Console.WriteLine(@$"TryFormat: {str2} {v}");
}

Console.WriteLine("END");