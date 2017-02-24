//
// Copyright (c) 2017 Equine Smart Bits, LLC. All rights reserved

using System.Text.RegularExpressions;
using Android.OS;
using System.IO;

namespace ESB
{
    public class ParseLog
    {
        public static void GetData(string line_input, out int hr, out int sp, out double temp, out bool calculated,
                                   out long time, out int red, out int ir)
        {
            hr = sp = -1;
            temp = -1.0;
            calculated = false;
            time = -1;
            red = ir = -1;

            string line = line_input.Trim();

            if (line.Length == 0)
                return;

            // time=nnn, red=nnn, ir=nnn
            Regex rgx0 = new Regex(@"time=(-?\d+), red=(\d+), ir=(\d+)");
            MatchCollection matches = rgx0.Matches(line);

            if (matches.Count == 1)
            {
                GroupCollection data = matches[0].Groups;

                System.Diagnostics.Debug.Assert(data.Count == 4);

                time = System.Convert.ToInt64(data[1].Value);
                red = System.Convert.ToInt32(data[2].Value);
                ir = System.Convert.ToInt32(data[3].Value);
                return;
            }


            // T=%.1fF
            Regex rgx1 = new Regex(@"T=(.*)F");
            matches = rgx1.Matches(line);

            if (matches.Count == 1)
            {
                GroupCollection data = matches[0].Groups;

                System.Diagnostics.Debug.Assert(data.Count == 2);

                temp = System.Convert.ToDouble(data[1].Value);
                return;
            }

            // Raw: TS=%d, HR=%d (valid=%d), SpO=%d (valid=%d)
            Regex rgx2 = new Regex(@"Raw TS=(-?\d+), HR=(\d+) \(valid=(\d)\), SP=(\d+) \(valid=(\d)\)");

            matches = rgx2.Matches(line);

            if (matches.Count == 1)
            {
                GroupCollection data = matches[0].Groups;

                System.Diagnostics.Debug.Assert(data.Count == 6);

                time = System.Convert.ToInt64(data[1].Value);
                hr = System.Convert.ToInt32(data[2].Value);
                int hr_valid = System.Convert.ToInt32(data[3].Value);
                sp = System.Convert.ToInt32(data[4].Value);
                int sp_valid = System.Convert.ToInt32(data[5].Value);
                return;
            }

            // Calculated HR=%d, SpO2=%d
            Regex rgx3 = new Regex(@"Calculated HR=(\d+), SP=(\d+)");

            matches = rgx3.Matches(line);

            if (matches.Count == 1)
            {
                GroupCollection data = matches[0].Groups;

                System.Diagnostics.Debug.Assert(data.Count == 3);

                hr = System.Convert.ToInt32(data[1].Value);
                sp = System.Convert.ToInt32(data[2].Value);
                calculated = true;
                return;
            }
        }
    };

    public class WriteLog
    {
        private static System.IO.StreamWriter s_sr = null;

        public WriteLog() { Init(); }

        ~WriteLog() { Close(); }

        public static bool Init()
        {
            if (s_sr != null)
                return true;

            string path = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads).AbsolutePath;

            System.DateTime dt = System.DateTime.Now;

            string ymhms = dt.ToString("MMddHHmmss");

            string filename = Path.Combine(path, "esblog" + ymhms + ".txt");

            try
            {
                s_sr = new StreamWriter(filename, true);
            }
            catch (System.Exception e)
            {
                s_sr = null;
                return false;
            }

            return true;
        }

        public static void Close()
        {
            if (s_sr != null)
            {
                s_sr.Close();
                s_sr = null;
            }
        }

        public void Write(string str)
        {
            if (s_sr != null)
            {
                s_sr.Write(str);
            }
        }

        public void WriteLn(string str)
        {
            if (s_sr != null)
            {
                s_sr.WriteLine(str);
            }
        }
    }
}
