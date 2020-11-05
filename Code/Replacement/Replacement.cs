using System.Xml.Serialization;


namespace BOB
{
	/// <summary>
	///  Individual tree/prop replacement record.
	/// </summary>
	public class Replacement
	{
		[XmlAttribute("IsTree")]
		public bool isTree = false;

		[XmlAttribute("TargetIndex")]
		public int targetIndex = -1;

		[XmlAttribute("TargetName")]
		public string targetName = string.Empty;

		[XmlAttribute("ReplaceName")]
		public string replaceName = string.Empty;

		[XmlAttribute("Probability")]
		public int probability = 100;

		[XmlAttribute("Angle")]
		public float angle = 0f;

		[XmlIgnore]
		public PrefabInfo replacementInfo;

		[XmlIgnore]
		public PrefabInfo targetInfo;

		[XmlIgnore]
		public int originalProb;
	}


	public class NetReplacement : Replacement
    {
		[XmlAttribute("Lane")]
		public int lane = -1;

		[XmlAttribute("OffsetX")]
		public float offsetX = 0f;

		[XmlAttribute("OffsetY")]
		public float offsetY = 0f;

		[XmlAttribute("OffsetZ")]
		public float offsetZ = 0f;
	}
}