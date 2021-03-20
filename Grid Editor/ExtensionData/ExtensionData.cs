using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

public class ExtensionData : IXmlSerializable {

    public int ownerUniqueID { get; protected set; }
    public string identifier { get; protected set; }

    public virtual void Initialize(int ownerUniqueID) {

    }

    public virtual void FixedInstructionCycle() {

    }

    public virtual void InstructionCycle() {

    }

    public virtual void LateInstructionCycle() {

    }

    public virtual void Switch() {

    }

    public ExtensionData() {

    }

    public virtual XmlSchema GetSchema() {
        return null;
    }

    public virtual void WriteXml(XmlWriter writer) {

    }

    public virtual void ReadXml(XmlReader reader) {

    }
}
