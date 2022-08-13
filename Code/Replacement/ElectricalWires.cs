namespace BOB
{
	using System.Collections.Generic;
	using AlgernonCommons;
	using UnityEngine;

	/// <summary>
	/// Struct to hold wire thickness details.
	/// </summary>
	public struct WireThickness
	{
		// Thickness records.
		public Vector2 main;
		public Vector2 component;


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="mainScale">Main material scaling</param>
		/// <param name="componentScale">Component material scaling</param>
		public WireThickness(Vector2 mainScale, Vector2 componentScale)
        {
			main = mainScale;
			component = componentScale;
        }
	}


	/// <summary>
	/// Class to handle changes to electrical wire visual appearance.
	/// </summary>
	internal class ElectricalWires
	{
		// Instance reference.
		private static ElectricalWires _instance;

		// Wire thickness dictionaries.
		private readonly Dictionary<NetInfo.Segment, WireThickness> segmentWires = new Dictionary<NetInfo.Segment, WireThickness>();
		private readonly Dictionary<NetInfo.Node, WireThickness> nodeWires = new Dictionary<NetInfo.Node, WireThickness>();


		/// <summary>
		/// Instance reference.
		/// </summary>
		internal static ElectricalWires Instance
		{
			get
			{
				// Create new instance if one doesn't exist.
				if (_instance == null)
				{
					_instance = new ElectricalWires();
				}

				// Return instance reference.
				return _instance;
			}
		}


		/// <summary>
		/// Applies a thinner size to electrial wires (networks using Custom/Net/Electricity shader).
		/// </summary>
		internal void ApplyThinnerWires()
		{
			// Use Railway Replacer figure for thinner wires.
			Vector2 thinnerWireScale = new Vector2(3.5f, 1f);

			/// Target shader name.
			string shaderName = "Custom/Net/Electricity";


			Logging.Message("thinning electrical wires");

			// Iterate thorugh each loaded net prefab.
			for (uint i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i)
			{
				NetInfo netPrefab = PrefabCollection<NetInfo>.GetLoaded(i);
				if (netPrefab?.m_segments != null)
				{
					// Iterate through each segment in prefab.
					foreach (NetInfo.Segment segment in netPrefab.m_segments)
					{
						// Check for electricity shader.
						Shader shader = segment?.m_material?.shader;
						if (shader != null && shader.name == shaderName)
						{
							// Using electricity shader - record original values (if we haven't already).
							if (!segmentWires.ContainsKey(segment))
							{
								segmentWires.Add(segment, new WireThickness(segment.m_material.mainTextureScale, segment.m_segmentMaterial.mainTextureScale));
							}

							// Rescale materials.
							segment.m_material.mainTextureScale = thinnerWireScale;
							segment.m_segmentMaterial.mainTextureScale = thinnerWireScale;
						}
					}

					// Iterate through each node in prefab.
					foreach (NetInfo.Node node in netPrefab.m_nodes)
					{
						// Check for electricity shader.
						Shader shader = node?.m_material?.shader;
						if (shader != null && shader.name == shaderName)
						{
							// Using electricity shader - record original values (if we haven't already).
							if (!nodeWires.ContainsKey(node))
							{
								nodeWires.Add(node, new WireThickness(node.m_material.mainTextureScale, node.m_nodeMaterial.mainTextureScale));
							}

							// Rescale materials.
							node.m_material.mainTextureScale = thinnerWireScale;
							node.m_nodeMaterial.mainTextureScale = thinnerWireScale;
						}
					}
				}
			}
		}


		/// <summary>
		/// Reverts thinner wire settings, restoring prefabs to their original state.
		/// </summary>
		internal void RevertThinnerWires()
        {
			Logging.Message("reverting electrical wires");

			// Iterate through segment dictionary, restoring original scaling values.
			foreach(KeyValuePair<NetInfo.Segment, WireThickness> segmentEntry in segmentWires)
            {
				segmentEntry.Key.m_material.mainTextureScale = segmentEntry.Value.main;
				segmentEntry.Key.m_segmentMaterial.mainTextureScale = segmentEntry.Value.component;
			}

			// Iterate through node dictionary, restoring original scaling values.
			foreach (KeyValuePair<NetInfo.Node, WireThickness> nodeEntry in nodeWires)
			{
				nodeEntry.Key.m_material.mainTextureScale = nodeEntry.Value.main;
				nodeEntry.Key.m_nodeMaterial.mainTextureScale = nodeEntry.Value.component;
			}

			// Clear dictionaries.
			segmentWires.Clear();
			nodeWires.Clear();
		}
	}
}