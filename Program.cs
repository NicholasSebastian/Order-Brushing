using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace OrderBrushing
{
    class Program
    {
        static void Main(string[] args)
        {
            List<(long, int, int, DateTime)> inputData = ReadFile();
            Dictionary<int, List<int>> finalData = ProcessByHour(inputData);
            WriteFile(finalData);
        }

        static List<(long, int, int, DateTime)> ReadFile()
        {
            var inputData = new List<(long, int, int, DateTime)>();
            try
            {
                string filename = "order_brush_order.csv";
                StreamReader reader = new StreamReader(filename);
                int lineCount = 0;
                using (reader)
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (!String.IsNullOrWhiteSpace(line) && lineCount != 0)
                        {
                            string[] values = line.Split(",");
                            long OrderId = Int64.Parse(values[0]);
                            int ShopId = Int32.Parse(values[1]);
                            int UserId = Int32.Parse(values[2]);
                            DateTime EventTime = DateTime.Parse(values[3]);
                            inputData.Add((OrderId, ShopId, UserId, EventTime));
                        }
                        lineCount++;
                    }
                }
                Console.WriteLine("Read {0} rows from {1}", lineCount, filename);
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read.");
                Console.WriteLine(e.Message);
            }
            return inputData;
        }

        static void WriteFile(Dictionary<int, List<int>> finalData)
        {
            string filename = "output.csv";
            StreamWriter writer = new StreamWriter(filename);
            using (writer)
            {
                writer.WriteLine("shopid,userid");
                foreach (var item in finalData)
                {
                    string value = "";
                    if (item.Value.Count == 0) value = "0";
                    else
                    {
                        List<int> diffBuyers = item.Value.Distinct().ToList();
                        value = diffBuyers[0].ToString();
                        if (diffBuyers.Count > 1)
                        {
                            for (int i = 1; i < diffBuyers.Count; i++)
                            {
                                value += String.Format(" & {0}", diffBuyers[i]);
                            }
                        }
                    }
                    writer.WriteLine("{0},{1}", item.Key, value);
                }
            }
            Console.WriteLine("Wrote {0} lines of data to {1}", finalData.Count, filename);
        }

        static Dictionary<int, List<int>> ProcessByHour(
            List<(long OrderId, int ShopId, int UserId, DateTime EventTime)> inputData
        )
        {
            Console.WriteLine("Processing data...");
            Stopwatch timer = new Stopwatch();
            timer.Start();

            inputData.Sort((x, y) => x.EventTime.CompareTo(y.EventTime));
            Dictionary<int, List<int>> finalData = new Dictionary<int, List<int>>();

            int i = 0, j = 0;
            bool wasExpanding = false;
            Dictionary<int, List<int>> range = new Dictionary<int, List<int>>();

            while (j < inputData.Count)
            {
                TimeSpan span = inputData[j].EventTime - inputData[i].EventTime;
                if (span.Hours <= 1)
                {
                    if (!range.ContainsKey(inputData[j].ShopId))
                    {
                        range.Add(inputData[j].ShopId, new List<int>());
                    }
                    range[inputData[j].ShopId].Add(inputData[j].UserId);
                    wasExpanding = true;
                    j++;
                }
                else
                {
                    if (wasExpanding)
                    {   // Calculate rates whenever a new range has been defined.
                        foreach (var item in range)
                        {
                            if (item.Value.Count > 0)
                            {
                                if (!finalData.ContainsKey(item.Key))
                                {
                                    finalData.Add(item.Key, new List<int>());
                                }
                                HashSet<int> diffBuyers = new HashSet<int>(item.Value);
                                if (item.Value.Count / diffBuyers.Count >= 3)
                                {
                                    finalData[item.Key].AddRange(item.Value);
                                }
                            }
                        }
                    }
                    range[inputData[i].ShopId].RemoveAt(0);
                    wasExpanding = false;
                    i++;
                }
            }
            timer.Stop();
            Console.WriteLine("Process completed in {0} seconds.", timer.Elapsed.Seconds);
            return finalData;
        }
    }
}
