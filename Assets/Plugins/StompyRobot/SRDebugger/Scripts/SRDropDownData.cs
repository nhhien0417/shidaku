using System;

namespace SRDebugger
{
    public class SRDropDownData
    {
        public event Action<int> SelectionChanged;
        public event Action OptionsChanged;
        public object[] Options;
        public object[] Values;
        private int _selectionIndex;

        public object SelectedValue => Values[_selectionIndex];
        
        public int SelectionIndex
        {
            get => _selectionIndex;
            set
            {
                if (_selectionIndex == value)
                {
                    return;
                }

                _selectionIndex = value;
                SelectionChanged?.Invoke(_selectionIndex);
            }
        }

        public void SetValueWithoutNotify(int value)
        {
            _selectionIndex = value;
        }
        
        public void UpdateOptions(object[] options, object[] values)
        {
            _selectionIndex = 0;
            Options = options;
            Values = values;
            OptionsChanged?.Invoke();
        }

        public SRDropDownData(int selectionIndex, object[] options, object[] values)
        {
            _selectionIndex = selectionIndex;
            Options = options;
            Values = values;
        }
    }
}
