using Shouldly;
using System.Buffers;
using System.Runtime.InteropServices.Marshalling;
using Xunit;

namespace VariantEnum.Tests;

public class NoneTest
{
    [Fact]
    public void Count()
    {
        None.Count.ShouldBe(0);
    }

    [Fact]
    public void GetName()
    {
        Assert.Throws<ArgumentException>(() => None.GetNumericValue(null));
    }

    [Fact]
    public void GetNames()
    {
        var names = None.GetNames();
        names.Length.ShouldBe(0);
    }

    [Fact]
    public void GetNumericValue()
    {
        Assert.Throws<ArgumentException>(() => None.GetNumericValue(null));
    }

    [Fact]
    public void ConvertEnum()
    {
        Assert.Throws<ArgumentException>(() => None.ConvertEnum(null));
    }

    [Fact]
    public void TryConvertEnum()
    {
        None.TryConvertEnum(null, out var _).ShouldBeFalse();
    }

    [Fact]
    public void Parse()
    {
        Assert.Throws<ArgumentException>(() => None.Parse("None"));
        Assert.Throws<ArgumentException>(() => None.Parse("None".AsSpan()));
        Assert.Throws<ArgumentException>(() => None.Parse("None", true));
    }

    [Fact]
    public void TryParse()
    {
        None.TryParse("None", out var _).ShouldBeFalse();
        None.TryParse("None".AsSpan(), out var _).ShouldBeFalse();
        None.TryParse("None", true, null, out var _).ShouldBeFalse();
    }

    [Fact]
    public void IsDefined()
    {
        None.IsDefined("None").ShouldBeFalse();

        None.IsDefined((None)default).ShouldBeFalse();
    }
}

public class IpAddrTest
{
    [Fact]
    public void CreateInstance()
    {
        {
            var v4 = new IpAddr.V4(127, 0, 0, 1);
            var v42 = new IpAddr.V4(127, 0, 0, 1);

            v4.ShouldBe(v42);
        }
        {
            var v4 = new IpAddr.V4(127, 0, 0, 1);
            var v42 = new IpAddr.V4(127, 0, 0, 2);

            v4.ShouldNotBe(v42);
        }
        {
            var v6 = new IpAddr.V6("::1");
            var v62 = new IpAddr.V6("::1");
            v6.ShouldBe(v62);
        }
        {
            var v6 = new IpAddr.V6("::1");
            var v62 = new IpAddr.V6("::");
            v6.ShouldNotBe(v62);
        }
    }

    [Fact]
    public void ISpanFormattable_TryFormat()
    {
        var buffer = new ArrayBufferWriter<char>(1024);

        var v4 = new IpAddr.V4(127, 0, 0, 1);
        v4.TryFormat(buffer.GetSpan(1024), out var charsWritten).ShouldBeTrue();
        buffer.Advance(charsWritten);
        v4.ToString().AsSpan().SequenceEqual(buffer.WrittenSpan).ShouldBeTrue();

        buffer.Clear();

        var v6 = new IpAddr.V6("::1");
        v6.TryFormat(buffer.GetSpan(1024), out var charsWritten2).ShouldBeTrue();
        buffer.Advance(charsWritten2);
        v6.ToString().AsSpan().SequenceEqual(buffer.WrittenSpan).ShouldBeTrue();

        buffer.Clear();

        var none = new IpAddr.None();
        none.TryFormat(buffer.GetSpan(1024), out var charsWritten3).ShouldBeTrue();
        buffer.Advance(charsWritten3);
        none.ToString().AsSpan().SequenceEqual(buffer.WrittenSpan).ShouldBeTrue();
    }

    [Fact]
    public void ValidateVariant()
    {
        var v4 = new IpAddr.V4(127, 0, 0, 1);
        v4.args0.ShouldBe((byte)127);
        v4.args1.ShouldBe((byte)0);
        v4.args2.ShouldBe((byte)0);
        v4.args3.ShouldBe((byte)1);

        var v6 = new IpAddr.V6("::1");
        v6.args0.ShouldBe("::1");
    }

    [Fact]
    public void Count()
    {
        IpAddr.Count.ShouldBe(3);
    }

    [Fact]
    public void GetName()
    {
        var v4 = new IpAddr.V4(127, 0, 0, 1);
        var v6 = new IpAddr.V6("::1");
        var none = new IpAddr.None();

        IpAddr.GetName(v4).ShouldBe(nameof(IpAddr.V4));
        IpAddr.GetName(v6).ShouldBe(nameof(IpAddr.V6));
        IpAddr.GetName(none).ShouldBe(nameof(IpAddr.None));

        IpAddr.GetName((IpAddr)default).ShouldBeNull();
    }

    [Fact]
    public void GetNames()
    {
        var names = IpAddr.GetNames();

        names[0].ShouldBe(nameof(IpAddr.V4));
        names[1].ShouldBe(nameof(IpAddr.V6));
        names[2].ShouldBe(nameof(IpAddr.None));
        names.Length.ShouldBe(3);
    }

