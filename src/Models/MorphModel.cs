using UnityEngine;

namespace ExpressionRND.Models
{
    sealed class MorphModel
    {
        readonly DAZMorph _morph;
        public string DisplayName { get; }
        public string FinalTwoRegions { get; }
        public string Label { get; }
        public JSONStorableBool EnabledJsb { get; set; }
        public float SmoothResetTimer { get; set; }

        float _previousValue;
        float _initialValue;
        float _currentValue;
        float _targetValue;

        public MorphModel(DAZMorph morph, string displayName, string region)
        {
            _morph = morph;
            DisplayName = displayName;

            string[] regions = region.Split('/');
            int lastIndex = regions.Length - 1;
            int secondLastIndex = lastIndex - 1;
            string finalRegion = lastIndex > -1 ? regions[lastIndex] : "";
            FinalTwoRegions = secondLastIndex > -1 ? regions[secondLastIndex] + "/" + finalRegion : finalRegion;
            Label = finalRegion + "/" + DisplayName;
            _initialValue = _morph.morphValue;
            _currentValue = Utils.RoundToDecimals(_morph.morphValue);
        }

        public void CalculateValue(float interpolant)
        {
            _previousValue = _currentValue;
            _currentValue = Utils.RoundToDecimals(Mathf.Lerp(_currentValue, _targetValue, interpolant));
            _morph.morphValue = _currentValue;
        }

        public void SetTargetValue(float min, float max, float multi, bool resetUsedExpressionsAtLoop)
        {
            _targetValue = resetUsedExpressionsAtLoop && _currentValue > 0.1f
                ? 0f
                : Utils.RoundToDecimals(Random.Range(min, max) * multi);
        }

        public void UpdateInitialValue()
        {
            _initialValue = Utils.RoundToDecimals(_morph.morphValue);
        }

        public bool SmoothResetValue(float interpolant)
        {
            _previousValue = _currentValue;
            _currentValue = Utils.RoundToDecimals(Mathf.Lerp(_currentValue, _initialValue, interpolant));
            /* Could get easily stuck to non-initial value otherwise... not sure why exactly */
            bool finished = Mathf.Abs(_currentValue - _initialValue) < 0.02f && Mathf.Abs(_previousValue - _currentValue) < 0.002f;
            if(finished)
            {
                _currentValue = _initialValue;
            }

            _morph.morphValue = _currentValue;
            return finished;
        }

        public void ResetToInitial()
        {
            _currentValue = _initialValue;
            _previousValue = _currentValue;
            _morph.morphValue = _currentValue;
        }

        public void ZeroValue()
        {
            _initialValue = 0f;
            ResetToInitial();
        }
    }
}
