using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeController.Utilities
{
    public static class Extensions
    {
        public static string Description<T>(this T value) where T : Enum => value.Description<T, Mod>();
    }
}
