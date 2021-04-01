namespace KianCommons
{
    using NodeController30;
    using System;
    using System.Diagnostics;
    internal static class Assertion
    {       
        internal static void AssertNotNull(object obj, string m = "") => Assert(obj != null, " unexpected null " + m);
        internal static void AssertEqual<T>(T a, T b, string m = "") where T : IComparable => Assert(a.CompareTo(b) == 0, $"expected {a} == {b} | " + m);
        internal static void AssertNeq<T>(T a, T b, string m = "") where T : IComparable => Assert(a.CompareTo(b) != 0, $"expected {a} != {b} | " + m);
        internal static void AssertGT<T>(T a, T b, string m = "") where T : IComparable => Assert(a.CompareTo(b) > 0, $"expected {a} > {b} | " + m);
        internal static void AssertGTEq<T>(T a, T b, string m = "") where T : IComparable => Assert(a.CompareTo(b) >= 0, $"expected {a} >= {b} | " + m);

        internal static void Assert(bool con, string m = "")
        {
            if (!con)
            {
                m = $"Assertion failed: {m}";
                Mod.Logger.Error(m);
                throw new Exception(m);
            }
        }
    }
}