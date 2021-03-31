using HarmonyLib;
using ModsCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeController30
{
    public class Patcher : BasePatcher
    {
        public Patcher(BaseMod mod) : base(mod) { }

        protected override bool PatchProcess()
        {
            var success = true;

            //success &= PatchToolControllerAwake();

            return success;
        }

        //private bool PatchToolControllerAwake()
        //{
        //    var prefix = AccessTools.Method(typeof(NodeMarkupTool), nameof(NodeMarkupTool.Create));

        //    return AddPrefix(prefix, typeof(ToolController), "Awake");
        //}
    }
}
