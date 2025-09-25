namespace WqpViewTest.Models
{
    public class Employee
    {
        public int EmployeeId { get; }
        public string Name { get; }
        public int Age { get; }
        public int DepartmentId { get; }
        public bool IsActive { get; }

        public Employee(int employeeId, string name, int age, int departmentId, bool isActive)
        {
            EmployeeId = employeeId;
            Name = name;
            Age = age;
            DepartmentId = departmentId;
            IsActive = isActive;
        }

        public Employee WithIsActive(bool isActive)
        {
            if (IsActive == isActive)
            {
                return this;
            }

            return new Employee(EmployeeId, Name, Age, DepartmentId, isActive);
        }
    }
}
