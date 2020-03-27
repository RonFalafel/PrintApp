using System;
using System.Collections.Generic;
using WebApp.Core;

namespace PrintApp.Logic
{
    public class MarlinStateUpdater
    {
        public event EventHandler DoneReadingFileList;

        private readonly PrinterState _state;
        private bool _isReadingFileList;

        public MarlinStateUpdater(PrinterState state)
        {
            _state = state;
            _isReadingFileList = false;
        }

        public void ReadMarlinResponse(string response)
        {
            if (response == "Begin file list")
            {
                _state.GCodes = new List<string>();
                _isReadingFileList = true;
            }
            else if (_isReadingFileList && response == "End file list")
            {
                _isReadingFileList = false;
                DoneReadingFileList?.Invoke(this, null);
            }
            else if (_isReadingFileList)
                _state.GCodes.Add(response);
        }
    }
}