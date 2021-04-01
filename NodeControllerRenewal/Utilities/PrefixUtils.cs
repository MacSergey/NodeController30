namespace KianCommons.Patches
{
    using CSUtil.Commons;
    public static class PrefixUtils
    {
        public static bool HandleTernaryBool(TernaryBool? res, ref bool __result)
        {
            if (res != null)
            {
                if (res == TernaryBool.True)
                {
                    __result = true;
                    return false;
                }
                if (res == TernaryBool.False)
                {
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }
}
