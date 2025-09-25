namespace WqpViewTest.ViewModels
{
    public class DepartmentItemViewModel : ObservableObject
    {
        private string _departmentName;

        public int DepartmentId { get; }

        public string DepartmentName
        {
            get => _departmentName;
            set => SetProperty(ref _departmentName, value);
        }

        public DepartmentItemViewModel(int departmentId, string departmentName)
        {
            DepartmentId = departmentId;
            _departmentName = departmentName;
        }

        public override string ToString() => DepartmentName;
    }
}
