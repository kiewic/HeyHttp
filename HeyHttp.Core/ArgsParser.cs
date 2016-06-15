using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeyHttp.Core
{
    public class ArgsParser
    {
        private List<ArgsParserElement> elements = new List<ArgsParserElement>();

        public ArgsParser(string[] args)
        {
            foreach (string arg in args)
            {
                elements.Add(new ArgsParserElement(arg));
            }
        }

        public bool HasOption(string optionName)
        {
            int index;
            return TryGetOptionIndex(optionName, out index);
        }

        public bool TryGetOptionValue(string optionName, out string stringValue)
        {
            stringValue = String.Empty;

            for (int i = 0; i < elements.Count; i++)
            {
                ArgsParserElement element = elements[i];

                if (element.IsOption)
                {
                    if (String.Compare(element.Value, optionName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        int index = i;

                        if (TryGetString(index + 1, ref stringValue))
                        {
                            elements.RemoveAt(index + 1);
                            elements.RemoveAt(index);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool TryGetOptionValue(string optionName, out int intValue)
        {
            intValue = 0;

            string stringValue;
            if (TryGetOptionValue(optionName, out stringValue))
            {
                if (Int32.TryParse(stringValue, out intValue))
                {
                    return true;
                }

                Console.WriteLine("Argument '{0}' is not a valid Int32 value.", stringValue);
            }

            return false;
        }

        private bool TryGetOptionIndex(string optionName, out int index)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                ArgsParserElement element = elements[i];

                if (element.IsOption)
                {
                    if (String.Compare(element.Value, optionName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        index = i;
                        elements.RemoveAt(index);
                        return true;
                    }
                }
            }

            index = -1;
            return false;
        }

        public bool TryGetString(int index, ref string value)
        {
            if (elements.Count <= index)
            {
                return false;
            }

            if (elements[index].IsOption)
            {
                return false;
            }

            value = elements[index].Value;
            return true;
        }

        public bool TryGetInt32(int index, ref int value)
        {
            if (elements.Count <= index)
            {
                return false;
            }

            if (elements[index].IsOption)
            {
                return false;
            }

            string stringValue = elements[index].Value;
            int intValue;
            if (Int32.TryParse(stringValue, out intValue))
            {
                value = intValue;
                return true;
            }

            Console.WriteLine("Argument '{0}' is not a valid Int32 value.", stringValue);
            return false;
        }

        public void GetHostname(IHeyHostnameSettings settings, int index, string defaultHostname)
        {
            string hostname = defaultHostname;
            TryGetString(index, ref hostname);
            settings.Hostname = hostname;
        }

        public void GetPort(IHeyPortSettings settings, int index, int defaultPort)
        {
            int port = defaultPort;
            TryGetInt32(index, ref port);
            settings.Port = port;
        }
    }
}
