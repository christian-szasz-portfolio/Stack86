namespace Stack86.Logic.Test.Pipeline.CodeGen;

using Stack86.Logic.Pipeline.CodeGen;

/// <summary>
/// Direct unit tests for <see cref="StackFrameLayout"/>.
/// </summary>
[TestClass]
public sealed class StackFrameLayoutTests
{
    [TestMethod]
    public void ParameterCount_is_stored()
    {
        var layout = new StackFrameLayout(3);
        Assert.AreEqual(3, layout.ParameterCount);
    }

    [TestMethod]
    public void LocalsSize_zero_initially()
    {
        var layout = new StackFrameLayout(0);
        Assert.AreEqual(0, layout.LocalsSize);
    }

    [TestMethod]
    [DataRow(0, 4)]
    [DataRow(1, 6)]
    [DataRow(2, 8)]
    [DataRow(5, 14)]
    public void GetParameterOffset_returns_bp_relative(int index, int expected)
    {
        Assert.AreEqual(expected, StackFrameLayout.GetParameterOffset(index));
    }

    [TestMethod]
    public void AllocateRegister_grows_locals_by_two()
    {
        var layout = new StackFrameLayout(0);
        var off1 = layout.AllocateRegister(1);
        var off2 = layout.AllocateRegister(2);
        Assert.AreEqual(2, off1);
        Assert.AreEqual(4, off2);
        Assert.AreEqual(4, layout.LocalsSize);
    }

    [TestMethod]
    public void AllocateRegister_reuses_offset_for_same_register()
    {
        var layout = new StackFrameLayout(0);
        var off1 = layout.AllocateRegister(1);
        var off2 = layout.AllocateRegister(1);
        Assert.AreEqual(off1, off2);
        Assert.AreEqual(2, layout.LocalsSize);
    }

    [TestMethod]
    public void AllocateVariable_with_size_grows_by_size()
    {
        var layout = new StackFrameLayout(0);
        var off = layout.AllocateVariable(1, 10);
        Assert.AreEqual(10, off);
        Assert.AreEqual(10, layout.LocalsSize);
    }

    [TestMethod]
    public void AllocateVariable_returns_existing_offset_for_known_register()
    {
        var layout = new StackFrameLayout(0);
        var first = layout.AllocateVariable(1, 4);
        var second = layout.AllocateVariable(1, 8);
        Assert.AreEqual(first, second);
        Assert.AreEqual(4, layout.LocalsSize);
    }

    [TestMethod]
    public void GetOffset_returns_allocated_offset()
    {
        var layout = new StackFrameLayout(0);
        layout.AllocateRegister(7);
        Assert.AreEqual(2, layout.GetOffset(7));
    }

    [TestMethod]
    public void GetSize_returns_allocated_size()
    {
        var layout = new StackFrameLayout(0);
        layout.AllocateVariable(1, 6);
        Assert.AreEqual(6, layout.GetSize(1));
    }

    [TestMethod]
    public void GetSize_unknown_returns_two()
    {
        var layout = new StackFrameLayout(0);
        Assert.AreEqual(2, layout.GetSize(99));
    }

    [TestMethod]
    public void IsAllocated_reports_state()
    {
        var layout = new StackFrameLayout(0);
        Assert.IsFalse(layout.IsAllocated(5));
        layout.AllocateRegister(5);
        Assert.IsTrue(layout.IsAllocated(5));
    }
}
