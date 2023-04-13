using System.Collections.Generic;
using System.Linq;

namespace ExpressionRND.Models
{
    sealed class Person
    {
        public Atom Atom { get; }
        readonly DAZCharacterSelector _geometry;
        readonly GenerateDAZMorphsControlUI _morphsControlUI;

        public bool isFemale { get; }

        public Person(Atom atom)
        {
            Atom = atom;
            _geometry = (DAZCharacterSelector) this.Atom.GetStorableByID("geometry");
            _morphsControlUI = _geometry.morphsControlUI;

            isFemale = !_geometry.selectedCharacter.isMale;
            Atom.GetStorableByID("AutoExpressions").SetBoolParamValue("enabled", false);
        }

        public IEnumerable<DAZMorph> GetNonBoneMorphs()
        {
            return _morphsControlUI.GetMorphs()
                .Where(morph => !morph.hasBoneModificationFormulas && !morph.hasBoneRotationFormulas);
        }

        public CollisionTrigger GetCollisionTrigger(string storableId)
        {
            return Atom.GetStorableByID(storableId) as CollisionTrigger;
        }

        public bool Exists()
        {
            return Atom != null && Atom.gameObject;
        }
    }
}
