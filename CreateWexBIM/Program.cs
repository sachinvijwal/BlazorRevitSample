using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xbim.COBieLite;
using Xbim.CobieLiteUk;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.ModelGeometry.Scene;
using XbimExchanger.IfcToCOBieLiteUK;

namespace CreateWexBIM
{
    class Program
    {
        //private const string IFC_FILE = "SampleHouse";
        //private const string IFC_FILE = "Sample";
        private const string IFC_FILE = "Rettorato";
        public static void Main()
        {
            Log.Logger = new LoggerConfiguration()
               .Enrich.FromLogContext()
               .WriteTo.ColoredConsole()
               .CreateLogger();

            var lf = new LoggerFactory().AddSerilog();
            var log = lf.CreateLogger("WexbimCreation");
            log.LogInformation("Creating wexBIM file from IFC model.");

            // set up xBIM logging. It will use your providers.
            XbimLogging.LoggerFactory = lf;

            const string fileName = @"" + IFC_FILE + ".ifc";
            log.LogInformation($"File size: {new FileInfo(fileName).Length / 1e6}MB");

            using (var model = IfcStore.Open(fileName, null, -1))
            {
                var context = new Xbim3DModelContext(model);
                context.CreateContext();

                var wexBimFilename = Path.ChangeExtension(fileName, "wexbim");
                using (var wexBimFile = File.Create(wexBimFilename))
                {
                    using (var wexBimBinaryWriter = new BinaryWriter(wexBimFile))
                    {
                        model.SaveAsWexBim(wexBimBinaryWriter);
                        wexBimBinaryWriter.Close();
                    }
                    wexBimFile.Close();
                }
            }

            GENERATE_JSON_FROM_IFC_FILE();
        }

        private static void GENERATE_JSON_FROM_IFC_FILE()
        {
            //https://github.com/xBimTeam/XbimEssentials/issues/94

            const string fileName = @"" + IFC_FILE + ".ifc";
            var model = IfcStore.Open(fileName);

            var facilities = new List<Facility>();
            var exchanger = new IfcToCOBieLiteUkExchanger(model, facilities);
            facilities = exchanger.Convert();

            //there might be more than one facilities in theory but 
            //COBie is only designed to hold a single building in a file.
            for (var i = 0; i < facilities.Count; i++)
            {
                var facility = facilities[i];
                var file = $"" + IFC_FILE + "-" + i + ".json";
                facility.WriteJson(file);
            }

        }
    }
}
