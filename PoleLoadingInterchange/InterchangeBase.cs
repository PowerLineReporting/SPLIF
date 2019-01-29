using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PoleLoadingInterchange
{
    public class Model
    {
        public List<InterchangeBase> Components = new List<InterchangeBase>();

        public InterchangeBase AddComponent(string pComponentType)
        {
            foreach (Type type in this.GetType().Assembly.GetTypes())
            {
                if (type.Name == pComponentType)
                {
                    InterchangeBase instance = (InterchangeBase)Activator.CreateInstance(type, this);
                    return instance;
                }
            }
            return null;
        }


        //enumerators
        public IEnumerable<Pole> Poles
        {
            get
            {
                foreach (InterchangeBase tobj in Components)
                {
                    if(tobj is Pole obj) yield return obj;
                }
            }
        }

        public IEnumerable<InterchangeBase> ComponentsAttachedTo(InterchangeBase pComponent)
        {
            foreach (InterchangeBase tobj in Components)
            {
                if (tobj is InterchangeSingleAttachment saobj && saobj.AttachedTo == pComponent) yield return saobj;
                else if (tobj is InterchangeDoubleAttachment daobj && (daobj.Attachment1 == pComponent  ||  daobj.Attachment2 == pComponent)) yield return daobj;
            }
        }

        public InterchangeBase this[Guid key]
        {
            get
            {
                foreach (InterchangeBase ib in Components)
                {
                    if (ib.UniqueID == key) return ib; 
                }
                return null;
            }
        }

        public void Save(string pFileName)
        {
            using (XmlTextWriter pWriter = new XmlTextWriter(pFileName, null))
            {
                pWriter.Formatting = Formatting.Indented;
                pWriter.Indentation = 4;

                pWriter.WriteStartDocument();
                pWriter.WriteStartElement("PoleLoadingInterchange");
                foreach (InterchangeBase element in Components)
                {
                    element.Save(pWriter);
                }
                pWriter.WriteEndElement();
                pWriter.WriteEndDocument();
            }
        }

        public void Load(string pFileName)
        {
            using (XmlTextReader pReader = new XmlTextReader(pFileName))
            {
                while (pReader.Read())
                {
                    if (pReader.NodeType == XmlNodeType.Element)
                    {
                        if (pReader.Name == "PoleLoadingInterchange") continue;
                        InterchangeBase obj = AddComponent(pReader.Name);
                        obj.Load(pReader);
                    }
                }
            }
            using (XmlTextReader pReader = new XmlTextReader(pFileName))
            {
                int idx = 0;
                while (pReader.Read())
                {
                    if (pReader.NodeType == XmlNodeType.Element)
                    {
                        if (pReader.Name == "PoleLoadingInterchange") continue;
                        if (pReader.Name == "AncillaryInfo") continue;
                        InterchangeBase obj = Components[idx];
                        idx++;
                        if (obj is InterchangeSingleAttachment saobj)
                        {
                            string sguid = pReader.GetAttribute("AttachedTo");
                            if (sguid != null)
                            {
                                saobj.AttachedTo = this[new Guid(sguid)];
                            }
                        }
                        else if (obj is InterchangeDoubleAttachment daobj)
                        {
                            string sguid = pReader.GetAttribute("Attachment1");
                            if (sguid != null)
                            {
                                daobj.Attachment1 = this[new Guid(sguid)];
                            }
                            sguid = pReader.GetAttribute("Attachment2");
                            if (sguid != null)
                            {
                                daobj.Attachment2 = this[new Guid(sguid)];
                            }
                        }
                    }
                }
            }
        }

    }



    public class InterchangeBase
    {
        public InterchangeBase(Model pModel)
        {
            if (pModel == null) return;
            pModel.Components.Add(this);
        }

        public Guid UniqueID = Guid.NewGuid();

        public string ConfigKey { set; get; }

        private Dictionary<string, string> m_AncillaryInfo = null;

        public void AddAncillaryInfo(string pName, string pValue)
        {
            if (m_AncillaryInfo == null) m_AncillaryInfo = new Dictionary<string, string>();
            m_AncillaryInfo.Add(pName, pValue);
        }

        public void Save(XmlTextWriter pWriter)
        {
            string elemName = this.GetType().ToString();
            elemName = elemName.Substring(elemName.LastIndexOf('.') + 1);
            pWriter.WriteStartElement(elemName);
            pWriter.WriteAttributeString("UniqueID", this.UniqueID.ToString());
            foreach (PropertyInfo fi in this.GetType().GetProperties(
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.SetProperty | BindingFlags.GetProperty))
            {
                string propName = fi.Name;
                Type propType = fi.PropertyType;
                object val = fi.GetValue(this);
                string value;
                if (val == null) continue;
                else if (val is InterchangeSingleAttachment ib) value = ib.UniqueID.ToString();
                else value = val.ToString();
                pWriter.WriteAttributeString(propName, value);
            }
            if (m_AncillaryInfo != null && m_AncillaryInfo.Count > 0)
            {
                pWriter.WriteAttributeString("AncillaryInfos", m_AncillaryInfo.Count.ToString());
                foreach (KeyValuePair<string, string> kvp in m_AncillaryInfo)
                {
                    pWriter.WriteStartElement("AncillaryInfo");
                    pWriter.WriteAttributeString("Name", kvp.Key);
                    pWriter.WriteAttributeString("Value", kvp.Value);
                    pWriter.WriteEndElement();
                }
            }
            pWriter.WriteEndElement();
        }

        public void Load(XmlTextReader pReader)
        {
            this.UniqueID = new Guid(pReader.GetAttribute("UniqueID"));
            foreach (PropertyInfo fi in this.GetType().GetProperties(
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.SetProperty | BindingFlags.GetProperty))
            {
                string propName = fi.Name;
                string svalue = pReader.GetAttribute(propName);
                if (svalue == null) continue;
                Type propType = fi.PropertyType;
                if (propType == typeof(double))
                {
                    fi.SetValue(this, Convert.ToDouble(svalue));
                }
                else if (propType == typeof(string))
                {
                    fi.SetValue(this, svalue);
                }
                else
                {
                    //Connect on second pass
                }
            }
            string sanc = pReader.GetAttribute("AncillaryInfos");
            if (sanc != null)
            {
                pReader.Read();
                for (int idx = 0; idx < Convert.ToInt32(sanc); idx++)
                {
                    while (!(pReader.NodeType == XmlNodeType.Element && pReader.Name == "AncillaryInfo")) pReader.Read();
                    AddAncillaryInfo(pReader.GetAttribute("Name"), pReader.GetAttribute("Value"));
                    pReader.Read();
                }
            }

        }

    }

    public class InterchangeSingleAttachment : InterchangeBase
    {
        public InterchangeSingleAttachment(Model pModel) : base(pModel) { }

        public InterchangeBase AttachedTo { set; get; }
    }

    public class InterchangeDoubleAttachment : InterchangeBase
    {
        public InterchangeDoubleAttachment(Model pModel) : base(pModel) { }

        private InterchangeBase m_Attachment1 = null;
        private InterchangeBase m_Attachment2 = null;

        public InterchangeBase Attachment1
        {
            set
            {
                m_Attachment1 = value;
            }
            get
            {
                return m_Attachment1;
            }
        }

        public InterchangeBase Attachment2
        {
            set
            {
                m_Attachment2 = value;
            }
            get
            {
                return m_Attachment2;
            }
        }

    }

    /// <summary>
    /// Defines a pole
    /// </summary>
    public class Pole : GroundAttachmentPoint
    {
        public Pole(Model pModel) : base(pModel) { }
        public double SettingDepthMeters { set; get; }
        public double LeanAmountDegrees { set; get; }
        public double LeanDirectionDegrees { set; get; }
    }

    /// <summary>
    /// Crossarm
    /// </summary>
    public class Crossarm : InterchangeSingleAttachment
    {
        public Crossarm(Model pModel) : base(pModel) { }
        public double AttachmentHeightMeters { set; get; }
        public double AttachmentOrientationDegrees { set; get; }
        public double AttachmentOffsetMeters { set; get; }
    }

    public class WoodBeamSimple : InterchangeDoubleAttachment
    {
        public WoodBeamSimple(Model pModel) : base(pModel) { }
        public double LenghtInMeters { set; get; }
    }

    public class WoodBeamComplex : InterchangeBase
    {
        public WoodBeamComplex(Model pModel) : base(pModel) { }
        public double LenghtInMeters { set; get; }
    }

    /// <summary>
    /// Pole Attachment
    /// </summary>
    public class PoleAttachmentPoint : InterchangeSingleAttachment
    {
        public PoleAttachmentPoint(Model pModel) : base(pModel) { }
        public double AttachmentHeightMeters { set; get; }
        public double AttachmentOrientationDegrees { set; get; }
    }

    /// <summary>
    /// Arm Attachment
    /// </summary>
    public class BeamAttachmentPoint : InterchangeSingleAttachment
    {
        public BeamAttachmentPoint(Model pModel) : base(pModel) { }
        public double AttachmentOffsetMeters { set; get; }
    }

    public class Joint : InterchangeDoubleAttachment
    {
        public Joint(Model pModel) : base(pModel) { }
    }


    /// <summary>
    /// Span Attachment
    /// </summary>
    public class SpanAttachmentPoint : InterchangeSingleAttachment
    {
        public SpanAttachmentPoint(Model pModel) : base(pModel) { }
        public double AttachmentOffsetMeters { set; get; }
    }

    public class FreeAttachmentPoint : GroundAttachmentPoint
    {
        public FreeAttachmentPoint(Model pModel) : base(pModel) { }
        public double AttachmentPointHeightInMeters { set; get; }
    }

    public class GroundAttachmentPoint : InterchangeSingleAttachment
    {
        public GroundAttachmentPoint(Model pModel) : base(pModel) { }
        public double Latitude { set; get; }
        public double Longitude { set; get; }
        public double ElevationMeteraAboveMSL { set; get; }
    }

    public class Equipment : InterchangeSingleAttachment
    {
        public Equipment(Model pModel) : base(pModel) { }

    }

    public class Span : InterchangeDoubleAttachment
    {
        public Span(Model pModel) : base(pModel) { }
    }

    public class Guy : InterchangeDoubleAttachment
    {
        public Guy(Model pModel) : base(pModel) { }
    }

    public class SidewalkGuyStrut : InterchangeDoubleAttachment
    {
        public SidewalkGuyStrut(Model pModel) : base(pModel) { }
    }
}
