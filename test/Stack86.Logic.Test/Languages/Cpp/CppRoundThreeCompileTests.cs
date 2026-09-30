namespace Stack86.Logic.Test.Languages.Cpp;

using Stack86.Logic.Compilation.Models;
using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// Round-three C++ feature pressure: inheritance with base class, methods/free
/// functions with reference parameters, escape sequences in string literals.
/// </summary>
[TestClass]
public sealed class CppRoundThreeCompileTests
{
    [TestMethod]
    [DataRow(
        @"#include <iostream>
class Animal {
public:
    int legs;
    Animal() { legs = 4; }
};
class Dog : public Animal {
public:
    Dog() : Animal() {}
};
int main() {
    Dog d;
    std::cout << d.legs << std::endl;
    return 0;
}
",
        "inherit_base_ctor")]
    [DataRow(
        @"#include <iostream>
void increment(int& x) { x = x + 1; }
int main() {
    int a = 5;
    increment(a);
    std::cout << a << std::endl;
    return 0;
}
",
        "free_fn_ref_param")]
    [DataRow(
        @"#include <iostream>
class Counter {
public:
    int value;
    Counter() { value = 0; }
    void addTo(int& sink) { sink = sink + value; }
};
int main() {
    Counter c;
    c.value = 5;
    int total = 10;
    c.addTo(total);
    std::cout << total << std::endl;
    return 0;
}
",
        "method_ref_param")]
    [DataRow(
        @"#include <iostream>
int main() {
    std::cout << ""tab\there\n"";
    std::cout << ""quote\""end"" << std::endl;
    return 0;
}
",
        "escape_sequences")]
    [DataRow(
        @"#include <iostream>
int main() {
    char c = '\n';
    std::cout << ""x"" << std::endl;
    return 0;
}
",
        "char_newline_literal")]
    public async Task Compiles(string source, string label)
    {
        var result = await CompileFixture.CompileLanguage(SupportedLanguage.Cpp, source, $"{label}.cpp");
        Assert.IsTrue(result.IsSuccess, $"{label}: {(result.IsFailure ? result.Error : string.Empty)}");
    }
}
