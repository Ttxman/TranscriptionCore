using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscriptionCore
{
    public class UpdateTracker
    {
        public Action<Undo[]>? ContentChanged;
        private bool _updated = false;
        private bool _logUpdates = true;
        private int _Updating = 0;
        public bool Updating
        {
            get { return _Updating > 0; }
        }
        readonly List<Undo> _changes = new List<Undo>();
        public bool OnContentChanged(params Undo[] actions)
        {
            if (_Updating <= 0)
            {
                ContentChanged?.Invoke(actions);
            }
            else
            {
                if (_logUpdates)
                    _changes.AddRange(actions);
                _updated = true;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Stop Bubbling changes through OnContentChanged() anc ContentChanged event and acumulate changes until EndUpdate is called
        /// </summary>
        public void BeginUpdate(bool logupdates = true)
        {
            if (_Updating <= 0)
            {
                _logUpdates = logupdates;
                _changes.Clear();
            }
            _Updating++;

        }

        /// <summary>
        /// Bubble all accumulated changes (since BeginUpdate) as one big change, and resume immediate Bubbling of changes
        /// </summary>
        public void EndUpdate()
        {
            _Updating--;
            if (_Updating > 0)
                return;

            if (_updated)
                OnContentChanged(_changes.ToArray());
            _changes.Clear();
            _updated = false;
        }
    }
}
