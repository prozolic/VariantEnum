using FluentAssertions;
using System.Buffers;

namespace VariantEnum.Tests;

public class NoneTest
{
    [Fact]
    public void Count()
    {
        None.Count.Should().Be(0);
    }

    [Fact]
    public void GetName()
    {
        Assert.Throws<InvalidOperationException>(() => None.GetNumericValue(null));
    }

    [Fact]
    public void GetNames()
    {
        var names = None.GetNames();
        names.Length.Should().Be(0);
    }

    [Fact]
    public void GetNumericValue()
    {
        Assert.Throws<InvalidOperationException>(() => None.GetNumericValue(null));
    }

    [Fact]
    public void ConvertEnum()
    {
        Assert.Throws<InvalidOperationException>(() => None.ConvertEnum(null));
    }

    [Fact]
    public void TryConvertEnum()
    {
        None.TryConvertEnum(null, out var _).Should().BeFalse();
    }

    [Fact]
    public void Parse()
    {
        Assert.Throws<InvalidOperationException>(() => None.Parse("None"));
    }

    [Fact]
    public void TryParse()
    {
        None.TryParse("None", out var _).Should().BeFalse();
    }

    [Fact]
    public void IsDefined()
    {
        None.IsDefined("None").Should().BeFalse();

        None.IsDefined((None)default).Should().BeFalse();
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

            v4.Should().Be(v42);
        }
        {
            var v4 = new IpAddr.V4(127, 0, 0, 1);
            var v42 = new IpAddr.V4(127, 0, 0, 2);

            v4.Should().NotBe(v42);
        }
    }

    [Fact]
    public void ISpanFormattable_TryFormat()
    {
        var buffer = new ArrayBufferWriter<char>(1024);

        var v4 = new IpAddr.V4(127, 0, 0, 1);
        v4.TryFormat(buffer.GetSpan(1024), out var charsWritten).Should().BeTrue();
        buffer.Advance(charsWritten);
        v4.ToString().AsSpan().SequenceEqual(buffer.WrittenSpan).Should().BeTrue();

        buffer.Clear();

        var v6 = new IpAddr.V6("::1");
        v6.TryFormat(buffer.GetSpan(1024), out var charsWritten2).Should().BeTrue();
        buffer.Advance(charsWritten2);
        v6.ToString().AsSpan().SequenceEqual(buffer.WrittenSpan).Should().BeTrue();
    }

    [Fact]
    public void ValidateVariant()
    {
        var v4 = new IpAddr.V4(127, 0, 0, 1);
        v4.args0.Should().Be(127);
        v4.args1.Should().Be(0);
        v4.args2.Should().Be(0);
        v4.args3.Should().Be(1);

        var v6 = new IpAddr.V6("::1");
        v6.args0.Should().Be("::1");
    }

    [Fact]
    public void Count()
    {
        IpAddr.Count.Should().Be(2);
    }

    [Fact]
    public void GetName()
    {
        var v4 = new IpAddr.V4(127, 0, 0, 1);
        var v6 = new IpAddr.V6("::1");

        IpAddr.GetName(v4).Should().Be(nameof(IpAddr.V4));
        IpAddr.GetName(v6).Should().Be(nameof(IpAddr.V6));

        Assert.Throws<InvalidOperationException>(() => IpAddr.GetNumericValue(null));
    }

    [Fact]
    public void GetNames()
    {
        var names = IpAddr.GetNames();

        names[0].Should().Be(nameof(IpAddr.V4));
        names[1].Should().Be(nameof(IpAddr.V6));
    }

    [Fact]
    public void GetNumericValue()
    {
        var v4 = new IpAddr.V4(127, 0, 0, 1);
        var v6 = new IpAddr.V6("::1");

        IpAddr.GetNumericValue(v4).Should().Be((byte)IpAddrVariant.V4);
        IpAddr.GetNumericValue(v6).Should().Be((byte)IpAddrVariant.V6);

        Assert.Throws<InvalidOperationException>(() => IpAddr.GetNumericValue(null));
    }

    [Fact]
    public void ConvertEnum()
    {
        var v4 = new IpAddr.V4(127, 0, 0, 1);
        var v6 = new IpAddr.V6("::1");

        IpAddr.ConvertEnum(v4).Should().Be(IpAddrVariant.V4);
        IpAddr.ConvertEnum(v6).Should().Be(IpAddrVariant.V6);

        Assert.Throws<InvalidOperationException>(() => IpAddr.ConvertEnum(null));
    }

    [Fact]
    public void TryConvertEnum()
    {
        var v4 = new IpAddr.V4(127, 0, 0, 1);
        var v6 = new IpAddr.V6("::1");

        IpAddr.TryConvertEnum(v4, out var v4Enum).Should().BeTrue();
        v4Enum.Should().Be(IpAddrVariant.V4);
        IpAddr.TryConvertEnum(v6, out var v6Enum).Should().BeTrue();
        v6Enum.Should().Be(IpAddrVariant.V6);

        IpAddr.TryConvertEnum(null, out var _).Should().BeFalse();
    }

    [Fact]
    public void Parse()
    {
        IpAddr.Parse("V4").Should().Be(IpAddr.V4.Default);
        IpAddr.Parse("V6").Should().Be(IpAddr.V6.Default);

        Assert.Throws<InvalidOperationException>(() => IpAddr.Parse("None"));
    }

    [Fact]
    public void TryParse()
    {
        IpAddr.TryParse("V4", out var v4Enum).Should().BeTrue();
        v4Enum.Should().Be(IpAddr.V4.Default);
        IpAddr.TryParse("V6", out var v6Enum).Should().BeTrue();
        v6Enum.Should().Be(IpAddr.V6.Default);

        IpAddr.TryParse("None", out var _).Should().BeFalse();
    }

    [Fact]
    public void IsDefined()
    {
        IpAddr.IsDefined("V4").Should().BeTrue();
        IpAddr.IsDefined("V6").Should().BeTrue();
        IpAddr.IsDefined("None").Should().BeFalse();

        IpAddr.IsDefined(new IpAddr.V4(127, 0, 0, 1)).Should().BeTrue();
        IpAddr.IsDefined(new IpAddr.V6("::1")).Should().BeTrue();
        IpAddr.IsDefined((IpAddr)default).Should().BeFalse();
    }
}
