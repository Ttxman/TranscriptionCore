using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscriptionCore
{
    internal interface IUpdateTracking
    {
        public bool Revert(Undo act);
        public UpdateTracker Updates { get; }
    }
}
