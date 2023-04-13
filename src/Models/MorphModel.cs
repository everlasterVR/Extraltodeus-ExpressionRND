using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace ExpressionRND.Models
{
    sealed class MorphModel
    {
        readonly DAZMorph _morph;
        public string DisplayName { get; }
        public string FinalTwoRegions { get; }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public string FinalRegion { get; }

        public string Label { get; }

        public JSONStorableBool EnabledJsb { get; set; }

        float _initialMorphValue;
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
            _currentMorphValue = Utils.RoundToDecimals(_morph.morphValue);
        }

        public void CalculateMorphValue(float interpolant)
        {
            _currentMorphValue = Utils.RoundToDecimals(Mathf.Lerp(_currentMorphValue, _newMorphValue, interpolant));
            _morph.morphValue = _currentMorphValue;
        }

        public void SetNewMorphValue(float min, float max, float multi, bool aba)
        {
            _newMorphValue = aba && _currentMorphValue > 0.1f
                ? 0
                : Utils.RoundToDecimals(Random.Range(min, max) * multi);
        }

        public void UpdateInitialValue()
        {
            _initialMorphValue = Utils.RoundToDecimals(_morph.morphValue);
        }

        public bool SmoothResetMorphValue(float interpolant)
        {
            _currentMorphValue = Utils.RoundToDecimals(Mathf.Lerp(_currentMorphValue, _initialMorphValue, interpolant));
            bool finished = Mathf.Abs(_currentMorphValue - _initialMorphValue) < 0.001f;
            if(finished)
            {
                _currentMorphValue = _initialMorphValue;
            }

            _morph.morphValue = _currentMorphValue;
            return finished;
        }

        public void ResetToInitial()
        {
            _currentMorphValue = _initialMorphValue;
            _morph.morphValue = _currentMorphValue;
        }

        public void ZeroValue()
        {
            _morph.morphValue = 0;
        }
    }
}