    [Fact]
    public void GetNumericValue()
    {
        var v4 = new IpAddr.V4(127, 0, 0, 1);
        var v6 = new IpAddr.V6("::1");
        var none = new IpAddr.None();

        IpAddr.GetNumericValue(v4).ShouldBe((byte)IpAddrVariant.V4);
        IpAddr.GetNumericValue(v6).ShouldBe((byte)IpAddrVariant.V6);
        IpAddr.GetNumericValue(none).ShouldBe((byte)IpAddrVariant.None);

        Assert.Throws<ArgumentException>(() => IpAddr.GetNumericValue(null));
    }

    [Fact]
    public void ConvertEnum()
    {
        var v4 = new IpAddr.V4(127, 0, 0, 1);
        var v6 = new IpAddr.V6("::1");
        var none = new IpAddr.None();

        IpAddr.ConvertEnum(v4).ShouldBe(IpAddrVariant.V4);
        IpAddr.ConvertEnum(v6).ShouldBe(IpAddrVariant.V6);
        IpAddr.ConvertEnum(none).ShouldBe(IpAddrVariant.None);

        Assert.Throws<ArgumentException>(() => IpAddr.ConvertEnum(null));
    }

    [Fact]
    public void TryConvertEnum()
    {
        var v4 = new IpAddr.V4(127, 0, 0, 1);
        var v6 = new IpAddr.V6("::1");
        var none = new IpAddr.None();

        IpAddr.TryConvertEnum(v4, out var v4Enum).ShouldBeTrue();
        v4Enum.ShouldBe(IpAddrVariant.V4);
        IpAddr.TryConvertEnum(v6, out var v6Enum).ShouldBeTrue();
        v6Enum.ShouldBe(IpAddrVariant.V6);
        IpAddr.TryConvertEnum(none, out var noneEnum).ShouldBeTrue();
        noneEnum.ShouldBe(IpAddrVariant.None);

        IpAddr.TryConvertEnum(null, out var _).ShouldBeFalse();
    }

    [Fact]
    public void Parse()
    {
        IpAddr.Parse("V4").ShouldBe(IpAddr.V4.Default);
        IpAddr.Parse("v4", true).ShouldBe(IpAddr.V4.Default);
        IpAddr.Parse("V6").ShouldBe(IpAddr.V6.Default);
        IpAddr.Parse("v6", true).ShouldBe(IpAddr.V6.Default);
        IpAddr.Parse("None").ShouldBe(IpAddr.None.Default);
        IpAddr.Parse("none", true).ShouldBe(IpAddr.None.Default);

        Assert.Throws<ArgumentException>(() => IpAddr.Parse(string.Empty));
        Assert.Throws<ArgumentException>(() => IpAddr.Parse(string.Empty.AsSpan()));
        Assert.Throws<ArgumentException>(() => IpAddr.Parse(string.Empty, true));
    }

    [Fact]
    public void TryParse()
    {
        IpAddr.TryParse("V4", out var v4Enum).ShouldBeTrue();
        v4Enum.ShouldBe(IpAddr.V4.Default);
        IpAddr.TryParse("v4", true, null, out v4Enum).ShouldBeTrue();
        v4Enum.ShouldBe(IpAddr.V4.Default);
        IpAddr.TryParse("V6", out var v6Enum).ShouldBeTrue();
        v6Enum.ShouldBe(IpAddr.V6.Default);
        IpAddr.TryParse("v6", true, null, out v6Enum).ShouldBeTrue();
        v6Enum.ShouldBe(IpAddr.V6.Default);
        IpAddr.TryParse("None", out var noneEnum).ShouldBeTrue();
        noneEnum.ShouldBe(IpAddr.None.Default);
        IpAddr.TryParse("none", true, null, out noneEnum).ShouldBeTrue();
        noneEnum.ShouldBe(IpAddr.None.Default);

        IpAddr.TryParse(string.Empty, out var _).ShouldBeFalse();
        IpAddr.TryParse(string.Empty.AsSpan(), out var _).ShouldBeFalse();
        IpAddr.TryParse(string.Empty, true, null,  out var _).ShouldBeFalse();
    }

    [Fact]
    public void IsDefined()
    {
        IpAddr.IsDefined("V4").ShouldBeTrue();
        IpAddr.IsDefined("V6").ShouldBeTrue();
        IpAddr.IsDefined("None").ShouldBeTrue();
        IpAddr.IsDefined(string.Empty).ShouldBeFalse();

        IpAddr.IsDefined(new IpAddr.V4(127, 0, 0, 1)).ShouldBeTrue();
        IpAddr.IsDefined(new IpAddr.V6("::1")).ShouldBeTrue();
        IpAddr.IsDefined(new IpAddr.None()).ShouldBeTrue();
        IpAddr.IsDefined((IpAddr)default).ShouldBeFalse();
    }
}
