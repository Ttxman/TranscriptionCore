using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscriptionCore
{
    public abstract record Undo()
    {
        public TranscriptionIndex TranscriptionIndex;
    }
}
