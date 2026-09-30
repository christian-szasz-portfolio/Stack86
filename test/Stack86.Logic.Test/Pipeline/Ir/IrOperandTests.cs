namespace Stack86.Logic.Test.Pipeline.Ir;

using Stack86.Logic.Pipeline.Ir;

[TestClass]
public sealed class IrOperandTests
{
    [TestMethod]
    public void Reg_SetsKindAndRegister()
    {
        var op = IrOperand.Reg(5);
        Assert.AreEqual(IrOperandKind.Register, op.Kind);
        Assert.AreEqual(5, op.Register);
    }

    [TestMethod]
    public void Imm_SetsKindAndValue()
    {
        var op = IrOperand.Imm(-7);
        Assert.AreEqual(IrOperandKind.Immediate, op.Kind);
        Assert.AreEqual(-7, op.Value);
    }

    [TestMethod]
    public void Lbl_SetsKindAndLabel()
    {
        var op = IrOperand.Lbl("end");
        Assert.AreEqual(IrOperandKind.Label, op.Kind);
        Assert.AreEqual("end", op.Label);
    }
}
