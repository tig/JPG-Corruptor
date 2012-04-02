//===================================================================
// JPG Corruptor http://tig.github.com/JPG-Corruptor
//
// Copyright © 2012 Charlie Kindel. 
// Licensed under the MIT License.
// Source code control at http://github.com/tig/JPG-Corruptor
//===================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace JPGCorrupt
{
    public class TextImagePair
    {
        [XmlAttribute("TextFile")]
        public String TextFile { get ;set;}

        [XmlAttribute("ImageFile")]
        public String ImageFile { get; set; }
    }

	public class Settings
	{
        private const string SettingsFileName = "JPGCorrupt.settings";

        static public void SerializeToXML(Settings settings)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            TextWriter textWriter = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\" + SettingsFileName);
            serializer.Serialize(textWriter, settings);
            textWriter.Close();
        }

        static public Settings DeserializeFromXML()
        {
            try
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(Settings));
                using (TextReader textReader = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\" + SettingsFileName))
                {
                    Settings settings = (Settings)deserializer.Deserialize(textReader);
                    textReader.Close();
                    return settings;
                }
            }
            catch (FileNotFoundException)
            {
                return new Settings();
            }
        }

        public Settings()
        {
            list = new TextImagePair[1];
            list[0] = new TextImagePair();
            TextFile = "";
            ImageFile = "";
        }

        [XmlElement("AutoStart")]
        public bool AutoStart { get; set; }

        [XmlElement("FullScreen")]
        public bool FullScreen { get; set; }

        [XmlElement("Loop")]
        public bool Loop { get; set; }

        [XmlArray("Files")]
        [XmlArrayItem("TextImagePair")]
        public TextImagePair[] list;

        [XmlIgnore]
        public String TextFile
        {
            get { return list[0].TextFile; }
            set { list[0].TextFile = value; }
        }

        [XmlIgnore]
        public String ImageFile
        {
            get { return list[0].ImageFile; }
            set { list[0].ImageFile = value; }
        }

	}
}
