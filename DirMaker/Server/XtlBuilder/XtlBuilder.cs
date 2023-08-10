using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;

namespace Com.Raf.Xtl.Build
{
    public delegate void StatusDelegate(string message);

    public class XtlBuilder
    {
        private static ILog log = LogManager.GetLogger(typeof(XtlBuilder));

        public bool Success { get; protected set; } = false;

        public StatusDelegate UpdateStatus { get; set; }

        public virtual void Build(bool cassMass, bool runTests) { }
    }
}
