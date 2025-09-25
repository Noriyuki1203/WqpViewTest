namespace WqpViewTest.Models
{
    public class Department
    {
        public int DepartmentId { get; }
        public string DepartmentName { get; }

        public Department(int departmentId, string departmentName)
        {
            DepartmentId = departmentId;
            DepartmentName = departmentName;
        }
    }
}
