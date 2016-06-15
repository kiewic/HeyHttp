using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeyHttp.Core
{
    public class ArgsParserElement
    {
        public bool IsOption { get; set; }
        public string Value { get; set; }

        public ArgsParserElement(string arg)
        {
            // Is it an option?
            if (arg.StartsWith("-") || arg.StartsWith("/"))
            {
                IsOption = true;

                // Remove '-' or '/'.
                Value = arg.Substring(1);
            }
            else
            {
                IsOption = false;
                Value = arg;
            }
        }
    }
}
