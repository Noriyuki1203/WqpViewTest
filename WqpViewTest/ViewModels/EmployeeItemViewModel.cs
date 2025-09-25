using System;

namespace WqpViewTest.ViewModels
{
    public class EmployeeItemViewModel : ObservableObject
    {
        private readonly Func<EmployeeItemViewModel, bool>? _onIsActiveChanged;
        private bool _isActive;

        public int EmployeeId { get; }
        public string Name { get; }
        public int Age { get; }
        public int DepartmentId { get; }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value)
                {
                    return;
                }

                bool oldValue = _isActive;
                _isActive = value;
                OnPropertyChanged();

                if (_onIsActiveChanged != null && !_onIsActiveChanged(this))
                {
                    _isActive = oldValue;
                    OnPropertyChanged();
                }
            }
        }

        public EmployeeItemViewModel(int employeeId, string name, int age, int departmentId, bool isActive, Func<EmployeeItemViewModel, bool>? onIsActiveChanged)
        {
            EmployeeId = employeeId;
            Name = name;
            Age = age;
            DepartmentId = departmentId;
            _isActive = isActive;
            _onIsActiveChanged = onIsActiveChanged;
        }
    }
}
