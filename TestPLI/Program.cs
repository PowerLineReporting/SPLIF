using PoleLoadingInterchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TestPLI
{
    class Program
    {
        const double foot = 0.3084;

        static void Main(string[] args)
        {

            //create model
            Model model = new Model();
            List<BeamAttachmentPoint> ins1 = AddPoleToModel(model, 43.04812, -76.1474, 102.99);
            List<BeamAttachmentPoint> ins2 = AddPoleToModel(model, 43.04851, -76.1474, 103.75);
            for (int idx = 0; idx < 3; idx++)
            {
                Span span = new Span(model);
                span.ConfigKey = "AAC 1/0";
                span.Attachment1 = ins1[idx];
                span.Attachment2 = ins2[idx];
            }
            model.Save(@"c:\temp\junk.xml");
            Model test = new Model();
            test.Load(@"c:\temp\junk.xml");
            test.Save(@"c:\temp\junkrt.xml");
        }

        static List<BeamAttachmentPoint> AddPoleToModel(Model model, double lat, double lon, double elev)
        {

            //add pole to model
            Pole pole = model.AddComponent("Pole") as Pole;
            pole.AddAncillaryInfo("Test1", "Value1");
            pole.AddAncillaryInfo("Test2", "Value2");
            pole.AddAncillaryInfo("Test3", "Value3");
            pole.AddAncillaryInfo("Test4", "Value4");

            pole.ConfigKey = "40_4_SP";
            pole.SettingDepthMeters = 6 * foot;
            pole.Latitude = lat;
            pole.Longitude = lon;
            pole.ElevationMeteraAboveMSL = elev;

            //add crossarm to pole
            Crossarm arm = new Crossarm(model);
            arm.AttachedTo = pole;
            arm.AttachmentOffsetMeters = 4 * foot;
            arm.ConfigKey = "8ft Std Arm";


            //add three insulators to the crossarm
            List<BeamAttachmentPoint> insulators = new List<BeamAttachmentPoint>();
            for (double x = foot / 2; x < 7.75 * foot; x += 3 * foot)
            {
                BeamAttachmentPoint ins = new BeamAttachmentPoint(model);
                ins.ConfigKey = "7.5\" Pin";
                ins.AttachmentOffsetMeters = x;
                ins.AttachedTo = arm;
                insulators.Add(ins);
            }
            return insulators;
        }
    }
}
