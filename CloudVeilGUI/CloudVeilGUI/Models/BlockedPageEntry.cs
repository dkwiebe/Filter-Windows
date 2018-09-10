﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CloudVeilGUI.Models
{
    public class BlockedPageEntry
    {
        public string CategoryName
        {
            get;
            private set;
        }

        public string FullRequest
        {
            get;
            private set;
        }

        public BlockedPageEntry(string category, string fullRequest)
        {
            this.CategoryName = category;
            this.FullRequest = fullRequest;
        }
    }
}