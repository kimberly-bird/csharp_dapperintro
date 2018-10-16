using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using System.Text;
using Dapper;
using DapperDepartments.Data;
using DapperDepartments.Models;

/*
    To install required packages from NuGet
        1. `dotnet add package Microsoft.Data.Sqlite`
        2. `dotnet add package Dapper`
        3. `dotnet restore`
 */

namespace DapperDepartments
{
    class Program
    {
        static void Main(string[] args)
        {
            SqliteConnection db = DatabaseInterface.Connection;
            DatabaseInterface.CheckDepartmentTable();
            DatabaseInterface.CheckEmployeeTable();

            /*
                1. Query database - get back the 3 departments and make them 3 dept object instances
                2. Convert result to list 
                3. Use ForEach to iterate the collection
            */
            List<Department> departments = db.Query<Department>(@"SELECT * FROM Department").ToList();
            departments.ForEach(dept => Console.WriteLine($"{dept.DeptName}"));

            // Chaining LINQ statements together - shortened version of the departments one above
            db.Query<Employee>(@"SELECT * FROM Employee")
              .ToList()
              .ForEach(emp => Console.WriteLine($"{emp.FirstName} {emp.LastName}"));


            /*
                Query the database for each employee, and join in the employee's department
                Since an employee is only assigned to one department at a time, you can simply
                assign the corresponding department as a property on the instance of the
                Employee class that is created by Dapper.
             */

             // 2 tables querying, last is what should the the end result object? - Must follow the pattern/order of how they are listed in the <> and in the select statement
            db.Query<Employee, Department, Employee>(@"
                SELECT e.Id,
                       e.FirstName,
                       e.LastName,
                       d.Id,
                       d.DeptName
                FROM Employee e
                JOIN Department d ON d.Id = e.DepartmentId
            ", 
            // this is a lambda function that takes 2 arguments. The arguments holds the 2 object instances from the 2 tables that were queried (db.Query<Employee, Department, Employee>) - takes those objects and passes them in to the lambda function 
            (employee, department) =>
            {
                // attach department instance to the employee instance - (connect marketing to marjorie) and return the employee instance
                employee.Department = department;
                return employee;
            })
            .ToList()
            .ForEach(emp => Console.WriteLine($"{emp.FirstName} {emp.LastName} works in the {emp.Department.DeptName} Department"));


            // get a list of all departments and the employees assigned to each dept
            Dictionary<string, List<Employee>> report = new Dictionary<string, List<Employee>>();

            IEnumerable<Department> deptAndEmps = db.Query<Department, Employee, Department>(@"
                SELECT d.Id,
                    d.DeptName,
                    e.Id,
                    e.FirstName,
                    e.LastName,
                    e.DepartmentId
                FROM Department d
                JOIN Employee e ON e.DepartmentId = d.Id
            ",
            (department, employee) => {
                // we want a department and a list of employees -> need to add employees to the department list of Employees
                if (!report.ContainsKey(department.DeptName)) {
                    // create a new employee list that has the id of the department
                    report[department.DeptName] = new List<Employee>();
                }
                report[department.DeptName].Add(employee);

                return department;
            });

            foreach (KeyValuePair<string, List<Employee>> reportItem in report)
            {
                Console.WriteLine($"{reportItem.Key} has the following employees");

                foreach (Employee emp in reportItem.Value)
                {
                    Console.WriteLine($@"   * {emp.FirstName} {emp.LastName}");
                }
            }
        }
    }
}