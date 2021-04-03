namespace KianCommons {
    using ColossalFramework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using static KianCommons.Math.MathUtil;
    using System.Reflection;
    using static KianCommons.ReflectionHelpers;

    internal static class EnumBitMaskExtensions {
        [Obsolete("this is buggy as it assumes enum is 0,1,2,3,4 ...\nuse String2EnumValue instead")]
        
        internal static bool CheckFlags(this NetNode.Flags value, NetNode.Flags required, NetNode.Flags forbidden =0) =>
            (value & (required | forbidden)) == required;


        internal static bool CheckFlags(this NetSegment.Flags value, NetSegment.Flags required, NetSegment.Flags forbidden=0) =>
            (value & (required | forbidden)) == required;
    }
}