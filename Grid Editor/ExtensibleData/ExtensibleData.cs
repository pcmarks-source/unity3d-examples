using System.Collections.Generic;
using UnityEngine;

public class ExtensibleData {

    public string instructionSetID { get; protected set; }
    public GameObject prefab { get; protected set; }

    private int UniqueID;
    public int uniqueID {
        get {
            return UniqueID;
        }

        set {
            UniqueID = value;
            if (UniqueID > Metrics.uniqueID) Metrics.uniqueID = UniqueID;
        }
    }
    public CellData cellData { get; protected set; }
    public Direction orientation { get; protected set; }
    public GameObject gameObject { get; protected set; }

    public LightMode lightMode { get; protected set; }
    public DoorMode doorMode { get; protected set; }
    public bool isInteractable { get; protected set; }
    public bool useVFX { get; protected set; }

    protected Dictionary<string, ExtensionData> extensions = new Dictionary<string, ExtensionData>();

    public ExtensionData GetExtension(string identifier) {
        if (string.IsNullOrEmpty(identifier) == true) return null;
        if (extensions == null || extensions.Count <= 0) return null;
        if (extensions.ContainsKey(identifier) == false) return null;

        return extensions[identifier];
    }

    public Dictionary<string, ExtensionData> GetExtensions() {
        return extensions;
    }
}
