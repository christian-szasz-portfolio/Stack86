namespace Stack86.Logic.Test.Languages.C;

using System.Collections.Generic;
using Stack86.Logic.Languages.C;

/// <summary>
/// Unit tests for <see cref="CLibraryRegistry"/> and <see cref="CLibraryFunction"/>.
/// </summary>
[TestClass]
public sealed class CLibraryRegistryTests
{
    [TestMethod]
    [DataRow("printf", "stdio.h", -1, LibraryFunctionKind.Intrinsic)]
    [DataRow("scanf", "stdio.h", -1, LibraryFunctionKind.Intrinsic)]
    [DataRow("putchar", "stdio.h", 1, LibraryFunctionKind.Intrinsic)]
    [DataRow("puts", "stdio.h", 1, LibraryFunctionKind.Intrinsic)]
    [DataRow("getchar", "stdio.h", 0, LibraryFunctionKind.Intrinsic)]
    [DataRow("abs", "stdlib.h", 1, LibraryFunctionKind.Intrinsic)]
    [DataRow("exit", "stdlib.h", 1, LibraryFunctionKind.Intrinsic)]
    public void TryGetFunction_KnownIntrinsic_ReturnsMetadata(
        string name,
        string header,
        int paramCount,
        LibraryFunctionKind kind)
    {
        Assert.IsTrue(CLibraryRegistry.TryGetFunction(name, out var fn));
        Assert.IsNotNull(fn);
        Assert.AreEqual(name, fn.Name);
        Assert.AreEqual(header, fn.Header);
        Assert.AreEqual(paramCount, fn.ParameterCount);
        Assert.AreEqual(kind, fn.Kind);
        Assert.AreEqual(0, fn.ServiceNumber);
    }

    [TestMethod]
    [DataRow("strlen", "string.h", 1, 0x02)]
    [DataRow("strcpy", "string.h", 2, 0x03)]
    [DataRow("strcmp", "string.h", 2, 0x04)]
    [DataRow("memset", "string.h", 3, 0x05)]
    [DataRow("rand", "stdlib.h", 0, 0x06)]
    [DataRow("srand", "stdlib.h", 1, 0x07)]
    [DataRow("atoi", "stdlib.h", 1, 0x08)]
    [DataRow("strcat", "string.h", 2, 0x09)]
    [DataRow("memcpy", "string.h", 3, 0x0A)]
    [DataRow("toupper", "ctype.h", 1, 0x0B)]
    [DataRow("tolower", "ctype.h", 1, 0x0C)]
    [DataRow("malloc", "stdlib.h", 1, 0x20)]
    [DataRow("free", "stdlib.h", 1, 0x21)]
    [DataRow("time", "time.h", 1, 0x26)]
    [DataRow("clock", "time.h", 0, 0x27)]
    [DataRow("__readint", "stdio.h", 0, 0x29)]
    [DataRow("__readstr", "stdio.h", 1, 0x2A)]
    public void TryGetFunction_KnownSyscall_ReturnsServiceNumber(
        string name,
        string header,
        int paramCount,
        int service)
    {
        Assert.IsTrue(CLibraryRegistry.TryGetFunction(name, out var fn));
        Assert.IsNotNull(fn);
        Assert.AreEqual(LibraryFunctionKind.Syscall, fn.Kind);
        Assert.AreEqual(header, fn.Header);
        Assert.AreEqual(paramCount, fn.ParameterCount);
        Assert.AreEqual(service, fn.ServiceNumber);
    }

    [TestMethod]
    [DataRow("isalpha")]
    [DataRow("isdigit")]
    [DataRow("isalnum")]
    [DataRow("isspace")]
    [DataRow("isupper")]
    [DataRow("islower")]
    [DataRow("ispunct")]
    [DataRow("isprint")]
    [DataRow("isxdigit")]
    [DataRow("iscntrl")]
    public void TryGetFunction_CtypePredicates_AreSyscallsInCtypeHeader(string name)
    {
        Assert.IsTrue(CLibraryRegistry.TryGetFunction(name, out var fn));
        Assert.IsNotNull(fn);
        Assert.AreEqual("ctype.h", fn.Header);
        Assert.AreEqual(LibraryFunctionKind.Syscall, fn.Kind);
        Assert.AreEqual(1, fn.ParameterCount);
    }

    [TestMethod]
    [DataRow("strncpy")]
    [DataRow("strncmp")]
    [DataRow("strncat")]
    [DataRow("strchr")]
    [DataRow("strrchr")]
    [DataRow("strstr")]
    [DataRow("memcmp")]
    [DataRow("calloc")]
    [DataRow("realloc")]
    [DataRow("itoa")]
    [DataRow("strtol")]
    [DataRow("difftime")]
    [DataRow("sleep")]
    [DataRow("Sleep")]
    public void TryGetFunction_ExtendedSyscalls_AreRegistered(string name)
    {
        Assert.IsTrue(CLibraryRegistry.TryGetFunction(name, out var fn));
        Assert.IsNotNull(fn);
        Assert.AreEqual(LibraryFunctionKind.Syscall, fn.Kind);
    }

    [TestMethod]
    public void TryGetFunction_Unknown_ReturnsFalse()
    {
        Assert.IsFalse(CLibraryRegistry.TryGetFunction("not_a_libc_function", out var fn));
        Assert.IsNull(fn);
    }

    [TestMethod]
    public void TryGetFunction_IsCaseSensitive_ButPreservesSleepBothCases()
    {
        // 'Sleep' (Win32 style) is a separate registration from 'sleep'.
        Assert.IsTrue(CLibraryRegistry.TryGetFunction("Sleep", out _));
        Assert.IsTrue(CLibraryRegistry.TryGetFunction("sleep", out _));

        // But 'PRINTF' uppercase is not registered (registry uses ordinal comparison).
        Assert.IsFalse(CLibraryRegistry.TryGetFunction("PRINTF", out _));
    }

    [TestMethod]
    public void IsHeaderIncluded_HeaderInSet_ReturnsTrue()
    {
        Assert.IsTrue(CLibraryRegistry.TryGetFunction("printf", out var fn));
        var headers = new HashSet<string>(StringComparer.Ordinal) { "stdio.h", "stdlib.h" };

        Assert.IsTrue(CLibraryRegistry.IsHeaderIncluded(fn!, headers));
    }

    [TestMethod]
    public void IsHeaderIncluded_HeaderNotInSet_ReturnsFalse()
    {
        Assert.IsTrue(CLibraryRegistry.TryGetFunction("strlen", out var fn));
        var headers = new HashSet<string>(StringComparer.Ordinal) { "stdio.h" };

        Assert.IsFalse(CLibraryRegistry.IsHeaderIncluded(fn!, headers));
    }

    [TestMethod]
    public void CLibraryFunction_RecordEquality_HoldsByValue()
    {
        var a = new CLibraryFunction("foo", "foo.h", 1, LibraryFunctionKind.Intrinsic, 0);
        var b = new CLibraryFunction("foo", "foo.h", 1, LibraryFunctionKind.Intrinsic, 0);
        var c = a with { ServiceNumber = 5 };

        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, c);
    }
}
