using ModsCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeController30
{
    public class Mod : BasePatcherMod
    {
        public override string WorkshopUrl => throw new NotImplementedException();
        protected override string ModName => "Node Controller Renewal";
        protected override string ModDescription => string.Empty;
        protected override List<Version> ModVersions => new List<Version>();

        protected override string ModId => nameof(NodeController30);
        protected override bool ModIsBeta => true;
        protected override string ModLocale => string.Empty;

        protected override BasePatcher CreatePatcher() => new Patcher(this);
    }
}
