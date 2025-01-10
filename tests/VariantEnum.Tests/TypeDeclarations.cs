using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VariantEnum.Tests;

public enum IpAddrVariant : byte
{
    [VariantValueType(typeof(byte), typeof(byte), typeof(byte), typeof(byte))]
    V4,
    [VariantValueType(typeof(string))]
    V6,
}

