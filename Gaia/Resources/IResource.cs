using System.Xml;

namespace Gaia.Resources
{
    public interface IResource
    {
        string Name { get; }
        void Destroy();
        void LoadFromXML(XmlNode node);
    }
}
