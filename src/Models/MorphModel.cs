using UnityEngine;

namespace ExpressionRND.Models
{
    sealed class MorphModel
    {
        readonly DAZMorph _morph;
        public string DisplayName { get; }
        public string FinalTwoRegions { get; }
        public string FinalRegion { get; }
        public string Label { get; }
        public bool DefaultOn { get; set; }
        public bool Preset1On { get; set; }
        public bool Preset2On { get; set; }
        public JSONStorableBool EnabledJsb { get; set; }

        readonly float _initialMorphValue;
        float _defaultMorphValue;
        float _currentMorphValue;
        float _newMorphValue;

        public MorphModel(DAZMorph morph, string displayName, string region)
        {
            _morph = morph;
            DisplayName = displayName;

            string[] regions = region.Split('/');
            int lastIndex = regions.Length - 1;
            int secondLastIndex = lastIndex - 1;
            FinalRegion = lastIndex > -1 ? regions[lastIndex] : "";
            FinalTwoRegions = secondLastIndex > -1 ? regions[secondLastIndex] + "/" + FinalRegion : FinalRegion;

            Label = FinalRegion + "/" + DisplayName;
            _initialMorphValue = _morph.morphValue;
            _defaultMorphValue = _initialMorphValue;
            _morph.morphValue = 0;
            _currentMorphValue = _morph.morphValue;
        }

        public void CalculateMorphValue(float interpolant)
        {
            _currentMorphValue = Mathf.Lerp(_currentMorphValue, _newMorphValue, interpolant);
            _morph.morphValue = _currentMorphValue;
        }

        public void SetNewMorphValue(float min, float max, float multi, bool aba)
        {
            _newMorphValue = aba && _currentMorphValue > 0.1f
                ? 0
                : Random.Range(min, max) * multi;
        }

        public void UpdateDefaultValue()
        {
            _defaultMorphValue = _morph.morphValue;
        }

        public void ResetToDefault()
        {
            _morph.morphValue = _defaultMorphValue;
        }

        public void ResetToInitial()
        {
            _morph.morphValue = _initialMorphValue;
        }

        public void ZeroValue()
        {
            _morph.morphValue = 0;
        }
    }
}
