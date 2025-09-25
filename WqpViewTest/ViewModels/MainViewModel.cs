using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace WqpViewTest.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly DatabeseHelper _databaseHelper = new();
        private DepartmentItemViewModel? _selectedDepartment;

        public ObservableCollection<DepartmentItemViewModel> Departments { get; } = new();
        public ObservableCollection<EmployeeItemViewModel> Employees { get; } = new();

        public DepartmentItemViewModel? SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (!SetProperty(ref _selectedDepartment, value))
                {
                    return;
                }

                if (_selectedDepartment == null)
                {
                    Employees.Clear();
                }
                else
                {
                    LoadEmployeesForSelectedDepartment();
                }
            }
        }

        public RelayCommand RefreshDepartmentsCommand { get; }

        public event Action<string>? ErrorOccurred;
        public event Action<string>? InfoOccurred;

        public MainViewModel()
        {
            RefreshDepartmentsCommand = new RelayCommand(LoadDepartments);
        }

        public void Initialize()
        {
            try
            {
                _databaseHelper.EnsureDatabase();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"DB初期化エラー:\n{ex.Message}");
                return;
            }

            LoadDepartments();
        }

        public void LoadDepartments(int? departmentIdToSelect = null)
        {
            int? targetDepartmentId = departmentIdToSelect ?? SelectedDepartment?.DepartmentId;

            Departments.Clear();

            try
            {
                foreach (var department in _databaseHelper.GetDepartments())
                {
                    Departments.Add(new DepartmentItemViewModel(department.DepartmentId, department.DepartmentName));
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Departments 読み込みエラー:\n{ex.Message}");
            }

            SelectedDepartment = Departments.FirstOrDefault(d => d.DepartmentId == targetDepartmentId);

            if (SelectedDepartment == null)
            {
                Employees.Clear();
            }
        }

        private void LoadEmployeesForSelectedDepartment()
        {
            Employees.Clear();

            if (SelectedDepartment == null)
            {
                return;
            }

            try
            {
                foreach (var employee in _databaseHelper.GetEmployeesByDepartment(SelectedDepartment.DepartmentId))
                {
                    Employees.Add(new EmployeeItemViewModel(
                        employee.EmployeeId,
                        employee.Name,
                        employee.Age,
                        employee.DepartmentId,
                        employee.IsActive,
                        OnEmployeeIsActiveChanged));
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"Employees 読み込みエラー:\n{ex.Message}");
            }
        }

        private bool OnEmployeeIsActiveChanged(EmployeeItemViewModel employee)
        {
            try
            {
                _databaseHelper.UpdateEmployeeIsActive(employee.EmployeeId, employee.IsActive);
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"フラグ更新エラー:\n{ex.Message}");
                return false;
            }
        }

        public bool TryRenameDepartment(DepartmentItemViewModel? department, string newName)
        {
            if (department == null)
            {
                return false;
            }

            try
            {
                _databaseHelper.UpdateDepartmentName(department.DepartmentId, newName);
                LoadDepartments(department.DepartmentId);
                InfoOccurred?.Invoke("部署名を更新しました。");
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"部署名更新エラー:\n{ex.Message}");
                return false;
            }
        }
    }
}
