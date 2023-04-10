using System.Linq;

public static class AtomExtensions
{
    public static JSONStorable GetPluginStorableById(this Atom atom, string id)
    {
        string storableIdName = atom.GetStorableIDs()
            .FirstOrDefault(storeId => !string.IsNullOrEmpty(storeId) && storeId.Contains(id));
        return storableIdName == null ? null : atom.GetStorableByID(storableIdName);
    }
}
