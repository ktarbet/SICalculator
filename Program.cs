using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reclamation.TimeSeries.Hydromet;
using Reclamation.TimeSeries;
using Reclamation.Core;

namespace HistoricalSiCalculator
{
    class Program
    {

        static void Main(string[] args)
        {

            if (args.Length != 7)
            {
                Console.WriteLine("Usage: SiCalculator cbtt mm/dd/yyyy  mm/dd/yyyy rollover SI|SI2 username password");
                Console.WriteLine("This program computes incremental solar radiation");
                Console.WriteLine(  );
                Console.WriteLine("Example: WRDO 7/21/2010 8/12/2010 1064.05 SI ktarbet mypassword");
                
 
                return;
            }

            var cbtt = args[0];
            DateTime t1 = Convert.ToDateTime(args[1]);
            DateTime t2 = Convert.ToDateTime(args[2]);
            double rollover = Convert.ToDouble(args[3]);
            var outputPcode = args[4];
            Console.WriteLine("cbtt = "+cbtt);
            Console.WriteLine("t1 = "+t1.ToShortDateString());
            Console.WriteLine("t2 = " + t2.ToShortDateString());
            Console.WriteLine("rollover = " + rollover);
            Console.WriteLine("pcode ="+outputPcode);


            var s = ComputeSI(cbtt, rollover, t1, t2);

            Logger.EnableLogger();

            HydrometInstantSeries.Save(s, cbtt, outputPcode, args[5], args[6]);

            if (t1.Year < 1980)
            {
                Console.WriteLine("Error: starting year is before 1980");
                return;
            }

            Reclamation.Core.ssh.Utility.Close();
        }      



        static Series ComputeSI(string cbtt,  double rollover,DateTime t1, DateTime t2)
        {
            var s = new HydrometInstantSeries(cbtt, "SQ", HydrometHost.PN);
            
            s.Read(t1.AddDays(-1), t2); // grab one extra day
            var si = new Series();
            

            int idx = s.LookupIndex(t1);
            if (idx == 0)
                idx++;
            
            for (int i = idx; i < s.Count; i++)
            {
                var prev = s[i - 1];
                var pt = s[i];

                bool goodData = !prev.IsMissing && !pt.IsMissing;

                if (goodData && pt.DateTime.Subtract(prev.DateTime).TotalMinutes <= 61) // must be within 61 minutes
                {
                    bool hasRollover = (pt.Value - prev.Value) < -50 ;
                    if( hasRollover )
                    {
                        si.Add(pt.DateTime, rollover - prev.Value + pt.Value);
                    }
                    else
                    {
                        si.Add(pt.DateTime, pt.Value - prev.Value);
                    }
                }
            }
            return si;
        }
    }
}
