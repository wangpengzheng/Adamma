using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace WorkShop
{
    [XmlRoot(ElementName = "TFSFields")]
    public class TFSFields
    {
        private List<TFSField> fields = null;

        [XmlElement(ElementName = "TFSFields")]
        public List<TFSField> Fields
        {
            get { return fields; }
            set { fields = value; }
        }

        public TFSFields()
        {
            Fields = new List<TFSField>();
        }

        public void SaveTFSFields(string _path)
        {
            FileStream fs = null;
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(TFSFields));
                fs = new FileStream(_path, FileMode.Create, FileAccess.Write);
                xs.Serialize(fs, this);
                fs.Close();
            }
            catch
            {
                if (fs != null)
                    fs.Close(); 
            }
        }

        public static TFSFields LoadTFSFields(string _path)
        {
            FileStream fs = null;
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(TFSFields));
                fs = new FileStream(_path, FileMode.Open, FileAccess.Read);
                TFSFields tfsFields = (TFSFields)xs.Deserialize(fs);
                fs.Close();
                return tfsFields;
            }
            catch
            {
                if (fs != null)
                    fs.Close();

                return null;
            }
        }

        public Dictionary<String, int> GetEnabledTFSFieldsAndWidth()
        {
            Dictionary<String, int> enabledFields = new Dictionary<String, int>();
            this.Fields.Sort();
            foreach (TFSField field in this.Fields)
            {
                if (field.Enabled)
                    enabledFields.Add(field.FieldWiqName, field.FieldWidth);
            }

            return enabledFields;
        }

        public int FindTFSFieldsWidthWithFieldDisplayName(String _fieldDisplayName)
        {
            if (_fieldDisplayName == null || _fieldDisplayName == "")
                return 100;

            foreach (TFSField tfsField in this.fields)
            {
                if (tfsField.FieldDisName == _fieldDisplayName)
                    return tfsField.FieldWidth;
            }

            return 100;
        }
    }

    public class TFSField : IComparable
    {
        private string fieldName;
        private string fieldWiqName;
        private string fieldDisName;
        private int fieldWidth;
        private int fieldNum;
        private Boolean enabled;

        [XmlAttribute(AttributeName = "FieldName")]
        public string FieldName
        {
            get { return fieldName; }
            set { fieldName = value;}
        }

        [XmlElement(ElementName = "fieldWiqName")]
        public string FieldWiqName
        {
            get { return fieldWiqName; }
            set { fieldWiqName = value; }
        }

        [XmlElement(ElementName = "fieldDisName")]
        public string FieldDisName
        {
            get { return fieldDisName; }
            set { fieldDisName = value; }
        }

        [XmlElement(ElementName = "FieldWidth")]
        public int FieldWidth
        {
            get { return fieldWidth; }
            set { fieldWidth = value; }
        }

        [XmlElement(ElementName = "FieldNum")]
        public int FieldNum
        {
            get { return fieldNum; }
            set { fieldNum = value; }
        }

        [XmlElement(ElementName = "Enabled")]
        public Boolean Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        public int CompareTo(object _tfsField)
        {
            TFSField tfsField = _tfsField as TFSField;

            return this.fieldNum.CompareTo(tfsField.fieldNum);
        }
    }
}
