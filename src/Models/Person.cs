using System.Collections.Generic;
using System.Linq;

namespace ExpressionRND.Models
{
    sealed class Person
    {
        public Atom Atom { get; }
        readonly DAZCharacterSelector _geometry;
        readonly GenerateDAZMorphsControlUI _morphsControlUI;
        // readonly GenerateDAZMorphsControlUI _morphsControlUIOtherGender;
        DAZCharacter _selectedCharacter;

        // TODO male & use other gender morphs support
        // readonly JSONStorableBool _useMaleMorphsOnFemaleJsb;
        // readonly JSONStorableBool _useFemaleMorphsOnMaleJsb;

        // public bool isFemale { get; }

        public Person(Atom atom)
        {
            Atom = atom;
            _geometry = (DAZCharacterSelector) Atom.GetStorableByID("geometry");
            _morphsControlUI = _geometry.morphsControlUI;
            _selectedCharacter = _geometry.selectedCharacter;

            // _useMaleMorphsOnFemaleJsb = _geometry.GetBoolJSONParam("useMaleMorphsOnFemale");
            // _useFemaleMorphsOnMaleJsb = _geometry.GetBoolJSONParam("useFemaleMorphsOnMale");

            // _useMaleMorphsOnFemaleJsb.setCallbackFunction += RefreshMorphs;
            // _useFemaleMorphsOnMaleJsb.setCallbackFunction += RefreshMorphs;

            // isFemale = !_geometry.selectedCharacter.isMale;
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
            return Atom && Atom.gameObject;
        }

        public bool GenderChanged()
        {
            bool changed = _selectedCharacter.isMale != _geometry.selectedCharacter.isMale;
            _selectedCharacter = _geometry.selectedCharacter;
            return changed;
        }

        // public void Destroy()
        // {
        //     _useMaleMorphsOnFemaleJsb.setCallbackFunction -= RefreshMorphs;
        //     _useFemaleMorphsOnMaleJsb.setCallbackFunction -= RefreshMorphs;
        // }
    }
}
