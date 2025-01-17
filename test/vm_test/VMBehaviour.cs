namespace vm_test
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using ancient.runtime.emit.sys;
    using ancient.runtime.tools;
    using MoreLinq;
    using NUnit.Framework;
    using vm.component;

    public abstract class VMBehaviour : IHalting
    {
        public Bus bus { get; private set; }
        public CPU cpu => bus.cpu;
        public State state => bus.State;
        public TestDevice dev => bus.find(0x1) as TestDevice;
        public BIOS bios => bus.find(0x45) as BIOS;

        protected VMBehaviour()
        {
            Module.Global.Add("assert->pass()", typeof(VMBehaviour).GetMethod("CallSuccess"));
            Module.Global.Add("assert->value()", typeof(VMBehaviour).GetMethod("CallValue"));
            reset();
        }
        [TearDown]
        public void reset()
        {
            bus = new Bus();
            bus.State.stack.__halter = this;
            bus.Add(new TestDevice());
            state.southFlag = true;
            bios.virtual_stack = true;
        }

        public static void CallSuccess() => Assert.Pass("call_success");
        public static int CallValue() => 0xDDD;

        public void load(params ulong[] values) => bus.State.Load("<exec>",values);

        public bool IsHalt() => state.halt == 1;
        public bool IsFastWrite() => state.fw;
        public bool IsOverflow() => state.of;
        public bool IsNegative() => state.nf;

        public void shot(uint count = 1) => 
            Enumerable.Range(0, (int) count).Pipe(x => cpu.Step()).ToArray();

        public void AssertRegister<T>(Expression<Func<State, T>> exp, T value) where T : struct, IFormattable
        {
            var name = RuntimeUtilities.GetFieldPath(exp);
            var selector = exp.Compile();
            var val = selector(state);
            if (value is float)
                Assert.AreEqual($"[{name}] {value}", $"[{name}] {val}");
            else
                Assert.AreEqual($"[{name}] 0x{value:X}", $"[{name}] 0x{val:X}");
        }

        public int halt(int reason, string text = "")
        {
            Assert.Fail($"HALT: {CPU.AssociationHaltCode(reason, text)}");
            return reason;
        }
    }
}