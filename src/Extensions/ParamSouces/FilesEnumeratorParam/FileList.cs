using System;
using System.Collections.Generic;
using System.Linq;
using MIISHandler;
using DotLiquid;
using System.Collections;

namespace FilesEnumeratorParam
{
    public class FileList : Drop, IReadOnlyList<MIISFile>
    {
        //Private info
        private IList<MIISFile> _fpl;   //File proxy list

    #region Constructors
        //Helper function
        private void InitInternalList(List<MIISFile> fpl)
        {
            _fpl = fpl;

            //Assign previous and next in the linkedlist
            int max = _fpl.Count - 1;
            for (int i = 0; i <= max; i++)
            {
                if (i > 0) _fpl[i].Previous = _fpl[i - 1];
                if (i < max) _fpl[i].Next = _fpl[i + 1];
                //TODO: Add unique Categories and Tags strings with HashSet<string>
            }
        }

        public FileList(List<MIISFile> fpl)
        {
            InitInternalList(fpl);
        }

        #endregion

        #region Special Properties
        public int Count
        {
            get
            {
                return _fpl.Count;
            }
        }

        public MIISFile First
        {
            get
            {
                return _fpl.First();
            }
        }

        public MIISFile Last
        {
            get
            {
                return _fpl.Last();
            }
        }
        #endregion

        #region Explicit IReadOnlyList implementation
        MIISFile IReadOnlyList<MIISFile>.this[int index] => _fpl[index];

        int IReadOnlyCollection<MIISFile>.Count => _fpl.Count;

        IEnumerator<MIISFile> IEnumerable<MIISFile>.GetEnumerator()
        {
            return _fpl.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _fpl.GetEnumerator();
        }
        #endregion
    }
}
